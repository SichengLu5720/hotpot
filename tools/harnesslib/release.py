"""Version snapshots, Designer-owned QA plans, Builder scripts, and deterministic execution."""
from __future__ import annotations
import copy
import os
from pathlib import Path
import signal
import subprocess
import sys
import time
from .common import *
from . import workspace as ws


def plan(root, s):
    if s["status"] in ("BLOCKED", "CANCELLED"):
        return {"status": s["status"], "next": [], "waiting": s.get("block")}
    if not s["prepared"]:
        return {"status": s["status"], "next": [], "waiting": "Integrate fixed task commits, then release prepare"}
    active = [r for r in s["runs"].values() if r["status"] == "ACTIVE"]
    if active:
        return {"status": s["status"], "next": [], "in_flight": [r["run_id"] for r in active]}
    if s.get("fix_request"):
        return {"status": s["status"], "next": ["release_fix"], "request": s["fix_request"]}
    steps = []
    if not s.get("qa_plan_handoff"):
        steps.append("interfaces")
    elif not s["script_handoff"]:
        steps.append("qa_scripts")
    elif s["results"]:
        steps.append("qa_report")
    return {"status": s["status"], "next": steps,
            "waiting": "Designer plan → Builder scripts → authorize → qa run" if not s["results"] else "PM review runner results",
            "interfaces": "Required read-only QA plan, cases, assertions, and interface requirements"}


def clean_candidate(root, doc, release_id):
    raw = ws.git(Path(root), "status", "--porcelain=v1", "-z", "--untracked-files=all").stdout.split("\0")
    allowed = {doc, f"releases/{release_id}/QA_BACKLOG.md", f"releases/{release_id}/QA_PLAN.md",
               f"releases/{release_id}/QA_EXECUTION.md"}
    dirty = []
    i = 0
    while i < len(raw):
        entry = raw[i]; i += 1
        if not entry:
            continue
        flag, path = entry[:2], entry[3:]
        if "R" in flag or "C" in flag:
            # No silently ignored rename to/from a control document.
            dirty.extend([path, raw[i] if i < len(raw) else "missing rename source"])
            i += 1
        elif path not in allowed:
            dirty.append(path)
    require(not dirty, "Candidate has uncommitted files (no stash/reset performed): " + ", ".join(dirty[:12]))


def intent_ids(s):
    return {x["id"] + ":" + item["id"] for x in s["inputs"] for item in x["qa_intent"]}


def render_backlog(s):
    lines = [f"# {s['id']} — QA Backlog", "", "> 自动从固定 Task 提取；修改来源 Task，不在这里另立标准。", "",
             f"Input digest: `{s['input_digest']}`", ""]
    for item in s["inputs"]:
        lines += [f"## {item['id']} · revision {item['task_revision']}", "",
                  f"Commit: `{item['commit']}` · Document: `{item['document']}`", "",
                  item["goal"], ""]
        for q in item["qa_intent"]:
            lines += [f"- `{item['id']}:{q['id']}` — {q['text']} (source: {q['source']})"]
        lines += ["", "### Implementation facts / interface map", "", "```json",
                  dump({"interface_map": item["interface_map"], "implementation_facts": item["implementation_facts"]}).rstrip(), "```", ""]
    return "\n".join(lines)


def prepare(root, doc, entries):
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        require(s["kind"] == "release", "Not a release document")
        require(s["status"] not in ("BLOCKED", "CANCELLED"), "Resolve release pause first")
        require(not any(r["status"] == "ACTIVE" for r in s["runs"].values()), "Revoke/settle existing work first")
        require(isinstance(entries, list) and entries, "Provide fixed task input entries")
        clean_candidate(root, doc, s["id"])
        inputs = []; seen = set()
        for e in entries:
            require(set(e) == {"id", "commit", "document"}, "Input entry uses id, commit, document")
            require(e["id"] not in seen, "Duplicate task input")
            seen.add(e["id"])
            commit = ws.resolve_commit(Path(root), e["commit"])
            require(commit == e["commit"], "Use a full commit hash")
            safe_path(root, e["document"])
            require(ws.git(Path(root), "merge-base", "--is-ancestor", commit, "HEAD", check=False).returncode == 0,
                    "Input commit is not integrated in the release candidate")
            task = parse_doc(ws.output(Path(root), "show", f"{commit}:{e['document']}"))
            require(task["kind"] == "task" and task["id"] == e["id"] and task["status"] == "READY_FOR_RELEASE", "Input task is not ready")
            require(not any(r["status"] == "ACTIVE" for r in task["runs"].values()), "Input task has active work")
            require(task["contract"]["qa_intent"], "Input task has no QA intent")
            facts = []
            for step, rid in task["completed"].items():
                r = task["runs"][rid]
                if step in ("code", "integration", "designer"):
                    facts.append({"step": step, "summary": r["summary"], "payload": r["payload"]})
                for a in r.get("artifacts", []):
                    # Hash exact committed bytes, never text-normalize assets.
                    safe_path(root, a["path"])
                    import hashlib
                    cp = subprocess.run(["git", "-C", str(root), "show", f"{commit}:{a['path']}"], capture_output=True,
                                        env=_git_env(), check=False)
                    require(cp.returncode == 0 and hashlib.sha256(cp.stdout).hexdigest() == a["sha256"],
                            f"Accepted artifact not committed unchanged: {a['path']}")
                    if a.get("kind") == "asset_manifest":
                        import json
                        inventory = json.loads(cp.stdout.decode("utf-8"))
                        for item in inventory["assets"]:
                            safe_path(root, item["path"])
                            asset = subprocess.run(["git", "-C", str(root), "show", f"{commit}:{item['path']}"],
                                                   capture_output=True, env=_git_env(), check=False)
                            if item["action"] == "delete":
                                require(asset.returncode != 0, "Deleted asset exists in pinned commit")
                            else:
                                require(asset.returncode == 0 and hashlib.sha256(asset.stdout).hexdigest() == item["sha256"],
                                        f"Production asset changed after acceptance: {item['path']}")
            inputs.append({**e, "task_revision": task["task_revision"], "contract_digest": digest(task["contract"]),
                           "goal": task["contract"]["goal"], "qa_intent": copy.deepcopy(task["contract"]["qa_intent"]),
                           "interface_map": task["contract"]["interface_map"], "owners": task["contract"]["owners"], "implementation_facts": facts})
        s.update(inputs=inputs, input_digest=digest(inputs), integrated_commit=ws.output(Path(root), "rev-parse", "HEAD"),
                 prepared=True, qa_plan_handoff=None, script_handoff=None, authorization=None, status="PREPARING", completed={}, fix_request=None)
        event(s, "release_inputs_fixed", digest=s["input_digest"])
        atomic_text(safe_path(root, f"releases/{s['id']}/QA_BACKLOG.md"), render_backlog(s))
        save(root, doc, s)
        return {"id": s["id"], "input_digest": s["input_digest"], "backlog": f"releases/{s['id']}/QA_BACKLOG.md", "task_count": len(inputs)}


def _git_env():
    env = os.environ.copy()
    for k in ("GIT_DIR", "GIT_WORK_TREE", "GIT_INDEX_FILE", "GIT_COMMON_DIR", "GIT_OBJECT_DIRECTORY", "GIT_ALTERNATE_OBJECT_DIRECTORIES"):
        env.pop(k, None)
    return env


def validate_plan(root, s, p):
    """Validate the read-only Designer's acceptance definition; it contains no executable command."""
    require(isinstance(p, dict) and set(p) == {"schema_version", "release_id", "input_digest", "interface_requirements", "checks", "manual_checks"},
            "QA plan fields: schema_version/release_id/input_digest/interface_requirements/checks/manual_checks")
    require(p["schema_version"] == 2 and p["release_id"] == s["id"] and p["input_digest"] == s["input_digest"], "QA plan input identity mismatch")
    require(isinstance(p["checks"], list) and isinstance(p["manual_checks"], list) and (p["checks"] or p["manual_checks"]), "Empty QA plan")
    require(isinstance(p["interface_requirements"], list), "interface_requirements must be a list")
    interface_ids = set()
    for item in p["interface_requirements"]:
        require(isinstance(item, dict) and set(item) == {"id", "requirement", "source", "intent_ids"},
                "Interface requirement fields: id/requirement/source/intent_ids")
        string(item["id"], "interface requirement id"); string(item["requirement"], "interface requirement")
        string(item["source"], "interface requirement source")
        require(item["id"] not in interface_ids, "Duplicate interface requirement id"); interface_ids.add(item["id"])
        require(isinstance(item["intent_ids"], list) and item["intent_ids"] and set(item["intent_ids"]) <= intent_ids(s),
                "Unknown interface requirement intent mapping")
    all_intents = intent_ids(s); covered = set(); ids = set()
    for c in p["checks"]:
        require(isinstance(c, dict) and set(c) == {"id", "case", "fixture_reset", "interface_requirement_ids", "assertions"},
                "Automated case fields: id/case/fixture_reset/interface_requirement_ids/assertions")
        string(c.get("id"), "check id")
        require(__import__('re').fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,63}", c["id"]), "Unsafe check id")
        require(c["id"] not in ids, "Duplicate check id"); ids.add(c["id"])
        string(c.get("case"), "test case")
        string(c.get("fixture_reset"), "fixture isolation/reset explanation")
        require(isinstance(c["interface_requirement_ids"], list) and set(c["interface_requirement_ids"]) <= interface_ids,
                "Unknown interface requirement mapping")
        require(isinstance(c.get("assertions"), list) and c["assertions"], "A check must contain explicit assertions")
        aids = set()
        for a in c["assertions"]:
            require(isinstance(a, dict) and isinstance(a.get("id"), str) and a["id"] not in aids, "Unique assertion IDs required")
            string(a["id"], "assertion id"); aids.add(a["id"])
            string(a.get("expectation"), "sourced expectation")
            require(isinstance(a.get("intent_ids"), list) and a["intent_ids"], "Every assertion maps to QA Intent")
            require(set(a["intent_ids"]) <= all_intents, "Unknown intent mapping")
            covered.update(a["intent_ids"])
    for c in p["manual_checks"]:
        require(isinstance(c, dict) and set(c) == {"id", "instruction", "intent_ids"},
                "Manual case fields: id/instruction/intent_ids")
        string(c.get("id"), "manual id"); require(c["id"] not in ids, "Duplicate manual/check id"); ids.add(c["id"])
        string(c.get("instruction"), "manual instruction")
        require(isinstance(c.get("intent_ids"), list) and c["intent_ids"] and set(c["intent_ids"]) <= all_intents, "Unknown manual intent mapping")
        covered.update(c["intent_ids"])
    require(covered == all_intents, "QA coverage missing: " + ", ".join(sorted(all_intents - covered)))
    return p


def validate_execution(root, s, x, plan):
    """Validate Builder wiring without allowing it to redefine cases or assertions."""
    require(isinstance(x, dict) and set(x) == {"schema_version", "release_id", "input_digest", "qa_plan_digest", "files", "checks"},
            "QA execution fields: schema_version/release_id/input_digest/qa_plan_digest/files/checks")
    require(x["schema_version"] == 2 and x["release_id"] == s["id"] and x["input_digest"] == s["input_digest"],
            "QA execution input identity mismatch")
    require(x["qa_plan_digest"] == digest(plan), "Builder execution does not bind the accepted Designer QA plan")
    require(isinstance(x["files"], list) and len(x["files"]) == len(set(x["files"])), "Invalid execution file list")
    for f in x["files"]:
        safe_path(root, f, exists=True)
    expected = {c["id"]: {a["id"] for a in c["assertions"]} for c in plan["checks"]}
    require(isinstance(x["checks"], list) and len(x["checks"]) == len(expected), "Execution must implement every automated Designer case exactly once")
    seen = set()
    for c in x["checks"]:
        require(isinstance(c, dict) and set(c) == {"id", "command", "timeout_seconds", "fixture_implementation", "assertion_ids"},
                "Execution check fields: id/command/timeout_seconds/fixture_implementation/assertion_ids")
        require(c["id"] in expected and c["id"] not in seen, "Unknown or duplicate execution check id"); seen.add(c["id"])
        require(isinstance(c["command"], list) and c["command"] and all(isinstance(v, str) and v for v in c["command"]),
                "Command must be argv, never shell text")
        require(type(c["timeout_seconds"]) in (int, float) and 0 < c["timeout_seconds"] <= 3600, "Invalid timeout")
        string(c["fixture_implementation"], "fixture implementation")
        require(isinstance(c["assertion_ids"], list) and len(c["assertion_ids"]) == len(set(c["assertion_ids"])) and set(c["assertion_ids"]) == expected[c["id"]],
                "Builder assertion IDs must exactly match the accepted Designer plan")
        for token in c["command"][1:]:
            if token in x["files"]:
                continue
            if token.endswith((".py", ".sh", ".gd", ".js")) and (Path(root) / token).is_file():
                require(False, f"Executable helper must be pinned in files: {token}")
    require(seen == set(expected), "Execution coverage differs from the accepted Designer plan")
    return x


def render_plan(p):
    lines = [f"# {p['release_id']} — QA Plan", "", "> 自动视图；验收计划由 Designer 只读交接并固定，Builder 不得修改。", "",
             f"Inputs: `{p['input_digest']}`", ""]
    if p["interface_requirements"]:
        lines += ["## Interface requirements", ""]
        for item in p["interface_requirements"]:
            lines.append(f"- `{item['id']}`: {item['requirement']} → {', '.join(item['intent_ids'])} (source: {item['source']})")
        lines.append("")
    for c in p["checks"]:
        lines += [f"## {c['id']}", "", c["case"], "", f"Fixture/reset requirement: {c['fixture_reset']}", ""]
        for a in c["assertions"]:
            lines.append(f"- `{a['id']}`: {a['expectation']} → {', '.join(a['intent_ids'])}")
        lines.append("")
    for c in p["manual_checks"]:
        lines += [f"## Manual: {c['id']}", c["instruction"], ""]
    return "\n".join(lines)


def render_execution(x):
    lines = [f"# {x['release_id']} — QA Execution", "", "> 自动视图；Builder 仅绑定固定 Designer 计划到可执行脚本。", "",
             f"QA plan digest: `{x['qa_plan_digest']}`", ""]
    for c in x["checks"]:
        lines += [f"## {c['id']}", "", f"Fixture implementation: {c['fixture_implementation']}", "",
                  "```json", dump(c["command"]).rstrip(), "```", ""]
    return "\n".join(lines)


def accept_payload(root, s, run, payload, artifacts):
    if run["step"] == "interfaces":
        p = validate_plan(root, s, payload["qa_plan"])
        require(not artifacts, "Read-only Designer QA plan must be returned in payload, not written as an artifact")
        s["qa_plan_handoff"] = {"run_id": run["run_id"], "plan": copy.deepcopy(p), "plan_digest": digest(p)}
        s["script_handoff"] = None
        s["authorization"] = None
        atomic_text(safe_path(root, f"releases/{s['id']}/QA_PLAN.md"), render_plan(p))
    elif run["step"] == "qa_scripts":
        path = payload["qa_execution"]
        require(isinstance(path, str) and any(a["path"] == path for a in artifacts), "QA execution manifest must be a returned, hashed artifact")
        p = current_designer_plan(root, s)
        x = validate_execution(root, s, json_read(safe_path(root, path, exists=True)), p)
        s["script_handoff"] = {"run_id": run["run_id"], "execution": record_file(root, path),
                                "files": [record_file(root, f) for f in x["files"]]}
        s["authorization"] = None
        atomic_text(safe_path(root, f"releases/{s['id']}/QA_EXECUTION.md"), render_execution(x))
    elif run["step"] == "release_fix":
        s["fix_request"] = None
        s["authorization"] = None
        s["status"] = "PREPARING"
    elif run["step"] == "qa_report":
        require(s["results"], "Nothing has been executed; no QA report can be summarized")
        # This payload is commentary only; never affects the runner's verdict.


def current_designer_plan(root, s):
    require(s.get("qa_plan_handoff"), "No accepted Designer QA plan")
    p = s["qa_plan_handoff"]["plan"]
    require(s["qa_plan_handoff"]["plan_digest"] == digest(p), "Accepted Designer QA plan changed")
    return validate_plan(root, s, p)


def current_plan(root, s):
    require(s["script_handoff"], "No accepted Builder QA script handoff")
    p = current_designer_plan(root, s)
    h = s["script_handoff"]
    verify_file(root, h["execution"])
    for f in h["files"]:
        verify_file(root, f)
    x = validate_execution(root, s, json_read(safe_path(root, h["execution"]["path"], exists=True)), p)
    merged = copy.deepcopy(p)
    wiring = {c["id"]: c for c in x["checks"]}
    for c in merged["checks"]:
        c.update(command=wiring[c["id"]]["command"], timeout_seconds=wiring[c["id"]]["timeout_seconds"],
                 fixture_implementation=wiring[c["id"]]["fixture_implementation"])
    return merged


def authorize(root, doc, decision):
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        require(s["kind"] == "release" and s["status"] not in ("BLOCKED", "CANCELLED"), "Release cannot execute")
        require(not any(r["status"] == "ACTIVE" for r in s["runs"].values()), "Finish existing writers before authorizing QA")
        string(decision, "execution authorization")
        require(not s.get("fix_request"), "Finish requested repair before QA")
        current_plan(root, s); clean_candidate(root, doc, s["id"])
        commit = ws.output(Path(root), "rev-parse", "HEAD")
        require(ws.git(Path(root), "merge-base", "--is-ancestor", s["integrated_commit"], commit, check=False).returncode == 0,
                "Release history changed after integration")
        auth = {"decision": decision, "candidate_commit": commit,
                "plan_digest": digest(s["qa_plan_handoff"]), "execution_digest": digest(s["script_handoff"]),
                "input_digest": s["input_digest"]}
        auth["candidate_key"] = digest({k: v for k, v in auth.items() if k != "decision"})
        s["authorization"] = auth; s["status"] = "QA_READY"
        event(s, "qa_execution_authorized", candidate=auth["candidate_key"], decision=decision)
        save(root, doc, s)
        return auth


def verify_authorization(root, doc, s):
    identity(root, s)
    require(s["kind"] == "release" and s["status"] not in ("BLOCKED", "CANCELLED"), "Release paused")
    auth = s["authorization"]
    require(auth and auth["input_digest"] == s["input_digest"] and auth["plan_digest"] == digest(s["qa_plan_handoff"])
            and auth["execution_digest"] == digest(s["script_handoff"]), "Missing/stale QA authorization")
    require(ws.output(Path(root), "rev-parse", "HEAD") == auth["candidate_commit"], "Candidate commit changed; re-authorize")
    clean_candidate(root, doc, s["id"])
    return current_plan(root, s)


def _execute(root, s, p, c, run_id, folder):
    argv = [sys.executable if v == "{python}" else v for v in c["command"]]
    result_path = folder / "check-result.json"
    folder.mkdir(parents=True, exist_ok=False)
    env = _git_env()
    env.update(HARNESS_RUN_ID=run_id, HARNESS_CHECK_ID=c["id"], HARNESS_RESULT_PATH=str(result_path),
               HARNESS_RUNTIME_ROOT=str(folder / "runtime"), HARNESS_CANDIDATE=s["authorization"]["candidate_commit"])
    (folder / "runtime").mkdir()
    log = folder / "process.log"
    started = time.monotonic(); status = "ERROR"; error = None; code = None; evidence = []
    data = None
    try:
        with log.open("wb") as out:
            proc = subprocess.Popen(argv, cwd=root, env=env, stdout=out, stderr=subprocess.STDOUT,
                                    shell=False, start_new_session=(os.name != "nt"))
            try:
                code = proc.wait(timeout=c["timeout_seconds"])
            except subprocess.TimeoutExpired:
                if os.name != "nt":
                    os.killpg(proc.pid, signal.SIGKILL)
                else:
                    proc.kill()  # External descendants may survive; PM must inspect on Windows.
                proc.wait()
                raise HarnessError("TIMEOUT; process stop attempted; inspect detached/external jobs")
        require(result_path.is_file() and not result_path.is_symlink(), "Process did not produce a fresh structured report")
        require(result_path.stat().st_size <= 2 * 1024 * 1024, "Check report is too large")
        data = json_read(result_path)
        require(isinstance(data, dict) and data.get("schema_version") == 1 and data.get("run_id") == run_id and data.get("check_id") == c["id"], "Report identity mismatch")
        assertions = data.get("assertions")
        expected = {x["id"] for x in c["assertions"]}
        require(isinstance(assertions, list) and len(assertions) == len(expected) and
                all(isinstance(x, dict) for x in assertions) and {x.get("id") for x in assertions} == expected,
                "Zero, missing, duplicate or unexpected assertions")
        require(all(x.get("status") in ("PASS", "FAIL", "ERROR", "SKIP") for x in assertions), "Invalid assertion result")
        require(all("actual" in x for x in assertions), "Every assertion needs actual observation")
        for f in data.get("evidence", []):
            evidence.append(record_file(root, f))
        status = "PASS" if code == 0 and all(x["status"] == "PASS" for x in assertions) else "FAIL"
        if code != 0:
            error = f"Process exited {code}; report cannot override nonzero exit"
    except (HarnessError, OSError, ValueError, TypeError) as e:
        error = str(e); status = "ERROR"
    return {"id": c["id"], "status": status, "returncode": code, "seconds": round(time.monotonic()-started, 4),
            "error": error, "assertions": data.get("assertions", []) if isinstance(data, dict) else [],
            "log": record_file(root, log.relative_to(root).as_posix()) if log.exists() else None,
            "evidence": evidence, "raw_report": record_file(root, result_path.relative_to(root).as_posix()) if result_path.is_file() and not result_path.is_symlink() else None}


def run(root, doc, check_ids=None, *, apply=False):
    root = Path(root).resolve()
    with lock(root, doc):
        s = load(root, doc)
        p = verify_authorization(root, doc, s)
        selected = [c for c in p["checks"] if check_ids is None or c["id"] in check_ids]
        require(check_ids is None or set(check_ids) == {x["id"] for x in selected}, "Unknown/duplicate check selection")
        if not apply:
            return {"action": "dry-run", "commands": [c["command"] for c in selected], "candidate": s["authorization"]}
        require(selected, "No automated checks selected; record required manual evidence instead")
        require(not any(r["status"] == "ACTIVE" for r in s["runs"].values()), "Do not run QA concurrently with writers or another runner")
        run_id = uid("qa")
        s["runs"][run_id] = {"run_id": run_id, "role": "deterministic_runner", "step": "qa_run", "status": "ACTIVE",
                              "created_at": now(), "inputs": {"candidate_key": s["authorization"]["candidate_key"]},
                              "stop_confirmed": False}
        save(root, doc, s)
        candidate = s["authorization"]["candidate_key"]
    rows = []; aborted = None
    for c in selected:
        try:
            live = load(root, doc)
            verify_authorization(root, doc, live)
            require(live["runs"][run_id]["status"] == "ACTIVE" and live["authorization"]["candidate_key"] == candidate, "QA run revoked")
            folder = safe_path(root, f".harness/qa/{s['id']}/{run_id}/{c['id']}")
            rows.append(_execute(root, s, p, c, run_id, folder))
            live = load(root, doc); verify_authorization(root, doc, live)
            require(live["runs"][run_id]["status"] == "ACTIVE", "QA run revoked while command was executing")
        except (HarnessError, OSError) as e:
            aborted = str(e); break
    result = {"schema_version": 1, "release_id": s["id"], "run_id": run_id, "candidate_key": candidate,
              "candidate_commit": s["authorization"]["candidate_commit"],
              "qa_plan": s["qa_plan_handoff"], "execution": s["script_handoff"],
              "selected": [c["id"] for c in selected], "checks": rows, "aborted": aborted,
              "at": now(), "runner_python": sys.version, "complete_suite": len(selected) == len(p["checks"]),
              "status": "ERROR" if aborted else ("PASS" if all(x["status"] == "PASS" for x in rows) else "FAIL")}
    result_path = f".harness/qa/{s['id']}/{run_id}/results.json"
    atomic_text(safe_path(root, result_path), dump(result))
    ref = record_file(root, result_path, run_id=run_id, candidate_key=candidate)
    with lock(root, doc):
        live = load(root, doc)
        r = live["runs"][run_id]
        r["stop_confirmed"] = True
        if r["status"] == "ACTIVE":
            r["status"] = "ACCEPTED"
            live["results"].append(ref)
        else:
            result["quarantined"] = True  # Saved report never contributes to release verdict.
        event(live, "qa_run_finished", run_id=run_id, status=result["status"])
        save(root, doc, live)
    return {"run_id": run_id, "result": result_path, "status": result["status"], "quarantined": result.get("quarantined", False)}


def report(root, doc):
    s = load(root, doc)
    p = verify_authorization(root, doc, s)
    candidate = s["authorization"]["candidate_key"]
    latest = {}; runs_with_errors = []
    for ref in s["results"]:
        if ref["candidate_key"] != candidate:
            continue
        verify_file(root, ref)
        data = json_read(safe_path(root, ref["path"], exists=True))
        if data["aborted"]:
            runs_with_errors.append(data["run_id"])
            for check_id in data["selected"]:
                latest[check_id] = {"id": check_id, "status": "ERROR", "error": data["aborted"]}
            continue
        for row in data["checks"]:
            # Evidence changed afterwards is not silently accepted as stable proof.
            for ev in [row.get("log"), row.get("raw_report"), *row.get("evidence", [])]:
                if ev:
                    verify_file(root, ev)
            latest[row["id"]] = row
    manual = {}
    for m in s["manual_results"]:
        if m["candidate_key"] == candidate:
            for ev in m["evidence"]:
                verify_file(root, ev)
            manual[m["id"]] = m
    required = {c["id"] for c in p["checks"]}
    required_manual = {c["id"] for c in p["manual_checks"]}
    missing = sorted(required - latest.keys())
    failures = sorted(k for k,v in latest.items() if v["status"] != "PASS")
    pending_manual = sorted(required_manual - manual.keys())
    manual_fail = sorted(k for k,v in manual.items() if v["status"] != "PASS")
    status = "FAIL" if failures or manual_fail else ("INCOMPLETE" if missing or pending_manual else "PASS")
    return {"release_id": s["id"], "candidate_key": candidate, "status": status, "missing_checks": missing,
            "failed_checks": failures, "pending_manual": pending_manual, "failed_manual": manual_fail,
            "checks": latest, "manual": manual, "aborted_runs": runs_with_errors,
            "meaning": "Executed coverage only; not an automatic publication authorization"}


def manual(root, doc, check_id, status, evidence, decision):
    with lock(root, doc):
        s = load(root, doc); p = verify_authorization(root, doc, s)
        require(check_id in {x["id"] for x in p["manual_checks"]}, "Unknown manual check")
        require(status in ("PASS", "FAIL"), "Manual status must be PASS or FAIL")
        string(decision, "human assessment/decision reference")
        require(evidence, "Manual check requires recorded evidence")
        row = {"id": check_id, "status": status, "decision": decision, "evidence": [record_file(root, f) for f in evidence],
               "candidate_key": s["authorization"]["candidate_key"], "at": now()}
        s["manual_results"].append(row); save(root, doc, s)
        return row


def complete(root, doc):
    with lock(root, doc):
        r = report(root, doc)
        require(r["status"] == "PASS", "Required coverage/evidence not complete")
        s = load(root, doc)
        require(not any(x["status"] == "ACTIVE" for x in s["runs"].values()), "Work still active")
        s["status"] = "QA_COMPLETE"; event(s, "qa_complete", candidate=r["candidate_key"])
        save(root, doc, s)
        return {"status": "QA_COMPLETE", "published": False}


def reopen_scripts(root, doc, decision):
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        require(s["kind"] == "release" and s["status"] not in ("BLOCKED", "CANCELLED"), "Release not editable")
        require(not any(r["status"] == "ACTIVE" for r in s["runs"].values()), "Settle active work first")
        string(decision, "script repair reason; accepted Designer plan remains unchanged")
        s["script_handoff"] = None; s["authorization"] = None; s["status"] = "PREPARING"
        s["completed"].pop("qa_scripts", None)
        event(s, "scripts_reopened", decision=decision); save(root, doc, s)
        return {"status": s["status"], "next": "qa_scripts", "qa_plan_preserved": True}


def request_fix(root, doc, paths, decision):
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        require(s["kind"] == "release" and s["prepared"] and s["status"] not in ("BLOCKED", "CANCELLED"), "Release not ready for repair")
        require(not any(r["status"] == "ACTIVE" for r in s["runs"].values()), "Settle active work first")
        string(decision, "repair reason and authorization")
        owned = [p for t in s["inputs"] for p in t["owners"]["code_builder"]]
        require(paths and all(within(p, owned) for p in paths), "Repair paths must belong to pinned tasks' code ownership; replan wider changes")
        for p in paths: safe_path(root, p)
        s["fix_request"] = {"paths": paths, "decision": decision}
        s["authorization"] = None; s["status"] = "PREPARING"
        event(s, "product_repair_requested", paths=paths, decision=decision); save(root, doc, s)
        return s["fix_request"]


def set_qa_scope(root, doc, paths, decision):
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        require(s["kind"] == "release" and not s["script_handoff"] and s["status"] not in ("BLOCKED", "CANCELLED"), "Set QA scope before script handoff")
        require(not any(r["status"] == "ACTIVE" for r in s["runs"].values()), "Settle active work first")
        string(decision, "scope decision")
        require(paths, "Need an explicit QA write scope")
        for p in paths:
            safe_path(root, p)
            require(p not in (".", "") and not p.startswith((".git", ".codex", "tasks", "versions", ".harness")), "Unsafe QA scope")
        s["qa_paths"] = paths
        event(s, "qa_scope_set", paths=paths, decision=decision); save(root, doc, s)
        return {"qa_paths": paths}
