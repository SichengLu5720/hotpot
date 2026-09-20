"""Single desired model source; narrowly managed TOML fields, optimistic edits and rollback."""
from __future__ import annotations
import copy
import difflib
import json
from pathlib import Path
import re
import tomllib
from .common import (HarnessError, require, dump, digest, sha, safe_path, atomic_text,
                     lock, json_read, uid, now, parse_doc)

ROLES = {
    "feature_designer": "feature-designer.toml",
    "design_art_agent": "design-art-agent.toml",
    "code_builder": "code-builder.toml",
    "qa_reporter": "qa-reporter.toml",
}
ROLE_PRESENTATION = {
    "feature_designer": {"display_name": "Feature Designer", "caption": "技术设计 · QA 计划/用例/断言/接口需求"},
    "design_art_agent": {"display_name": "Design-Art", "caption": "实机预览 · 资产概念 · 正式资产"},
    "code_builder": {"display_name": "Code Builder", "caption": "功能实现 · 最终绑定 · 按计划编写测试脚本"},
    "qa_reporter": {"display_name": "QA Reporter", "caption": "可选 · 只读 Runner 结果总结"},
}
EFFORTS = {"", "none", "minimal", "low", "medium", "high", "xhigh", "max", "ultra"}
START = "# BEGIN HARNESS MODEL\n"
END = "# END HARNESS MODEL\n"
PATTERN = re.compile(r"^# BEGIN HARNESS MODEL\n.*?^# END HARNESS MODEL\n", re.M | re.S)


def read_config(text):
    try:
        c = tomllib.loads(text)
    except tomllib.TOMLDecodeError as e:
        raise HarnessError(f"Invalid models.toml: {e}") from e
    require(set(c) == {"schema_version", "active_profile", "profiles", "agents"}, "Unexpected/missing model config keys")
    require(c["schema_version"] == 1, "Unsupported models schema")
    require(c["active_profile"] in c["profiles"], "Unknown active profile")
    require(set(c["agents"]) == set(ROLES), "Model config must list the four supported subagents")
    require(c["profiles"] and len(c["profiles"]) <= 20, "Need 1-20 profiles")
    for key, p in c["profiles"].items():
        require(re.fullmatch(r"[a-z][a-z0-9_-]{0,31}", key), "Invalid profile name")
        require(set(p) == {"model", "effort"}, "A profile has exactly model and effort")
        _values(p)
    for role, a in c["agents"].items():
        require(set(a) == {"enabled", "profile", "model", "effort"}, f"Invalid fields for {role}")
        require(type(a["enabled"]) is bool, "enabled must be boolean")
        require(a["profile"] == "" or a["profile"] in c["profiles"], f"Unknown profile for {role}")
        _values(a)
    return c


def _values(v):
    require(isinstance(v["model"], str) and len(v["model"]) <= 160 and
            not any(ord(c) < 32 for c in v["model"]), "Invalid model identifier")
    require(v["effort"] in EFFORTS, "Unsupported effort label")


def effective(c, role):
    a = c["agents"][role]
    p = c["profiles"][a["profile"] or c["active_profile"]]
    return {"model": a["model"] or p["model"] or None,
            "effort": a["effort"] or p["effort"] or None, "enabled": a["enabled"]}


def serialize(c):
    read_config(_serialize(c))
    return _serialize(c)


def _serialize(c):
    q = lambda v: json.dumps(v, ensure_ascii=False)
    lines = ["# Harness desired configuration; blank model means host inheritance, not a cheap model.",
             "schema_version = 1", f'active_profile = {q(c["active_profile"])}', ""]
    for key, p in c["profiles"].items():
        lines += [f"[profiles.{key}]", f'model = {q(p["model"])}', f'effort = {q(p["effort"])}', ""]
    for role in ROLES:
        a = c["agents"][role]
        lines += [f"[agents.{role}]", f'enabled = {str(a["enabled"]).lower()}',
                  f'profile = {q(a["profile"])}', f'model = {q(a["model"])}', f'effort = {q(a["effort"])}', ""]
    return "\n".join(lines)


def managed(e):
    lines = [START.rstrip()]
    if e["model"]:
        lines.append("model = " + json.dumps(e["model"], ensure_ascii=False))
    if e["effort"]:
        lines.append("model_reasoning_effort = " + json.dumps(e["effort"]))
    lines.append(END.rstrip())
    return "\n".join(lines) + "\n"


def block(text):
    matches = PATTERN.findall(text)
    require(len(matches) == 1, "Agent needs exactly one managed model block; do not overwrite a custom agent blindly")
    return matches[0]


def paths(root):
    return {role: safe_path(root, ".codex/agents/" + filename, exists=True) for role, filename in ROLES.items()}


def snapshot(root):
    p = safe_path(root, "models.toml", exists=True)
    files = {"models.toml": p.read_text(encoding="utf-8")}
    for role, f in paths(root).items():
        files[f".codex/agents/{ROLES[role]}"] = f.read_text(encoding="utf-8")
    return files


def pending(root):
    d = safe_path(root, ".harness/model-transactions")
    return [p for p in d.glob("*/journal.json") if json_read(p).get("status") == "prepared"]


def check_pending(root):
    require(not pending(root), "Interrupted model transaction; run models recover before further changes or dispatch")


def status(root):
    files = snapshot(root)
    c = read_config(files["models.toml"])
    rows = []
    sync_file = safe_path(root, ".harness/model-sync.json")
    sync = json_read(sync_file) if sync_file.exists() else {}
    for role, filename in ROLES.items():
        text = files[".codex/agents/" + filename]
        b = block(text)
        try:
            agent = tomllib.loads(text)
        except tomllib.TOMLDecodeError as e:
            raise HarnessError(f"Invalid native Agent TOML: {filename}: {e}") from e
        require(agent.get("name") == role, "Native agent name mismatch")
        observed = []
        for parent in ("tasks", "versions"):
            for p in safe_path(root, parent).glob("*.md"):
                if p.name.startswith("_"):
                    continue
                try:
                    s = parse_doc(p.read_text(encoding="utf-8"))
                    for run in s["runs"].values():
                        if run.get("role") == role:
                            observed.append({"run_id": run["run_id"], "at": run["created_at"],
                                             "target": run.get("model_target"),
                                             "observed": run.get("model_observed"), "status": run["status"]})
                except (HarnessError, ValueError):
                    continue
        observed.sort(key=lambda x: x["at"], reverse=True)
        e = effective(c, role)
        baseline = sync.get("blocks", {}).get(role)
        rows.append({"role": role, **ROLE_PRESENTATION[role], "target": e,
                     "native": {"model": agent.get("model"), "effort": agent.get("model_reasoning_effort")},
                     "synced": b == managed(e),
                     "manual_drift": baseline is not None and baseline != digest(b),
                     "recent_runs": observed[:5]})
    return {"root": str(Path(root).resolve()), "etag": digest(files), "config": c,
            "rows": rows, "pending_transactions": [p.parent.name for p in pending(root)],
            "availability": "Not queried; model IDs and effort compatibility require host confirmation"}


def preview(root, text=None, *, reconcile=False):
    check_pending(root)
    before = snapshot(root)
    c = read_config(text if text is not None else before["models.toml"])
    current = status(root)
    first_sync = not safe_path(root, ".harness/model-sync.json").exists()
    require(reconcile or (not any(x["manual_drift"] for x in current["rows"]) and
                          not (first_sync and any(not x["synced"] for x in current["rows"]))),
            "Native model fields were edited externally. Inspect diff and explicitly reconcile.")
    after = dict(before)
    if text is not None:
        after["models.toml"] = text
    for role, file in ROLES.items():
        path = ".codex/agents/" + file
        block(before[path])
        after[path] = PATTERN.sub(lambda m: managed(effective(c, role)), before[path], count=1)
        try:
            tomllib.loads(after[path])
        except tomllib.TOMLDecodeError as e:
            raise HarnessError(f"Conflicting unmanaged model fields in {path}: {e}") from e
    diff = "".join("".join(difflib.unified_diff(before[k].splitlines(True), after[k].splitlines(True),
                                            fromfile=k, tofile=k)) for k in before)
    return {"etag": digest(before), "diff": diff, "before": before, "after": after,
            "config": c, "reconcile": reconcile}


def _sync_record(root, config):
    atomic_text(safe_path(root, ".harness/model-sync.json"), dump({
        "at": now(), "blocks": {r: digest(managed(effective(config, r))) for r in ROLES}}))


def _commit(root, before, after):
    txid = uid("models")
    journal_path = safe_path(root, f".harness/model-transactions/{txid}/journal.json")
    j = {"id": txid, "at": now(), "status": "prepared", "before": before, "after": after}
    atomic_text(journal_path, dump(j))
    try:
        for name, text in after.items():
            if text != before[name]:
                atomic_text(safe_path(root, name, exists=True), text)
        _sync_record(root, read_config(after["models.toml"]))
    except Exception:
        # Keep journal prepared if rollback itself fails. Recovery is explicit.
        for name, text in before.items():
            atomic_text(safe_path(root, name), text)
        _sync_record(root, read_config(before["models.toml"]))
        j["status"] = "rolled_back"
        atomic_text(journal_path, dump(j))
        raise
    j["status"] = "committed"
    atomic_text(journal_path, dump(j))
    return {"transaction": txid, "status": "applied", "etag": digest(after),
            "host_reload": "New dispatch only; reload native agent definitions as required by your host"}


def apply(root, text, etag, *, reconcile=False):
    with lock(root, "models"):
        p = preview(root, text, reconcile=reconcile)
        require(p["etag"] == etag, "Configuration changed since preview; reload and preview again")
        return _commit(root, p["before"], p["after"])


def update_agents(root, updates, etag):
    """Apply the compact native dashboard's per-agent model and effort fields."""
    require(isinstance(updates, dict) and set(updates) == set(ROLES),
            "Native dashboard must provide exactly the four supported subagents")
    with lock(root, "models"):
        check_pending(root)
        current = status(root)
        require(current["etag"] == etag,
                "Configuration changed since the window was loaded; refresh and try again")
        config = copy.deepcopy(current["config"])
        for role in ROLES:
            value = updates[role]
            require(isinstance(value, dict) and set(value) == {"model", "effort"},
                    f"Invalid native dashboard fields for {role}")
            _values(value)
            # The compact UI edits explicit role overrides so its visible values are
            # exactly the values used for the next dispatch.
            config["agents"][role]["profile"] = ""
            config["agents"][role]["model"] = value["model"]
            config["agents"][role]["effort"] = value["effort"]
        text = serialize(config)
        proposal = preview(root, text)
        require(proposal["etag"] == etag,
                "Configuration changed since the window was loaded; refresh and try again")
        return _commit(root, proposal["before"], proposal["after"])


def rollback(root, txid):
    require(re.fullmatch(r"models-[a-f0-9]{16}", txid), "Invalid model transaction ID")
    with lock(root, "models"):
        check_pending(root)
        j = json_read(safe_path(root, f".harness/model-transactions/{txid}/journal.json", exists=True))
        require(j["status"] == "committed", "Only committed transactions can be rolled back")
        current = snapshot(root)
        # Preserve subsequent unrelated instructions edits, but reject model drift.
        require(current["models.toml"] == j["after"]["models.toml"], "models.toml changed after this transaction")
        restored = dict(current)
        restored["models.toml"] = j["before"]["models.toml"]
        for name in current:
            if name == "models.toml":
                continue
            require(block(current[name]) == block(j["after"][name]), "Model fields changed after this transaction")
            restored[name] = PATTERN.sub(lambda m: block(j["before"][name]), current[name], count=1)
        result = _commit(root, current, restored)
        result["rollback_of"] = txid
        return result


def recover(root):
    with lock(root, "models"):
        recovered = []
        for path in pending(root):
            j = json_read(path)
            current = snapshot(root)
            require(all(current[k] in (j["before"][k], j["after"][k]) for k in current),
                    "Interrupted transaction conflicts with manual changes; inspect journal, do not overwrite")
            for name, text in j["before"].items():
                atomic_text(safe_path(root, name), text)
            _sync_record(root, read_config(j["before"]["models.toml"]))
            j["status"] = "rolled_back"
            atomic_text(path, dump(j))
            recovered.append(j["id"])
        return {"recovered": recovered}


def dispatch_target(root, role):
    require(role in ROLES, "Unknown agent")
    with lock(root, "models"):
        check_pending(root)
        st = status(root)
        row = next(x for x in st["rows"] if x["role"] == role)
        require(row["target"]["enabled"], f"Agent disabled: {role}")
        require(row["synced"] and not row["manual_drift"], "Models not synced; preview/apply before new dispatch")
        return {**row["target"], "config_digest": digest(st["config"]), "actual": "unknown"}
