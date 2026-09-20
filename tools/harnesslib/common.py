"""Shared deterministic IO. Python 3.11+, no third-party runtime dependencies."""
from __future__ import annotations
from contextlib import contextmanager
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import tempfile
import uuid

class HarnessError(RuntimeError):
    pass

def require(condition, message):
    if not condition:
        raise HarnessError(message)

def now():
    return datetime.now(timezone.utc).isoformat()

def uid(prefix="run"):
    return prefix + "-" + uuid.uuid4().hex[:16]

def dump(value):
    return json.dumps(value, ensure_ascii=False, indent=2, allow_nan=False) + "\n"

def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, ensure_ascii=False,
                                    separators=(",", ":"), allow_nan=False).encode()).hexdigest()

def sha(path):
    h = hashlib.sha256()
    with Path(path).open("rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

def json_read(path):
    try:
        return json.loads(Path(path).read_text(encoding="utf-8"),
                          parse_constant=lambda s: (_ for _ in ()).throw(ValueError(s)))
    except (ValueError, OSError) as e:
        raise HarnessError(f"Cannot read JSON {path}: {e}") from e

def safe_path(root, value, *, exists=False):
    root = Path(root).resolve()
    require(isinstance(value, str) and value and "\\" not in value, "Use a nonempty POSIX relative path")
    rel = Path(value)
    require(not rel.is_absolute() and not any(p in ("..", ".git") for p in rel.parts),
            f"Unsafe path: {value}")
    p = root / rel
    require(p.resolve().is_relative_to(root), f"Path escapes workspace: {value}")
    require(all(not a.is_symlink() for a in [p, *list(p.parents)[:len(rel.parts)]]),
            f"Symlinked path not allowed: {value}")
    if exists:
        require(p.is_file(), f"Missing regular file: {value}")
    return p

def atomic_text(path, text):
    path = Path(path)
    require(not path.is_symlink(), f"Refusing symlink: {path}")
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, tmp = tempfile.mkstemp(prefix=".harness-", dir=path.parent)
    try:
        with os.fdopen(fd, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
            f.flush()
            os.fsync(f.fileno())
        os.replace(tmp, path)
    finally:
        if os.path.exists(tmp):
            os.unlink(tmp)

@contextmanager
def lock(root, key):
    p = safe_path(root, ".harness/locks/" + hashlib.sha256(key.encode()).hexdigest()[:24] + ".lock")
    p.parent.mkdir(parents=True, exist_ok=True)
    try:
        fd = os.open(p, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    except FileExistsError as e:
        raise HarnessError(f"Locked: {p}. If a process crashed, confirm it has stopped before removing this lock.") from e
    try:
        with os.fdopen(fd, "w") as f:
            f.write(dump({"pid": os.getpid(), "time": now(), "key": key}))
        yield
    finally:
        p.unlink(missing_ok=True)

BLOCK = re.compile(r"^```harness-state\n(.*?)\n```\s*$", re.M | re.S)

def parse_doc(text):
    # Non-greedy boundaries, require exactly one authoritative block.
    blocks = re.findall(r"^```harness-state\n(.*?)\n```[ \t]*$", text, re.M | re.S)
    require(len(blocks) == 1, "Document needs exactly one harness-state block; legacy documents require migration")
    try:
        s = json.loads(blocks[0])
    except ValueError as e:
        raise HarnessError(f"Invalid state JSON: {e}") from e
    require(isinstance(s, dict) and s.get("schema_version") == 1, "Unsupported state schema")
    require(s.get("kind") in ("task", "release"), "Invalid document kind")
    require(isinstance(s.get("runs"), dict), "Missing runs record")
    return s

def load(root, doc):
    p = safe_path(root, doc, exists=True)
    return parse_doc(p.read_text(encoding="utf-8"))

def save(root, doc, state):
    p = safe_path(root, doc, exists=True)
    text = p.read_text(encoding="utf-8")
    parse_doc(text)
    pattern = re.compile(r"^```harness-state\n.*?\n```[ \t]*$", re.M | re.S)
    new = "```harness-state\n" + dump(state).rstrip() + "\n```"
    atomic_text(p, pattern.sub(lambda m: new, text, count=1))

def new_doc(root, doc, title, state):
    p = safe_path(root, doc)
    require(not p.exists(), f"Document exists: {doc}")
    atomic_text(p, f"# {title}\n\n机器状态由 `tools/harness.py` 维护。以下是唯一可执行记录；备注不是第二份合同。\n\n"
                + "```harness-state\n" + dump(state).rstrip() + "\n```\n\n## Notes\n\n")

def event(s, what, **data):
    s.setdefault("history", []).append({"at": now(), "event": what, **data})

def identity(root, s):
    from . import workspace as ws
    require(Path(s.get("workspace", "")).resolve() == Path(root).resolve(), "Wrong workspace for document")
    branch = ws.output(Path(root), "branch", "--show-current")
    require(branch == s.get("branch") and branch, "Wrong branch / detached HEAD")
    require(ws.git(Path(root), "merge-base", "--is-ancestor", s["base_commit"], "HEAD", check=False).returncode == 0,
            "Base commit is no longer an ancestor")
    meta = ws.git_path(Path(root), "harness-workspace.json")
    require(meta.is_file(), "Use a helper-created isolated worktree; no workspace identity record")
    m = json_read(meta)
    require(m["id"] == s["id"] and m["kind"] == s["kind"] and m["branch"] == branch,
            "Workspace registration does not match document")

def record_file(root, path, **extra):
    p = safe_path(root, path, exists=True)
    return {"path": path, "sha256": sha(p), "bytes": p.stat().st_size, **extra}

def verify_file(root, rec):
    require(isinstance(rec, dict) and "path" in rec and "sha256" in rec, "Invalid file identity")
    require(sha(safe_path(root, rec["path"], exists=True)) == rec["sha256"], f"File changed: {rec['path']}")

def within(path, prefixes):
    rel = Path(path)
    return any(rel == Path(p) or rel.is_relative_to(Path(p)) for p in prefixes)

def string(value, label):
    require(isinstance(value, str) and value.strip(), f"{label} is required")
    return value
