#!/usr/bin/env python3
"""Plan/create isolated task or release worktrees. Python 3.11+ and Git required.

Default is a dry run; --apply creates a local branch, worktree and draft document.
Never commits, merges, resets, deletes worktrees, runs tests, or starts agents.
This is workspace setup, not a permissions sandbox or runtime-data adapter.
"""
from __future__ import annotations

import argparse
from contextlib import contextmanager
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import re
import subprocess
import sys
from typing import Iterator, Optional


class WorkspaceError(RuntimeError):
    """An actionable precondition or Git operation failed."""


def git(repo: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess:
    env = os.environ.copy()
    # Never let inherited routing variables redirect Git to another worktree/index.
    for key in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR",
                "GIT_OBJECT_DIRECTORY", "GIT_ALTERNATE_OBJECT_DIRECTORIES"):
        env.pop(key, None)
    try:
        result = subprocess.run(
            ["git", "-C", str(repo), *args], capture_output=True,
            text=True, encoding="utf-8", errors="replace", env=env,
        )
    except (FileNotFoundError, OSError) as exc:
        raise WorkspaceError(f"Unable to execute Git: {exc}") from exc
    if check and result.returncode:
        raise WorkspaceError(
            f"git {' '.join(args)} failed: {result.stderr.strip() or result.stdout.strip()}"
        )
    return result


def output(repo: Path, *args: str) -> str:
    return git(repo, *args).stdout.strip()


def repo_root(path: Path) -> Path:
    root = Path(output(path.resolve(), "rev-parse", "--show-toplevel")).resolve()
    if output(root, "rev-parse", "--is-bare-repository") != "false":
        raise WorkspaceError("A non-bare repository with an initial commit is required.")
    return root


def git_path(repo: Path, name: str) -> Path:
    value = Path(output(repo, "rev-parse", "--git-path", name))
    return value.resolve() if value.is_absolute() else (repo / value).resolve()


def worktrees(repo: Path) -> list[dict[str, str]]:
    """Use NUL separators so spaces and non-ASCII paths are not Git-quoted."""
    result: list[dict[str, str]] = []
    record: dict[str, str] = {}
    for field in git(repo, "worktree", "list", "--porcelain", "-z").stdout.split("\0"):
        if not field:
            if record:
                result.append(record)
                record = {}
            continue
        key, _, value = field.partition(" ")
        record[key] = value
    if record:
        result.append(record)
    return result


def is_inside(path: Path, parent: Path) -> bool:
    try:
        path.relative_to(parent)
        return True
    except ValueError:
        return False


def resolve_commit(repo: Path, ref: str) -> str:
    if not ref or ref.startswith("-"):
        raise WorkspaceError("Base must be a commit/ref, not an option.")
    return output(repo, "rev-parse", "--verify", "--end-of-options", f"{ref}^{{commit}}")


def require_template(repo: Path, commit: str, path: str) -> str:
    result = git(repo, "show", f"{commit}:{path}", check=False)
    if result.returncode:
        raise WorkspaceError(
            f"Base commit lacks {path}. First merge/install and explicitly commit the "
            "harness in your chosen baseline; uncommitted files are never copied."
        )
    return result.stdout


@contextmanager
def creation_lock(repo: Path) -> Iterator[None]:
    common = Path(output(repo, "rev-parse", "--git-common-dir"))
    if not common.is_absolute():
        common = repo / common
    lock = common.resolve() / "harness-create.lock"
    try:
        fd = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    except FileExistsError as exc:
        raise WorkspaceError(
            f"Workspace creation is locked: {lock}. If a previous process crashed, "
            "confirm no creation is active before manually removing this lock."
        ) from exc
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as stream:
            json.dump({"pid": os.getpid(), "created_at": now()}, stream)
        yield
    finally:
        lock.unlink(missing_ok=True)


def now() -> str:
    return datetime.now(timezone.utc).isoformat()


def make_plan(repo: Path, args: argparse.Namespace) -> dict:
    commit = resolve_commit(repo, args.base)
    existing = worktrees(repo)
    if not existing or "worktree" not in existing[0]:
        raise WorkspaceError("Could not identify the primary worktree.")
    primary = Path(existing[0]["worktree"]).resolve()
    if args.command == "task":
        if not re.fullmatch(r"TASK-[0-9]{3,}", args.task_id):
            raise WorkspaceError("Task ID must match TASK-001 (at least three digits).")
        if not re.fullmatch(r"[a-z0-9]+(?:-[a-z0-9]+)*", args.slug) or len(args.slug) > 64:
            raise WorkspaceError("Slug must be 1-64 lowercase letters/digits separated by hyphens.")
        ident = args.task_id
        key = f"{ident}-{args.slug}"
        branch, template, document = f"task/{key}", "tasks/_TEMPLATE.md", f"tasks/{key}.md"
        if output(repo, "for-each-ref", "--format=%(refname)", f"refs/heads/task/{ident}-*"):
            raise WorkspaceError(f"Task ID {ident} already has a local branch.")
        tracked = git(repo, "ls-tree", "-r", "--name-only", "-z", commit, "--", "tasks/").stdout
        if any(Path(p).name.startswith(f"{ident}-") for p in tracked.split("\0") if p):
            raise WorkspaceError(f"Task ID {ident} already exists in the selected baseline.")
        title = args.title or args.slug
    else:
        ident = args.version
        if (not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,63}", ident)
                or ".." in ident):
            raise WorkspaceError("Version must be a safe identifier such as v0.1.0.")
        key, branch = f"release-{ident}", f"release/{ident}"
        template, document, title = "versions/_TEMPLATE.md", f"versions/{ident}.md", ident
    if "\n" in title or "\r" in title:
        raise WorkspaceError("Title must be a single line.")
    git(repo, "check-ref-format", "--branch", branch)
    if git(repo, "show-ref", "--verify", "--quiet", f"refs/heads/{branch}", check=False).returncode == 0:
        raise WorkspaceError(f"Branch already exists: {branch}")
    parent = (args.root.expanduser().resolve() if args.root else
              primary.parent / f"{primary.name}-worktrees")
    destination = (parent / key).resolve()
    if destination.exists() or destination.is_symlink():
        raise WorkspaceError(f"Destination already exists: {destination}")
    for item in existing:
        if "worktree" in item and is_inside(destination, Path(item["worktree"]).resolve()):
            raise WorkspaceError("New worktrees must be outside all existing worktrees.")
    for path in ("AGENTS.md", ".codex/agents/code-builder.toml", template,
                 "tools/harness.py", "tools/harnesslib/engine.py", "tools/harnesslib/common.py",
                 "tools/harnesslib/workspace.py", "tools/harnesslib/release.py", "tools/harnesslib/models.py",
                 "tools/harnesslib/manifest.py", "models.toml", ".codex/agents/feature-designer.toml", ".codex/agents/qa-reporter.toml",
                 ".codex/agents/design-art-agent.toml", ".harness/.gitignore"):
        require_template(repo, commit, path)
    if git(repo, "cat-file", "-e", f"{commit}:{document}", check=False).returncode == 0:
        raise WorkspaceError(f"Document already exists at the selected base: {document}")
    # Recursive submodule setup is deliberately not automated by this minimal tool.
    entries = git(repo, "ls-tree", "-r", "-z", commit).stdout.split("\0")
    if any(entry.startswith("160000 ") for entry in entries):
        raise WorkspaceError("Submodule projects require a separately reviewed setup; this helper does not initialize them.")
    dirty = bool(git(repo, "status", "--porcelain=v1", "--untracked-files=all").stdout)
    return {
        "schema_version": 1, "kind": args.command, "id": ident,
        "title": title, "source_worktree": str(repo), "primary_worktree": str(primary),
        "workspace_root": str(destination), "branch": branch,
        "base_commit": commit, "document": document, "template": template,
        "runtime_root": str(destination / ".harness" / "runtime" / ident),
        "source_is_dirty": dirty, "created_at": None,
        "runtime_isolation": "Not Configured; application/engine adapters are required",
        "git_command": ["git", "-C", str(repo), "worktree", "add", "-b", branch,
                        str(destination), commit],
    }


def create(repo: Path, args: argparse.Namespace) -> dict:
    if not args.apply:
        plan = make_plan(repo, args)
        plan["action"] = "dry-run; no files or refs written"
        return plan
    with creation_lock(repo):
        plan = make_plan(repo, args)  # Repeat identity checks while holding shared helper lock.
        if plan["source_is_dirty"]:
            raise WorkspaceError(
                "Source worktree has uncommitted/untracked changes. Preserve them and "
                "choose an explicitly committed, clean source before --apply; no stash/reset is performed."
            )
        destination = Path(plan["workspace_root"])
        destination.parent.mkdir(parents=True, exist_ok=True)
        git(repo, "worktree", "add", "-b", plan["branch"], str(destination), plan["base_commit"])
        try:
            doc = destination / plan["document"]
            # Reject symlinked directories pointing outside the new worktree.
            if not is_inside(doc.resolve(), destination):
                raise WorkspaceError("Document path escapes the new worktree.")
            from .engine import initialize
            initialize(destination, plan)
            runtime = Path(plan["runtime_root"])
            if not is_inside(runtime.resolve(), destination):
                raise WorkspaceError("Runtime path escapes the new worktree.")
            for name in ("build", "cache", "user-data", "logs", "tmp"):
                (runtime / name).mkdir(parents=True, exist_ok=True)
            plan["created_at"] = now()
            metadata = git_path(destination, "harness-workspace.json")
            with metadata.open("x", encoding="utf-8", newline="\n") as stream:
                json.dump(plan, stream, ensure_ascii=False, indent=2)
                stream.write("\n")
        except (OSError, WorkspaceError) as exc:
            raise WorkspaceError(
                f"Worktree was created at {destination}, but initialization did not finish: {exc}. "
                "It has NOT been deleted; inspect it and git worktree list before manual recovery."
            ) from exc
        return {**plan, "action": "created; draft is uncommitted; no tests or agents started"}


def check_workspace(repo: Path) -> dict:
    metadata = git_path(repo, "harness-workspace.json")
    if not metadata.is_file():
        raise WorkspaceError("No helper metadata for this worktree. Run check inside a helper-created task/release.")
    try:
        plan = json.loads(metadata.read_text(encoding="utf-8"))
    except (ValueError, OSError) as exc:
        raise WorkspaceError(f"Unreadable workspace metadata: {exc}") from exc
    required = ("workspace_root", "branch", "base_commit", "document", "id", "kind")
    if not isinstance(plan, dict) or any(not isinstance(plan.get(k), str) for k in required):
        raise WorkspaceError("Workspace metadata is incomplete; inspect it, do not guess identity.")
    branch = output(repo, "branch", "--show-current")
    if Path(plan["workspace_root"]).resolve() != repo or branch != plan["branch"]:
        raise WorkspaceError("Worktree path or branch identity changed. Stop and reconcile the Task contract.")
    if git(repo, "merge-base", "--is-ancestor", plan["base_commit"], "HEAD", check=False).returncode:
        raise WorkspaceError("Recorded base is not an ancestor of HEAD. Reconcile the workspace contract.")
    if not (repo / plan["document"]).is_file():
        raise WorkspaceError("The assigned Task/Version document is missing.")
    return {
        "workspace_identity": "OK", "id": plan["id"], "kind": plan["kind"],
        "workspace_root": str(repo), "branch": branch,
        "head": output(repo, "rev-parse", "HEAD"), "base_commit": plan["base_commit"],
        "document": plan["document"],
        "working_changes": git(repo, "status", "--short", "--untracked-files=all").stdout,
        "committed_change_summary": git(repo, "diff", "--stat", plan["base_commit"], "HEAD").stdout,
        "scope_review": "PM must compare changes with Task ownership; no path ACL is enforced",
        "runtime_isolation": "Not assessed; directory creation does not configure the application",
        "game_qa": "Not run; this command checks workspace identity only",
    }


def main(argv: Optional[list[str]] = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path.cwd(), help="Source repository or worktree")
    sub = parser.add_subparsers(dest="command", required=True)
    task = sub.add_parser("task", help="Plan/create a task branch, worktree and draft")
    task.add_argument("task_id")
    task.add_argument("slug")
    task.add_argument("--title", help="Human-readable single-line Task title")
    release = sub.add_parser("release", help="Plan/create a release branch, worktree and draft")
    release.add_argument("version")
    for command in (task, release):
        command.add_argument("--base", required=True, help="Explicit committed ref/hash; resolved once to a hash")
        command.add_argument("--root", type=Path, help="Worktree parent, outside all existing worktrees")
        command.add_argument("--apply", action="store_true", help="Actually create local branch/worktree/draft")
    sub.add_parser("list", help="Read registered Git worktrees")
    sub.add_parser("check", help="Check identity of this helper-created worktree; not game QA")
    args = parser.parse_args(argv)
    try:
        repo = repo_root(args.repo)
        if args.command in ("task", "release"):
            result = create(repo, args)
        elif args.command == "list":
            result = {"worktrees": worktrees(repo)}
        else:
            result = check_workspace(repo)
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    except (WorkspaceError, OSError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
