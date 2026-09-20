"""Task state machine and shared dispatch lifecycle. No model calls or process termination."""
from __future__ import annotations
import copy
import re
from pathlib import Path
from .common import *
from . import workspace as ws
from . import models

TASK_STATES = ("DRAFT", "READY", "BUILDING", "BLOCKED", "READY_FOR_RELEASE", "CANCELLED")
STEPS = {
    "designer": {"role": "feature_designer", "requires": [], "output": "interface_notes"},
    "design": {"role": "design_art_agent", "requires": ["designer when requested"], "output": "visual_mapping"},
    "code": {"role": "code_builder", "requires": ["build authorization"], "output": "implementation_facts"},
    "art": {"role": "design_art_agent", "requires": ["build authorization"], "output": "assets"},
    "integration": {"role": "code_builder", "requires": ["code when needed", "art"], "output": "binding_notes"},
    "interfaces": {"role": "feature_designer", "requires": ["release prepared"], "output": "qa_plan"},
    "release_fix": {"role": "code_builder", "requires": ["PM repair request"], "output": "implementation_facts"},
    "qa_scripts": {"role": "code_builder", "requires": ["accepted Designer QA plan"], "output": "qa_execution"},
    "qa_report": {"role": "qa_reporter", "requires": ["runner results"], "output": "report"},
}


def starter_contract():
    return {"goal": "", "qa_intent": [], "technical_design_required": False,
            "visual_impact": "none", "needs_code": True, "needs_art": False,
            "owners": {"code_builder": [], "design_art_agent": []},
            "references": [], "dependencies": [], "shared_touchpoints": [],
            "interface_map": [], "asset_contract": [],
            "runtime_isolation": {"status": "unconfigured"},
            "generation_budget": {"limit": None, "decision": "Not yet agreed"}}


def initialize(root, plan):
    s = {"schema_version": 1, "kind": plan["kind"], "id": plan["id"],
         "workspace": str(Path(root).resolve()), "branch": plan["branch"],
         "base_commit": plan["base_commit"], "runs": {}, "history": []}
    if s["kind"] == "task":
        s.update(task_revision=1, status="DRAFT", contract=starter_contract(), approvals={},
                 artifacts={}, completed={}, block=None)
    else:
        s.update(status="PREPARING", inputs=[], input_digest=None, integrated_commit=None,
                 prepared=False, qa_plan_handoff=None, script_handoff=None, authorization=None, results=[],
                 qa_paths=["tests/release", f"releases/{plan["id"]}"], fix_request=None,
                 manual_results=[], block=None, completed={})
    event(s, "created")
    p = safe_path(root, plan["document"])
    # Workspace creation only: fresh file, no edits or migration of existing records.
    require(not p.exists(), "Refuse to initialize an existing document")
    # Preserve human template sections (including publication/rollback notes).
    text = safe_path(root, plan["template"], exists=True).read_text(encoding="utf-8")
    parse_doc(text)
    pattern = re.compile(r"^```harness-state\n.*?\n```[ \t]*$", re.M | re.S)
    text = pattern.sub(lambda m: "```harness-state\n" + dump(s).rstrip() + "\n```", text, count=1)
    text = re.sub(r"^# .*", lambda m: "# " + plan["title"], text, count=1)
    atomic_text(p, text)
    return s


def validate_contract(root, c, *, ready=False):
    require(isinstance(c, dict) and set(c) == set(starter_contract()), "Contract must use exactly the documented fields")
    require(isinstance(c["goal"], str), "goal must be text")
    for k in ("needs_code", "needs_art", "technical_design_required"):
        require(type(c[k]) is bool, f"{k} must be boolean")
    require(c["needs_code"] or c["needs_art"], "A task must deliver code or art")
    require(c["visual_impact"] in ("none", "asset", "screen", "major"), "Invalid visual_impact")
    require(not c["needs_art"] or c["visual_impact"] != "none", "Art production needs an explicit visual impact")
    require(set(c["owners"]) == {"code_builder", "design_art_agent"}, "Define both ownership lists")
    for role, paths in c["owners"].items():
        require(isinstance(paths, list), "Owned paths must be a list")
        for p in paths:
            safe_path(root, p)
            require(p not in (".", "") and not p.startswith((".harness", "tasks", "versions", ".codex", "tools/harness")),
                    "Product ownership may not include harness state/config or the entire repository")
    for a in c["owners"]["code_builder"]:
        for b in c["owners"]["design_art_agent"]:
            require(not within(a, [b]) and not within(b, [a]), "Code and art ownership overlap; assign one writer")
    require(isinstance(c["qa_intent"], list), "QA Intent must be a list")
    ids = []
    for item in c["qa_intent"]:
        require(set(item) == {"id", "text", "source"}, "Intent has id, text, source only")
        string(item["text"], "intent text"); string(item["source"], "intent source")
        require(__import__('re').fullmatch(r"[A-Za-z0-9][A-Za-z0-9._-]{0,63}", item["id"]), "Invalid intent id")
        ids.append(item["id"])
    require(len(ids) == len(set(ids)), "Duplicate QA Intent ids")
    require(isinstance(c["asset_contract"], list), "asset_contract must be a list")
    asset_ids = [x.get("id") for x in c["asset_contract"] if isinstance(x, dict)]
    require(len(asset_ids) == len(c["asset_contract"]) and all(isinstance(x, str) and x for x in asset_ids) and len(asset_ids) == len(set(asset_ids)), "Asset specs need unique IDs")
    for r in c["references"]:
        verify_file(root, r)
    for dep in c["dependencies"]:
        require(all(dep.get(k) for k in ("task_id", "task_revision", "commit")), "Pin dependency task/revision/commit")
        require(ws.resolve_commit(Path(root), dep["commit"]) == dep["commit"], "Use full dependency commit hashes")
    for point in c["shared_touchpoints"]:
        require(all(isinstance(point.get(k), str) for k in ("resource", "region", "coordinator", "resolution")),
                "Shared touchpoint needs resource, region, coordinator, resolution")
        if ready:
            string(point["coordinator"], "shared coordinator"); string(point["resolution"], "shared resolution")
    if ready:
        string(c["goal"], "goal")
        require(c["qa_intent"], "PM must record at least one sourced QA Intent")
        if c["needs_code"] or c["needs_art"]:
            # Even art-only tasks require code-side binding ownership.
            require(c["owners"]["code_builder"], "Define code / final-binding ownership")
        if c["needs_art"]:
            require(c["owners"]["design_art_agent"] and c["asset_contract"], "Art needs paths and production specifications")
        if c["visual_impact"] in ("screen", "major"):
            require(c["references"], "Real gameplay reference (or explicitly approved replacement) is required")


def check_inputs(root, s):
    for r in s["contract"]["references"]:
        verify_file(root, r)
    for approval in s["approvals"].values():
        for aid in approval.get("artifacts", []):
            require(aid in s["artifacts"], "Approval references missing artifact")
            verify_file(root, s["artifacts"][aid])


def verify_art_outputs(root, s):
    if not done(s, "art"):
        return
    from .common import json_read
    r = s["runs"][s["completed"]["art"]]
    manifests = [a for a in r["artifacts"] if a["kind"] == "asset_manifest"]
    require(manifests, "Missing generated art manifest")
    for a in manifests:
        verify_file(root, a)
        m = json_read(safe_path(root, a["path"], exists=True))
        for item in m["assets"]:
            if item["action"] == "delete":
                require(not safe_path(root, item["path"]).exists(), "Deleted asset reappeared before binding")
            else:
                verify_file(root, item)


def visual_scopes(s):
    c = s["contract"]
    scopes = []
    if c["visual_impact"] in ("screen", "major"):
        scopes.append("preview")
    if c["needs_art"]:
        scopes.append("concept")
    return scopes


def done(s, step):
    return step in s["completed"]


def active(s, step=None):
    return [r for r in s["runs"].values() if r["status"] == "ACTIVE" and (step is None or r["step"] == step)]


def task_plan(root, s):
    require(s["status"] in TASK_STATES, "Unknown task state")
    if s["status"] in ("BLOCKED", "CANCELLED"):
        return {"status": s["status"], "next": [], "waiting": s.get("block")}
    try:
        validate_contract(root, s["contract"], ready=True)
        check_inputs(root, s)
    except HarnessError as e:
        return {"status": s["status"], "next": [], "waiting": str(e)}
    if s["contract"]["technical_design_required"] and not done(s, "designer"):
        candidates = ["designer"]
    elif visual_scopes(s) and not done(s, "design"):
        candidates = ["design"]
    else:
        missing = [x for x in [*visual_scopes(s), "build"] if x not in s["approvals"]]
        if missing:
            return {"status": s["status"], "next": [], "waiting": {"user_approval": missing}}
        candidates = []
        if s["contract"]["needs_code"] and not done(s, "code"):
            candidates.append("code")
        if s["contract"]["needs_art"] and not done(s, "art"):
            candidates.append("art")
        if not candidates and s["contract"]["needs_art"] and not done(s, "integration"):
            candidates = ["integration"]
    waiting_runs = [r["run_id"] for r in active(s)]
    return {"status": s["status"], "next": [x for x in candidates if not active(s, x)],
            "in_flight": waiting_runs, "game_qa": "NOT_RUN"}


def refresh(root, s):
    if s["status"] in ("BLOCKED", "CANCELLED"):
        return
    p = task_plan(root, s)
    if p.get("waiting"):
        s["status"] = "DRAFT"
        return
    build_steps = ["code"] if s["contract"]["needs_code"] else []
    if s["contract"]["needs_art"]:
        build_steps += ["art", "integration"]
    if all(done(s, x) for x in build_steps):
        s["status"] = "READY_FOR_RELEASE"
    elif "build" in s["approvals"]:
        started = any(r["step"] in build_steps and r["status"] in ("ACTIVE", "ACCEPTED") for r in s["runs"].values())
        s["status"] = "BUILDING" if started else "READY"
    else:
        s["status"] = "DRAFT"


def _block(s, reason, needs_user=True):
    old = s["status"]
    for r in active(s):
        r.update(status="REVOKED", stop_confirmed=False)
    s["block"] = {"reason": string(reason, "block reason"), "needs_user": needs_user,
                  "previous_status": old, "at": now(), "stop_request": "PM must cancel through host; not automatically sent"}
    s["status"] = "BLOCKED"
    event(s, "blocked", reason=reason)


def update_contract(root, doc, contract, decision):
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        require(s["kind"] == "task" and s["status"] != "CANCELLED", "Not an editable task")
        validate_contract(root, contract)
        require(not active(s), "Pause/revoke active work before changing its inputs")
        require(all(r.get("stop_confirmed", True) for r in s["runs"].values() if r["status"] == "REVOKED"),
                "Old jobs have not been confirmed stopped")
        string(decision, "decision/source")
        if digest(contract) == digest(s["contract"]):
            return {"changed": False, "task_revision": s["task_revision"]}
        s["task_revision"] += 1
        s["contract"] = copy.deepcopy(contract)
        # Conservative invalidation for semantic contract edits; notes and receipts do not trigger it.
        s["approvals"] = {}; s["completed"] = {}
        if s["status"] != "BLOCKED":
            s["status"] = "DRAFT"
        event(s, "contract_changed", revision=s["task_revision"], decision=decision,
              invalidation="all dependent task completions; unchanged history/artifacts retained")
        save(root, doc, s)
        return {"changed": True, "task_revision": s["task_revision"], "approval": "required again"}


def approve(root, doc, scopes, artifacts, decision):
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        require(s["kind"] == "task" and s["status"] not in ("BLOCKED", "CANCELLED"), "Task not approvable")
        require(not active(s), "Do not alter approval records during active work; pause and settle first")
        validate_contract(root, s["contract"], ready=True)
        string(decision, "actual user decision reference")
        require(scopes and set(scopes) <= {"preview", "concept", "build"}, "Invalid approval scope")
        require(not s["contract"]["technical_design_required"] or done(s, "designer"), "Designer handoff missing")
        for scope in scopes:
            if scope != "build":
                require(scope in visual_scopes(s) and done(s, "design"), "Visual handoff not ready")
                selected = [a for a in artifacts if a in s["artifacts"] and s["artifacts"][a]["kind"] == scope]
                require(selected, f"Select a concrete {scope} artifact")
                for aid in selected:
                    r = s["artifacts"][aid]
                    require(r["task_revision"] == s["task_revision"], "Artifact belongs to older task revision")
                    verify_file(root, r)
                s["approvals"][scope] = {"artifacts": selected, "task_revision": s["task_revision"], "decision": decision}
        if "build" in scopes:
            if s["contract"]["needs_art"]:
                mapping = s["runs"][s["completed"]["design"]]["payload"]["visual_mapping"]["asset_concepts"]
                approved = {s["artifacts"][a]["path"] for a in s["approvals"].get("concept", {}).get("artifacts", [])}
                require(all(x["concept_path"] in approved for x in mapping), "Not all required asset concepts were approved")
            require(all(x in s["approvals"] for x in visual_scopes(s)), "Approve required previews/concepts before authorizing build")
            s["approvals"]["build"] = {"task_revision": s["task_revision"], "contract_digest": digest(s["contract"]),
                                         "decision": decision, "artifacts": []}
        refresh(root, s); event(s, "approved", scopes=scopes, decision=decision)
        save(root, doc, s)
        return {"status": s["status"], "approvals": s["approvals"]}


def snapshot_identity(s):
    if s["kind"] == "task":
        return {"task_revision": s["task_revision"], "contract_digest": digest(s["contract"])}
    return {"input_digest": s["input_digest"], "integrated_commit": s["integrated_commit"]}


def allowed_paths(s, step):
    if step in ("designer", "interfaces", "qa_report"):
        return []
    if s["kind"] == "release":
        if step == "release_fix":
            return s["fix_request"]["paths"]
        return s["qa_paths"]
    if step == "design":
        return [f".harness/{x}/{s['id']}" for x in ("previews", "concepts", "references")]
    if step == "art":
        return s["contract"]["owners"]["design_art_agent"]
    return s["contract"]["owners"]["code_builder"]


def context_packet(s, step):
    """Derived handoff context, not another editable state document; excludes full history."""
    if s["kind"] == "task":
        needed = {"designer": [], "design": ["designer"], "code": ["designer", "design"],
                  "art": ["design"], "integration": ["code", "art"]}.get(step, [])
        return {"task_revision": s["task_revision"], "contract": s["contract"],
                "approved_artifacts": {a: s["artifacts"][a] for ap in s["approvals"].values() for a in ap.get("artifacts", [])},
                "prior_handoffs": {k: {"payload": s["runs"][s["completed"][k]]["payload"],
                                        "artifacts": s["runs"][s["completed"][k]]["artifacts"]}
                                    for k in needed if k in s["completed"]}}
    if step == "qa_report":
        return {"candidate": s["authorization"], "qa_plan_handoff": s.get("qa_plan_handoff"),
                "script_handoff": s["script_handoff"], "results": s["results"],
                "instruction": "Read only current-candidate reports and necessary evidence"}
    return {"input_digest": s["input_digest"], "backlog": f"releases/{s['id']}/QA_BACKLOG.md",
            "inputs": s["inputs"], "qa_plan_handoff": s.get("qa_plan_handoff"),
            "script_handoff": s["script_handoff"],
            "fix_request": s.get("fix_request")}


def dispatch(root, doc, step):
    require(step in STEPS, "Unknown step")
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        if s["kind"] == "task":
            p = task_plan(root, s)
        else:
            from .release import plan as release_plan
            p = release_plan(root, s)
        require(step in p.get("next", []), f"Step not eligible: {step}; {dump(p)}")
        model = models.dispatch_target(root, STEPS[step]["role"])
        if s["kind"] == "task" and step == "integration":
            verify_art_outputs(root, s)
        r = {"run_id": uid(), "step": step, "role": STEPS[step]["role"], "status": "ACTIVE",
             "created_at": now(), "inputs": snapshot_identity(s), "model_target": model,
             "model_observed": None, "allowed_paths": allowed_paths(s, step), "stop_confirmed": False}
        if s["kind"] == "task":
            check_inputs(root, s)
            r["approved_inputs"] = copy.deepcopy(s["approvals"])
        s["runs"][r["run_id"]] = r
        if s["kind"] == "task":
            refresh(root, s)
        event(s, "dispatched", run_id=r["run_id"], step=step)
        save(root, doc, s)
        return {"document": doc, "workspace": str(root), "id": s["id"], **r,
                "context": context_packet(s, step),
                "host_action_required": "PM starts the registered agent; this command does not spawn a process"}


def check_run(root, s, run_id):
    identity(root, s)
    require(run_id in s["runs"], "Unregistered run")
    r = s["runs"][run_id]
    require(r["status"] == "ACTIVE" and s["status"] not in ("BLOCKED", "CANCELLED"), "Run revoked/completed or task paused")
    require(r["inputs"] == snapshot_identity(s), "Run inputs are stale")
    if s["kind"] == "task":
        check_inputs(root, s)
        require(r["approved_inputs"] == s["approvals"], "Approval changed after dispatch")
        if r["step"] == "integration":
            verify_art_outputs(root, s)
    return r


def accept(root, doc, handoff):
    require(isinstance(handoff, dict), "Handoff must be an object")
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        run_id = handoff.get("run_id")
        require(run_id in s["runs"], "Unregistered return")
        r = s["runs"][run_id]
        if r["status"] == "ACCEPTED":
            require(r["inputs"] == snapshot_identity(s), "Previously accepted result is from an old revision")
            require(r["handoff_digest"] == digest(handoff), "Conflicting second return for an accepted run")
            return {"idempotent": True, "status": s["status"], "run_id": run_id}
        check_run(root, s, run_id)
        require(handoff.get("role") == r["role"], "Wrong returning role")
        if handoff.get("status") in ("NEEDS_CLARIFICATION", "BLOCKED"):
            _block(s, string(handoff.get("reason"), "reason"), needs_user=True)
            save(root, doc, s)
            return {"status": "BLOCKED", "waiting_for": "USER_DECISION_VIA_PM"}
        require(handoff.get("status") == "READY", "Use READY, NEEDS_CLARIFICATION, or BLOCKED")
        require(handoff.get("game_qa", "NOT_RUN") == "NOT_RUN", "Agent handoff is not executed game QA")
        string(handoff.get("summary"), "handoff summary")
        payload = handoff.get("payload", {})
        require(isinstance(payload, dict) and STEPS[r["step"]]["output"] in payload, "Missing required step output")
        artifacts = []
        for item in handoff.get("artifacts", []):
            path = item.get("path", "")
            require(within(path, r["allowed_paths"]), f"Returned artifact outside step ownership: {path}")
            artifacts.append(record_file(root, path, artifact_id=uid("artifact"), kind=item.get("kind", "file"),
                                         run_id=run_id, task_revision=s.get("task_revision")))
        if s["kind"] == "task":
            if r["step"] == "design":
                require(all(any(x["kind"] == scope for x in artifacts) for scope in visual_scopes(s)),
                        "Visual handoff must include the required full preview and asset concepts")
                if s["contract"]["visual_impact"] in ("screen", "major"):
                    require(any(x["kind"] in ("editable_source", "composition_recipe") for x in artifacts),
                            "Screen preview needs an editable source or reproducible composition recipe")
                if s["contract"]["needs_art"]:
                    mapping = payload["visual_mapping"]
                    require(isinstance(mapping, dict) and isinstance(mapping.get("asset_concepts"), list),
                            "Map every production asset ID to its concept path")
                    mapping = mapping["asset_concepts"]
                    expected = {x["id"] for x in s["contract"]["asset_contract"]}
                    require(len(mapping) == len(expected) and {x.get("asset_id") for x in mapping} == expected,
                            "Asset/concept coverage missing or duplicated")
                    concept_paths = {x["path"] for x in artifacts if x["kind"] == "concept"}
                    require(all(x.get("concept_path") in concept_paths for x in mapping), "Concept mapping refers to nonreturned artifact")
            if r["step"] == "art":
                from .manifest import generate
                manifest = generate(root, s, payload["assets"], run_id)
                artifacts.append(manifest)
            for a in artifacts:
                s["artifacts"][a["artifact_id"]] = a
        else:
            from .release import accept_payload
            accept_payload(root, s, r, payload, artifacts)
        r.update(status="ACCEPTED", handoff_digest=digest(handoff), accepted_at=now(), stop_confirmed=True,
                 payload=copy.deepcopy(payload), summary=handoff["summary"], artifacts=artifacts)
        s["completed"][r["step"]] = run_id
        if s["kind"] == "task":
            refresh(root, s)
        event(s, "accepted", run_id=run_id)
        save(root, doc, s)
        return {"run_id": run_id, "status": s["status"], "artifacts": artifacts, "game_qa": "NOT_RUN"}


def change_control(root, doc, action, *, reason=None, decision=None, run_id=None):
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        if action == "block":
            require(s["status"] not in ("BLOCKED", "CANCELLED"), "Already blocked/cancelled")
            _block(s, string(reason, "reason"))
        elif action == "stopped":
            require(run_id in s["runs"] and s["runs"][run_id]["status"] == "REVOKED", "Only revoked jobs need stop confirmation")
            string(decision, "host stop evidence")
            s["runs"][run_id].update(stop_confirmed=True, stop_evidence=decision)
            event(s, "host_stop_recorded", run_id=run_id, evidence=decision)
        elif action == "resume":
            require(s["status"] == "BLOCKED", "Task/release is not blocked")
            string(decision, "user decision reference")
            require(all(r.get("stop_confirmed", False) for r in s["runs"].values() if r["status"] == "REVOKED"),
                    "Cannot reuse workspace while revoked jobs may still be writing")
            s["status"] = "DRAFT" if s["kind"] == "task" else "PREPARING"
            s["block"] = None
            if s["kind"] == "task":
                refresh(root, s)
            else:
                s["authorization"] = None
            event(s, "resumed", decision=decision)
        elif action == "cancel":
            _block(s, string(reason, "cancellation reason"))
            s["status"] = "CANCELLED"
        else:
            raise HarnessError("Unknown control operation")
        save(root, doc, s)
        return {"status": s["status"], "block": s.get("block"), "runs": {k: v["status"] for k,v in s["runs"].items()}}


def observe(root, doc, run_id, model, effort, evidence):
    with lock(root, doc):
        s = load(root, doc); identity(root, s)
        require(run_id in s["runs"], "Unknown run")
        string(model, "host-reported model"); string(evidence, "host evidence reference")
        s["runs"][run_id]["model_observed"] = {"model": model, "effort": effort, "evidence": evidence, "at": now()}
        save(root, doc, s)
        return s["runs"][run_id]["model_observed"]
