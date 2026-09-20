#!/usr/bin/env python3
"""GameDev Harness 1.1: one local entry point; models decide, this tool records/checks."""
from __future__ import annotations
import argparse
from pathlib import Path
import sys
sys.dont_write_bytecode = True
if sys.version_info < (3, 11):
    sys.exit("Python 3.11 or later is required")
from harnesslib import engine, workspace, models, release
from harnesslib.common import *


def parser():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--repo", type=Path, default=Path.cwd(), help="Current isolated repository/worktree")
    sub = p.add_subparsers(dest="command", required=True)
    w = sub.add_parser("workspace", help="Plan/create/list/check isolated worktrees")
    w.add_argument("arguments", nargs=argparse.REMAINDER)
    for op in ("status", "plan", "dispatch", "check", "accept", "block", "resume", "stopped", "cancel", "approve", "observe"):
        q = sub.add_parser(op)
        q.add_argument("--doc", required=True)
        if op == "dispatch": q.add_argument("--step", choices=engine.STEPS, required=True)
        if op in ("check", "stopped", "observe"): q.add_argument("--run", required=True)
        if op == "accept": q.add_argument("--handoff", type=Path, required=True)
        if op in ("block", "cancel"): q.add_argument("--reason", required=True)
        if op in ("resume", "stopped", "approve"): q.add_argument("--decision", required=True)
        if op == "approve":
            q.add_argument("--scope", action="append", choices=("preview", "concept", "build"), required=True)
            q.add_argument("--artifact", action="append", default=[])
        if op == "observe":
            q.add_argument("--model", required=True); q.add_argument("--effort", default=None); q.add_argument("--evidence", required=True)
    task = sub.add_parser("task").add_subparsers(dest="operation", required=True)
    tc = task.add_parser("contract"); tc.add_argument("--doc", required=True); tc.add_argument("--input", type=Path, required=True); tc.add_argument("--decision", required=True)
    tn = task.add_parser("note"); tn.add_argument("--doc", required=True); tn.add_argument("--text", required=True)
    rel = sub.add_parser("release").add_subparsers(dest="operation", required=True)
    for op in ("prepare", "authorize", "reopen-scripts", "complete", "fix-request", "scope"):
        r = rel.add_parser(op); r.add_argument("--doc", required=True)
        if op == "prepare": r.add_argument("--inputs", type=Path, required=True)
        if op in ("fix-request", "scope"): r.add_argument("--path", action="append", required=True)
        if op in ("authorize", "reopen-scripts", "fix-request", "scope"): r.add_argument("--decision", required=True)
    qa = sub.add_parser("qa").add_subparsers(dest="operation", required=True)
    qr = qa.add_parser("run"); qr.add_argument("--doc", required=True); qr.add_argument("--checks", nargs="+"); qr.add_argument("--apply", action="store_true")
    qp = qa.add_parser("report"); qp.add_argument("--doc", required=True)
    qm = qa.add_parser("manual"); qm.add_argument("--doc", required=True); qm.add_argument("--id", required=True); qm.add_argument("--status", choices=("PASS","FAIL"),required=True); qm.add_argument("--evidence", action="append", required=True); qm.add_argument("--decision", required=True)
    ms = sub.add_parser("models").add_subparsers(dest="operation", required=True)
    ms.add_parser("show")
    mu = ms.add_parser("update-agents")
    mu.add_argument("--input", type=Path, required=True)
    mu.add_argument("--etag", required=True)
    for op in ("preview", "apply"):
        m = ms.add_parser(op); m.add_argument("--input", type=Path); m.add_argument("--reconcile", action="store_true")
        if op == "apply": m.add_argument("--etag", required=True)
    mr = ms.add_parser("rollback"); mr.add_argument("--transaction", required=True); mr.add_argument("--apply", action="store_true")
    ms.add_parser("recover")
    db = sub.add_parser("dashboard"); db.add_argument("--port", type=int, default=8765)
    sub.add_parser("workflow-table", help="Generate human-readable task states and step table from implementation")
    return p


def route(a):
    root = a.repo.resolve(); cmd = a.command
    if cmd == "workspace":
        return workspace.main(["--repo", str(root), *a.arguments]), True
    if cmd == "dashboard":
        from harnesslib.dashboard import serve
        serve(root, a.port); return None, False
    if cmd == "workflow-table":
        return {"task_states": engine.TASK_STATES, "steps": engine.STEPS}, False
    if cmd == "models":
        if a.operation == "show": return models.status(root), False
        if a.operation == "update-agents":
            payload = json_read(a.input)
            return models.update_agents(root, payload.get("agents"), a.etag), False
        if a.operation in ("preview", "apply"):
            text = a.input.read_text(encoding="utf-8") if a.input else None
            if a.operation == "preview":
                data = models.preview(root, text, reconcile=a.reconcile)
                return {k:data[k] for k in ("etag","diff","config")}, False
            return models.apply(root, text, a.etag, reconcile=a.reconcile), False
        if a.operation == "rollback":
            require(a.apply, "Rollback changes files; pass --apply after inspecting the transaction")
            return models.rollback(root, a.transaction), False
        return models.recover(root), False
    if cmd == "task":
        if a.operation == "contract":
            return engine.update_contract(root, a.doc, json_read(a.input), a.decision), False
        with lock(root, a.doc):
            s = load(root, a.doc); identity(root, s)
            require("```harness-state" not in a.text, "Do not put another machine block in notes")
            p = safe_path(root, a.doc, exists=True)
            atomic_text(p, p.read_text(encoding="utf-8") + "\n" + a.text + "\n")
            return {"noted": True, "task_revision": s.get("task_revision"), "invalidated": False}, False
    if cmd == "release":
        if a.operation == "prepare": return release.prepare(root, a.doc, json_read(a.inputs)), False
        if a.operation == "authorize": return release.authorize(root, a.doc, a.decision), False
        if a.operation == "reopen-scripts": return release.reopen_scripts(root, a.doc, a.decision), False
        if a.operation == "fix-request": return release.request_fix(root, a.doc, a.path, a.decision), False
        if a.operation == "scope": return release.set_qa_scope(root, a.doc, a.path, a.decision), False
        return release.complete(root, a.doc), False
    if cmd == "qa":
        if a.operation == "run": return release.run(root, a.doc, a.checks, apply=a.apply), False
        if a.operation == "report": return release.report(root, a.doc), False
        return release.manual(root, a.doc, a.id, a.status, a.evidence, a.decision), False
    if cmd in ("status", "plan", "check"):
        s = load(root, a.doc); identity(root, s)
        if cmd == "status": return s, False
        if cmd == "check": return engine.check_run(root, s, a.run), False
        return (engine.task_plan(root, s) if s["kind"] == "task" else release.plan(root, s)), False
    if cmd == "dispatch": return engine.dispatch(root, a.doc, a.step), False
    if cmd == "accept": return engine.accept(root, a.doc, json_read(a.handoff)), False
    if cmd == "approve": return engine.approve(root, a.doc, a.scope, a.artifact, a.decision), False
    if cmd == "observe": return engine.observe(root, a.doc, a.run, a.model, a.effort, a.evidence), False
    return engine.change_control(root, a.doc, cmd, reason=getattr(a,"reason",None), decision=getattr(a,"decision",None), run_id=getattr(a,"run",None)), False


def main(argv=None):
    a = parser().parse_args(argv)
    try:
        result, already_printed = route(a)
        if already_printed:
            return result
        if result is not None:
            print(dump(result), end="")
        if a.command == "qa" and isinstance(result, dict) and result.get("status") in ("FAIL", "ERROR", "INCOMPLETE"):
            return 3
        return 0
    except (HarnessError, workspace.WorkspaceError, OSError, ValueError, KeyError, TypeError) as e:
        print(dump({"error": str(e)}), file=sys.stderr, end="")
        return 2

if __name__ == "__main__":
    raise SystemExit(main())
