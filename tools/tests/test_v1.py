"""Synthetic harness tests; no real game or model API. Run explicitly, not per feature task."""
from __future__ import annotations
import argparse
import copy
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import threading
import tomllib
import unittest
import urllib.request
import urllib.error

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
from harnesslib.common import *
from harnesslib import engine as en, models as mo, workspace as ws, release as rel, manifest as mf, dashboard as db


def cli(root, *args):
    return subprocess.run([sys.executable, str(ROOT / "tools/harness.py"), "--repo", str(root), *args],
                          text=True, capture_output=True, timeout=30)


class PackageTests(unittest.TestCase):
    def test_four_roles_and_single_designer(self):
        files = sorted((ROOT/".codex/agents").glob("*.toml"))
        self.assertEqual(len(files),4)
        roles = {tomllib.loads(p.read_text(encoding="utf-8"))["name"] for p in files}
        self.assertEqual(roles,set(mo.ROLES))
        designer = tomllib.loads((ROOT/".codex/agents/feature-designer.toml").read_text(encoding="utf-8"))
        self.assertEqual(designer["sandbox_mode"],"read-only")
        self.assertFalse(designer["agents"]["enabled"])
    def test_no_old_executable_entrypoints(self):
        self.assertFalse((ROOT/"tools/harness_gate.py").exists())
        self.assertFalse((ROOT/"tools/harness_workflow.py").exists())
        self.assertFalse((ROOT/".codex/agents/workflow-monitor.toml").exists())
    def test_six_task_states(self):
        self.assertEqual(len(en.TASK_STATES),6)
        self.assertNotIn("Verified",en.TASK_STATES)
    def test_qa_responsibility_split_without_new_role_or_mode(self):
        self.assertEqual(en.STEPS["interfaces"]["role"],"feature_designer")
        self.assertEqual(en.STEPS["interfaces"]["output"],"qa_plan")
        self.assertEqual(en.STEPS["qa_scripts"]["role"],"code_builder")
        self.assertEqual(en.STEPS["qa_scripts"]["output"],"qa_execution")
        self.assertEqual(len(mo.ROLES),4)
        self.assertEqual(set(mo.ROLE_PRESENTATION),set(mo.ROLES))
    def test_model_defaults_honest(self):
        s=mo.status(ROOT)
        self.assertTrue(all(r["synced"] for r in s["rows"]))
        self.assertTrue(all(r["target"]["model"] is None for r in s["rows"]))
        self.assertFalse(s["config"]["agents"]["qa_reporter"]["enabled"])
    def test_templates_and_cli(self):
        for p in (ROOT/"tasks/_TEMPLATE.md",ROOT/"versions/_TEMPLATE.md"):
            parse_doc(p.read_text(encoding="utf-8"))
        self.assertEqual(cli(ROOT,"--help").returncode,0)


class RepoTest(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.seedtmp=tempfile.TemporaryDirectory(prefix="harness-v1-seed-",ignore_cleanup_errors=(os.name=="nt"))
        cls.addClassCleanup(cls.seedtmp.cleanup)
        cls.seed=Path(cls.seedtmp.name)/"seed"
        shutil.copytree(ROOT,cls.seed,ignore=shutil.ignore_patterns("__pycache__","*.pyc"))
        for args in [("init","-b","main"),("config","user.email","test@example.invalid"),("config","user.name","Harness Test"),
                     ("config","commit.gpgsign","false"),("add","."),("commit","-m","synthetic harness baseline")]:
            ws.git(cls.seed,*args)
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory(prefix="harness-v1-case-",ignore_cleanup_errors=(os.name=="nt"));self.addCleanup(self.temp.cleanup)
        self.parent=Path(self.temp.name);self.repo=self.parent/"游戏 repo"
        subprocess.run(["git","clone","-q","--no-hardlinks",str(self.seed),str(self.repo)],check=True,capture_output=True)
        ws.git(self.repo,"config","user.name","Harness Test");ws.git(self.repo,"config","user.email","test@example.invalid")
        ws.git(self.repo,"config","commit.gpgsign","false")
    def create(self,kind="task",ident="TASK-001",base="main",apply=True):
        a=argparse.Namespace(command=kind,task_id=ident,slug="feature",title=None,version=ident,base=base,root=None,apply=apply)
        p=ws.create(self.repo,a);return Path(p["workspace_root"]),p["document"],p
    def contract(self,root,doc,art=False,designer=False):
        c=en.starter_contract();c.update(goal="Synthetic feature for harness tests",technical_design_required=designer)
        c["qa_intent"]=[{"id":"AC-1","text":"Synthetic value is 42", "source":"synthetic test fixture, not a user approval"}]
        c["owners"]["code_builder"]=["game"]
        if art:
            c.update(needs_art=True,visual_impact="asset")
            c["owners"]["design_art_agent"]=["assets"]
            c["asset_contract"]=[{"id":"icon","purpose":"synthetic icon","width":2,"height":2}]
        en.update_contract(root,doc,c,"synthetic decision")
        return c
    def handoff(self,r,payload,artifacts=None,status="READY"):
        return {"run_id":r["run_id"],"role":r["role"],"status":status,"summary":"synthetic result", "payload":payload,
                "artifacts":artifacts or [],"game_qa":"NOT_RUN"}
    def ready(self,art=False,designer=False):
        root,doc,_=self.create();self.contract(root,doc,art,designer)
        if designer:
            r=en.dispatch(root,doc,"designer");en.accept(root,doc,self.handoff(r,{"interface_notes":[]}))
        if art:
            r=en.dispatch(root,doc,"design")
            f=f".harness/concepts/TASK-001/concept.txt";atomic_text(safe_path(root,f),"synthetic concept")
            ret=en.accept(root,doc,self.handoff(r,{"visual_mapping":{"asset_concepts":[{"asset_id":"icon","concept_path":f}]}},[{"path":f,"kind":"concept"}]))
            en.approve(root,doc,["concept","build"],[ret["artifacts"][0]["artifact_id"]],"synthetic approval")
        else:
            en.approve(root,doc,["build"],[],"synthetic approval")
        return root,doc
    def complete_task(self):
        root,doc=self.ready();r=en.dispatch(root,doc,"code")
        atomic_text(root/"game/value.txt","42")
        en.accept(root,doc,self.handoff(r,{"implementation_facts":{"entry":"game/value.txt"}},[{"path":"game/value.txt"}]))
        ws.git(root,"add",".");ws.git(root,"commit","-m","synthetic implementation")
        return root,doc,ws.output(root,"rev-parse","HEAD")
    def release_world(self,script=None,two=False,manual=False):
        troot,tdoc,commit=self.complete_task()
        root,doc,_=self.create("release","v-test",commit)
        rel.prepare(root,doc,[{"id":"TASK-001","commit":commit,"document":tdoc}])
        designer=en.dispatch(root,doc,"interfaces")
        s=load(root,doc)
        plan={"schema_version":2,"release_id":s["id"],"input_digest":s["input_digest"],"interface_requirements":[],
              "checks":[{"id":"C1","case":"Read the committed synthetic value and compare it with the sourced intent",
                         "fixture_reset":"read-only synthetic committed value; no shared mutations","interface_requirement_ids":[],
                         "assertions":[{"id":"value","expectation":"value equals 42","intent_ids":["TASK-001:AC-1"]}]}],"manual_checks":[]}
        if two:
            c=copy.deepcopy(plan["checks"][0]);c["id"]="C2";plan["checks"].append(c)
        if manual:
            plan["manual_checks"].append({"id":"M1","instruction":"human examines synthetic evidence", "intent_ids":["TASK-001:AC-1"]})
        en.accept(root,doc,self.handoff(designer,{"qa_plan":plan}))
        r=en.dispatch(root,doc,"qa_scripts")
        source=script or '''import json,os\nfrom pathlib import Path\nvalue=Path("game/value.txt").read_text()\nPath(os.environ["HARNESS_RESULT_PATH"]).write_text(json.dumps({"schema_version":1,"run_id":os.environ["HARNESS_RUN_ID"],"check_id":os.environ["HARNESS_CHECK_ID"],"assertions":[{"id":"value","status":"PASS" if value=="42" else "FAIL","actual":value}]}))\n'''
        atomic_text(root/"tests/release/check.py",source)
        execution={"schema_version":2,"release_id":s["id"],"input_digest":s["input_digest"],"qa_plan_digest":digest(plan),
                   "files":["tests/release/check.py"],"checks":[]}
        for check in plan["checks"]:
            execution["checks"].append({"id":check["id"],"command":["{python}","tests/release/check.py"],"timeout_seconds":5,
                                         "fixture_implementation":"read-only file access; no mutable fixture",
                                         "assertion_ids":[a["id"] for a in check["assertions"]]})
        atomic_text(root/"tests/release/execution.json",dump(execution))
        en.accept(root,doc,self.handoff(r,{"qa_execution":"tests/release/execution.json"},[{"path":"tests/release/execution.json"},{"path":"tests/release/check.py"}]))
        ws.git(root,"add",".");ws.git(root,"commit","-m","synthetic QA scripts")
        rel.authorize(root,doc,"synthetic execution authorization")
        return root,doc,{"plan":plan,"execution":execution}


class WorkspaceTests(RepoTest):
    def test_dry_run(self):
        r,d,p=self.create(apply=False);self.assertFalse(r.exists())
    def test_create_and_check(self):
        r,d,p=self.create();self.assertEqual(ws.check_workspace(r)["workspace_identity"],"OK");self.assertEqual(load(r,d)["status"],"DRAFT")
    def test_duplicate_rejected(self):
        self.create()
        with self.assertRaises(ws.WorkspaceError):self.create()
    def test_dirty_source_rejected(self):
        (self.repo/"keep.txt").write_text("mine")
        with self.assertRaises(ws.WorkspaceError):self.create()
        self.assertEqual((self.repo/"keep.txt").read_text(),"mine")
    def test_parallel_file_and_index_isolation(self):
        a,_,_=self.create();b,_,_=self.create(ident="TASK-002")
        (a/"a.txt").write_text("a");ws.git(a,"add","a.txt")
        self.assertFalse((b/"a.txt").exists());self.assertNotIn("a.txt",ws.output(b,"diff","--cached","--name-only"))
    def test_wrong_branch(self):
        r,d,_=self.create();ws.git(r,"switch","-c","other")
        with self.assertRaises(HarnessError):en.dispatch(r,d,"code")
    def test_unregistered_workspace(self):
        r,d,_=self.create();ws.git_path(r,"harness-workspace.json").unlink()
        with self.assertRaises(HarnessError):identity(r,load(r,d))
    def test_unified_workspace_cli(self):
        p=cli(self.repo,"workspace","task","TASK-090","via-cli","--base","main","--apply")
        self.assertEqual(p.returncode,0,p.stderr)
        self.assertEqual(json.loads(p.stdout)["action"],"created; draft is uncommitted; no tests or agents started")


class TaskTests(RepoTest):
    def test_empty_task_not_dispatchable(self):
        r,d,_=self.create();self.assertEqual(en.task_plan(r,load(r,d))["next"],[])
        with self.assertRaises(HarnessError):en.dispatch(r,d,"code")
    def test_build_needs_authorization(self):
        r,d,_=self.create();self.contract(r,d)
        with self.assertRaises(HarnessError):en.dispatch(r,d,"code")
    def test_designer_not_mandatory(self):
        r,d=self.ready();self.assertEqual(en.task_plan(r,load(r,d))["next"],["code"])
    def test_designer_when_requested(self):
        r,d,_=self.create();self.contract(r,d,designer=True)
        self.assertEqual(en.task_plan(r,load(r,d))["next"],["designer"])
        with self.assertRaises(HarnessError):en.approve(r,d,["build"],[],"synthetic")
    def test_code_only_no_extra_binding_step(self):
        r,d,_=self.complete_task();self.assertEqual(load(r,d)["status"],"READY_FOR_RELEASE")
    def test_duplicate_accept_is_idempotent(self):
        r,d=self.ready();run=en.dispatch(r,d,"code");h=self.handoff(run,{"implementation_facts":[]})
        en.accept(r,d,h);before=(r/d).read_text(encoding="utf-8");self.assertTrue(en.accept(r,d,h)["idempotent"]);self.assertEqual(before,(r/d).read_text(encoding="utf-8"))
    def test_conflicting_second_return(self):
        r,d=self.ready();run=en.dispatch(r,d,"code");h=self.handoff(run,{"implementation_facts":[]});en.accept(r,d,h)
        h["summary"]="conflict"
        with self.assertRaises(HarnessError):en.accept(r,d,h)
    def test_parallel_accept_does_not_revoke_other(self):
        r,d=self.ready(art=True);code=en.dispatch(r,d,"code");art=en.dispatch(r,d,"art")
        en.accept(r,d,self.handoff(code,{"implementation_facts":[]}))
        self.assertEqual(en.check_run(r,load(r,d),art["run_id"])["status"],"ACTIVE")
    def test_block_revokes_all_and_resume_requires_stop(self):
        r,d=self.ready(art=True);code=en.dispatch(r,d,"code");art=en.dispatch(r,d,"art")
        en.change_control(r,d,"block",reason="synthetic ambiguity")
        self.assertTrue(all(x["status"]=="REVOKED" for x in load(r,d)["runs"].values() if x["run_id"] in [code["run_id"],art["run_id"]]))
        with self.assertRaises(HarnessError):en.change_control(r,d,"resume",decision="synthetic answer")
    def test_resume_new_run_old_return_rejected(self):
        r,d=self.ready();old=en.dispatch(r,d,"code");en.change_control(r,d,"block",reason="pause")
        en.change_control(r,d,"stopped",run_id=old["run_id"],decision="synthetic host stopped")
        en.change_control(r,d,"resume",decision="synthetic user continue")
        new=en.dispatch(r,d,"code");self.assertNotEqual(new["run_id"],old["run_id"])
        with self.assertRaises(HarnessError):en.accept(r,d,self.handoff(old,{"implementation_facts":[]}))
    def test_artifact_tamper_rejected(self):
        r,d=self.ready(art=True);s=load(r,d);aid=s["approvals"]["concept"]["artifacts"][0]
        (r/s["artifacts"][aid]["path"]).write_text("different")
        with self.assertRaises(HarnessError):en.dispatch(r,d,"code")
    def test_clarification_is_whole_task_block(self):
        r,d=self.ready(art=True);code=en.dispatch(r,d,"code");art=en.dispatch(r,d,"art")
        h=self.handoff(art,{},status="NEEDS_CLARIFICATION");h["reason"]="unknown style"
        self.assertEqual(en.accept(r,d,h)["status"],"BLOCKED")
        with self.assertRaises(HarnessError):en.check_run(r,load(r,d),code["run_id"])
    def test_scope_escape_rejected(self):
        r,d=self.ready();run=en.dispatch(r,d,"code")
        with self.assertRaises(HarnessError):en.accept(r,d,self.handoff(run,{"implementation_facts":[]},[{"path":"AGENTS.md"}]))
    def test_overlapping_owners_rejected(self):
        r,d,_=self.create();c=en.starter_contract();c["owners"]={"code_builder":["assets"],"design_art_agent":["assets/icons"]}
        with self.assertRaises(HarnessError):en.update_contract(r,d,c,"synthetic")
    def test_task_cannot_claim_qa_pass(self):
        r,d=self.ready();run=en.dispatch(r,d,"code");h=self.handoff(run,{"implementation_facts":[]});h["game_qa"]="PASS"
        with self.assertRaises(HarnessError):en.accept(r,d,h)
    def test_notes_do_not_invalidate_dispatch(self):
        r,d=self.ready();run=en.dispatch(r,d,"code")
        p=cli(r,"task","note","--doc",d,"--text","noncontract note")
        self.assertEqual(p.returncode,0,p.stderr);en.check_run(r,load(r,d),run["run_id"])
    def test_semantic_update_resets_gates(self):
        r,d=self.ready();c=load(r,d)["contract"];rev=load(r,d)["task_revision"];c["goal"]="new goal"
        en.update_contract(r,d,c,"synthetic changed requirement")
        s=load(r,d);self.assertEqual(s["task_revision"],rev+1);self.assertEqual(s["approvals"],{})
    def test_art_manifest_auto_generated(self):
        r,d=self.ready(art=True);run=en.dispatch(r,d,"art")
        atomic_text(r/"assets/icon.bin","synthetic")
        out=en.accept(r,d,self.handoff(run,{"assets":[{"asset_id":"icon","path":"assets/icon.bin"}]}))
        record=out["artifacts"][0];m=json_read(r/record["path"])
        self.assertEqual(m["assets"][0]["sha256"],sha(r/"assets/icon.bin"));self.assertEqual(m["game_qa"],"NOT_RUN")
    def test_symlink_escape(self):
        r,d=self.ready();external=self.parent/"external";external.mkdir();(r/"game").symlink_to(external,target_is_directory=True)
        with self.assertRaises(HarnessError):safe_path(r,"game/data.txt")

    def test_screen_preview_requires_composition_source(self):
        r,d,_=self.create();c=self.contract(r,d)
        ref=".harness/references/TASK-001/base.txt";atomic_text(r/ref,"synthetic reference, not game image")
        c["visual_impact"]="screen";c["references"]=[record_file(r,ref)]
        en.update_contract(r,d,c,"synthetic visual change")
        run=en.dispatch(r,d,"design");out=".harness/previews/TASK-001/preview.txt";atomic_text(r/out,"synthetic preview")
        with self.assertRaises(HarnessError):en.accept(r,d,self.handoff(run,{"visual_mapping":{}},[{"path":out,"kind":"preview"}]))
    def test_art_concepts_must_cover_every_asset(self):
        r,d,_=self.create();self.contract(r,d,art=True);run=en.dispatch(r,d,"design")
        f=".harness/concepts/TASK-001/one.txt";atomic_text(r/f,"concept")
        with self.assertRaises(HarnessError):en.accept(r,d,self.handoff(run,{"visual_mapping":{"asset_concepts":[]}},[{"path":f,"kind":"concept"}]))
    def test_binding_checks_production_asset_not_only_manifest(self):
        r,d=self.ready(art=True);code=en.dispatch(r,d,"code");art=en.dispatch(r,d,"art")
        en.accept(r,d,self.handoff(code,{"implementation_facts":[]}));atomic_text(r/"assets/icon.bin","v1")
        en.accept(r,d,self.handoff(art,{"assets":[{"asset_id":"icon","path":"assets/icon.bin"}]}))
        atomic_text(r/"assets/icon.bin","unaccepted edit")
        with self.assertRaises(HarnessError):en.dispatch(r,d,"integration")

class ModelTests(RepoTest):
    def change(self):
        c=mo.status(self.repo)["config"];c["agents"]["code_builder"]["model"]="synthetic-model-not-a-real-provider-id";return mo.serialize(c)
    def test_preview_is_read_only(self):
        before=mo.snapshot(self.repo);p=mo.preview(self.repo,self.change());self.assertTrue(p["diff"]);self.assertEqual(before,mo.snapshot(self.repo))
    def test_apply_and_rollback(self):
        before=mo.snapshot(self.repo);text=self.change();p=mo.preview(self.repo,text);tx=mo.apply(self.repo,text,p["etag"])
        self.assertTrue(all(x["synced"] for x in mo.status(self.repo)["rows"]))
        mo.rollback(self.repo,tx["transaction"]);self.assertEqual(mo.snapshot(self.repo),before)
    def test_native_dashboard_agent_update(self):
        status=mo.status(self.repo)
        updates={row["role"]:{"model":row["target"]["model"] or "","effort":row["target"]["effort"] or ""}
                 for row in status["rows"]}
        updates["code_builder"]={"model":"synthetic-native-model","effort":"high"}
        result=mo.update_agents(self.repo,updates,status["etag"])
        row=next(x for x in mo.status(self.repo)["rows"] if x["role"]=="code_builder")
        self.assertEqual(row["target"],{"model":"synthetic-native-model","effort":"high","enabled":True})
        self.assertEqual(result["status"],"applied")
    def test_stale_preview(self):
        text=self.change();p=mo.preview(self.repo,text);f=self.repo/".codex/agents/code-builder.toml";f.write_text(f.read_text(encoding="utf-8")+"\n# external edit\n",encoding="utf-8")
        with self.assertRaises(HarnessError):mo.apply(self.repo,text,p["etag"])
    def test_rollback_preserves_instructions(self):
        text=self.change();p=mo.preview(self.repo,text);tx=mo.apply(self.repo,text,p["etag"])
        f=self.repo/".codex/agents/code-builder.toml";f.write_text(f.read_text(encoding="utf-8")+"\n# keep custom text\n",encoding="utf-8")
        mo.rollback(self.repo,tx["transaction"]);self.assertIn("# keep custom text",f.read_text(encoding="utf-8"))
    def test_manual_drift_needs_explicit_reconcile(self):
        text=self.change();p=mo.preview(self.repo,text);mo.apply(self.repo,text,p["etag"])
        f=self.repo/".codex/agents/code-builder.toml";f.write_text(f.read_text(encoding="utf-8").replace('model_reasoning_effort = "medium"','model_reasoning_effort = "low"'),encoding="utf-8")
        with self.assertRaises(HarnessError):mo.preview(self.repo)
        self.assertTrue(mo.preview(self.repo,reconcile=True)["diff"])
    def test_disabled_role_blocks_dispatch(self):
        r,d=self.ready();c=mo.status(r)["config"];c["agents"]["code_builder"]["enabled"]=False;text=mo.serialize(c);p=mo.preview(r,text);mo.apply(r,text,p["etag"])
        with self.assertRaises(HarnessError):en.dispatch(r,d,"code")
    def test_unknown_actual_is_not_target(self):
        r,d=self.ready();run=en.dispatch(r,d,"code");self.assertIsNone(run["model_observed"])
        en.observe(r,d,run["run_id"],"synthetic-host-model","low","synthetic host event")
        row=next(x for x in mo.status(r)["rows"] if x["role"]=="code_builder")
        self.assertEqual(row["recent_runs"][0]["observed"]["model"],"synthetic-host-model")
    def test_config_change_does_not_rewrite_active_snapshot(self):
        r,d=self.ready();run=en.dispatch(r,d,"code");c=mo.status(r)["config"];c["active_profile"]="quality";text=mo.serialize(c);p=mo.preview(r,text);mo.apply(r,text,p["etag"])
        current=en.check_run(r,load(r,d),run["run_id"])
        self.assertEqual(current["model_target"]["effort"],"medium")
    def test_interrupted_transaction_recovery(self):
        text=self.change();p=mo.preview(self.repo,text);txid=uid("models");path=self.repo/f".harness/model-transactions/{txid}/journal.json"
        j={"id":txid,"status":"prepared","before":p["before"],"after":p["after"],"at":now()};atomic_text(path,dump(j))
        atomic_text(self.repo/"models.toml",p["after"]["models.toml"])
        with self.assertRaises(HarnessError):mo.preview(self.repo,text)
        self.assertEqual(mo.recover(self.repo)["recovered"],[txid]);self.assertEqual(mo.snapshot(self.repo),p["before"])


class QATests(RepoTest):
    def test_success_runner_and_completion(self):
        r,d,p=self.release_world();out=rel.run(r,d,apply=True);self.assertEqual(out["status"],"PASS")
        self.assertEqual(rel.report(r,d)["status"],"PASS");self.assertFalse(rel.complete(r,d)["published"])
    def test_dry_run_does_not_execute(self):
        r,d,p=self.release_world();before=load(r,d);out=rel.run(r,d)
        self.assertEqual(out["action"],"dry-run");self.assertEqual(before,load(r,d))
    def test_zero_assertions_not_pass(self):
        script='import os,json\nfrom pathlib import Path\nPath(os.environ["HARNESS_RESULT_PATH"]).write_text(json.dumps({"schema_version":1,"run_id":os.environ["HARNESS_RUN_ID"],"check_id":os.environ["HARNESS_CHECK_ID"],"assertions":[]}))\n'
        r,d,p=self.release_world(script);self.assertEqual(rel.run(r,d,apply=True)["status"],"FAIL");self.assertEqual(rel.report(r,d)["status"],"FAIL")
    def test_exit_zero_without_report_not_pass(self):
        r,d,p=self.release_world('print("done")\n');self.assertEqual(rel.run(r,d,apply=True)["status"],"FAIL")
    def test_wrong_result_identity(self):
        script='import os,json\nfrom pathlib import Path\nPath(os.environ["HARNESS_RESULT_PATH"]).write_text(json.dumps({"schema_version":1,"run_id":"old","check_id":"C1","assertions":[{"id":"value","status":"PASS","actual":42}]}))\n'
        r,d,p=self.release_world(script);self.assertEqual(rel.run(r,d,apply=True)["status"],"FAIL")
    def test_skip_is_not_pass(self):
        script='import os,json\nfrom pathlib import Path\nPath(os.environ["HARNESS_RESULT_PATH"]).write_text(json.dumps({"schema_version":1,"run_id":os.environ["HARNESS_RUN_ID"],"check_id":os.environ["HARNESS_CHECK_ID"],"assertions":[{"id":"value","status":"SKIP","actual":None}]}))\n'
        r,d,p=self.release_world(script);self.assertEqual(rel.run(r,d,apply=True)["status"],"FAIL")
    def test_nonzero_cannot_be_overridden_by_pass_report(self):
        script='import os,json,sys\nfrom pathlib import Path\nPath(os.environ["HARNESS_RESULT_PATH"]).write_text(json.dumps({"schema_version":1,"run_id":os.environ["HARNESS_RUN_ID"],"check_id":os.environ["HARNESS_CHECK_ID"],"assertions":[{"id":"value","status":"PASS","actual":42}]}))\nsys.exit(4)\n'
        r,d,p=self.release_world(script);self.assertEqual(rel.run(r,d,apply=True)["status"],"FAIL")
    def test_missing_coverage_rejected(self):
        r,d,p=self.release_world();p["plan"]["checks"][0]["assertions"][0]["intent_ids"]=["missing:AC-2"]
        with self.assertRaises(HarnessError):rel.validate_plan(r,load(r,d),p["plan"])
    def test_script_modified_invalidates_authorization(self):
        r,d,p=self.release_world();f=r/"tests/release/check.py";f.write_text(f.read_text()+"\n# changed\n")
        with self.assertRaises(HarnessError):rel.run(r,d,apply=True)
    def test_partial_runs_require_full_coverage(self):
        r,d,p=self.release_world(two=True);rel.run(r,d,["C1"],apply=True);self.assertEqual(rel.report(r,d)["status"],"INCOMPLETE")
        rel.run(r,d,["C2"],apply=True);self.assertEqual(rel.report(r,d)["status"],"PASS")
    def test_manual_evidence_needed(self):
        r,d,p=self.release_world(manual=True);rel.run(r,d,apply=True);self.assertEqual(rel.report(r,d)["status"],"INCOMPLETE")
        f=".harness/qa/v-test/human/evidence.txt";atomic_text(r/f,"synthetic human inspection")
        rel.manual(r,d,"M1","PASS",[f],"synthetic manual decision");self.assertEqual(rel.report(r,d)["status"],"PASS")
    def test_tampered_results_rejected(self):
        r,d,p=self.release_world();out=rel.run(r,d,apply=True);f=r/out["result"];f.write_text(f.read_text()+" ")
        with self.assertRaises(HarnessError):rel.report(r,d)
    def test_test_cannot_modify_candidate_silently(self):
        script='from pathlib import Path\nPath("game/value.txt").write_text("bad")\n'
        r,d,p=self.release_world(script);out=rel.run(r,d,apply=True);self.assertEqual(out["status"],"ERROR")
        with self.assertRaises(HarnessError):rel.report(r,d)
    def test_timeout_is_error(self):
        r,d,p=self.release_world('import time\ntime.sleep(2)\n')
        # Reopen, reaccept changed plan; do not edit hashes or authorization in-place.
        rel.reopen_scripts(r,d,"synthetic timeout setting");run=en.dispatch(r,d,"qa_scripts")
        p["execution"]["checks"][0]["timeout_seconds"]=0.1;atomic_text(r/"tests/release/execution.json",dump(p["execution"]))
        en.accept(r,d,self.handoff(run,{"qa_execution":"tests/release/execution.json"},[{"path":"tests/release/execution.json"},{"path":"tests/release/check.py"}]))
        ws.git(r,"add",".");ws.git(r,"commit","-m","timeout plan");rel.authorize(r,d,"synthetic authorization")
        self.assertEqual(rel.run(r,d,apply=True)["status"],"FAIL")
    def test_product_fix_scope_and_no_qa_rerun_by_builder(self):
        r,d,p=self.release_world();rel.run(r,d,apply=True)
        rel.request_fix(r,d,["game"],"synthetic requested repair");run=en.dispatch(r,d,"release_fix")
        self.assertEqual(run["role"],"code_builder");self.assertEqual(run["allowed_paths"],["game"])
        en.accept(r,d,self.handoff(run,{"implementation_facts":["synthetic fix description"]}))
        self.assertIsNone(load(r,d)["authorization"])
    def test_candidate_change_invalidates_old_evidence(self):
        r,d,p=self.release_world();rel.run(r,d,apply=True)
        atomic_text(r/"game/extra.txt","candidate changed");ws.git(r,"add",".");ws.git(r,"commit","-m","new candidate")
        with self.assertRaises(HarnessError):rel.report(r,d)
        rel.authorize(r,d,"new synthetic candidate");self.assertEqual(rel.report(r,d)["status"],"INCOMPLETE")

    def test_pause_during_runner_quarantines_late_result(self):
        import time
        script='import time,os,json\nfrom pathlib import Path\ntime.sleep(0.4)\nPath(os.environ["HARNESS_RESULT_PATH"]).write_text(json.dumps({"schema_version":1,"run_id":os.environ["HARNESS_RUN_ID"],"check_id":os.environ["HARNESS_CHECK_ID"],"assertions":[{"id":"value","status":"PASS","actual":42}]}))\n'
        r,d,p=self.release_world(script)
        output=[];errors=[]
        def worker():
            try:output.append(rel.run(r,d,apply=True))
            except Exception as e:errors.append(e)
        t=threading.Thread(target=worker);t.start()
        for _ in range(200):
            if any(x["status"]=="ACTIVE" and x["step"]=="qa_run" for x in load(r,d)["runs"].values()):break
            time.sleep(.005)
        # Runner holds no long-lived state lock while its subprocess executes.
        for _ in range(20):
            try:en.change_control(r,d,"block",reason="synthetic user pause");break
            except HarnessError as e:
                if "Locked:" not in str(e):raise
                time.sleep(.01)
        t.join(5);self.assertFalse(errors);self.assertFalse(t.is_alive());self.assertTrue(output[0]["quarantined"])
        self.assertFalse(load(r,d)["results"])
    def test_qa_scope_can_match_existing_project_tests(self):
        tr,td,commit=self.complete_task();r,d,_=self.create("release","v-existing",commit)
        rel.prepare(r,d,[{"id":"TASK-001","commit":commit,"document":td}])
        rel.set_qa_scope(r,d,["ExistingTests"],"synthetic project convention")
        designer=en.dispatch(r,d,"interfaces");s=load(r,d)
        plan={"schema_version":2,"release_id":s["id"],"input_digest":s["input_digest"],"interface_requirements":[],
              "checks":[{"id":"C1","case":"synthetic case","fixture_reset":"isolated","interface_requirement_ids":[],
                         "assertions":[{"id":"value","expectation":"value equals 42","intent_ids":["TASK-001:AC-1"]}]}],"manual_checks":[]}
        en.accept(r,d,self.handoff(designer,{"qa_plan":plan}))
        run=en.dispatch(r,d,"qa_scripts");self.assertEqual(run["allowed_paths"],["ExistingTests"])

    def test_designer_plan_is_required_before_builder_scripts(self):
        tr,td,commit=self.complete_task();r,d,_=self.create("release","v-order",commit)
        rel.prepare(r,d,[{"id":"TASK-001","commit":commit,"document":td}])
        self.assertEqual(rel.plan(r,load(r,d))["next"],["interfaces"])
        with self.assertRaises(HarnessError):en.dispatch(r,d,"qa_scripts")

    def test_builder_cannot_change_designer_assertions(self):
        r,d,p=self.release_world();rel.reopen_scripts(r,d,"synthetic script repair")
        run=en.dispatch(r,d,"qa_scripts");p["execution"]["checks"][0]["assertion_ids"]=["different"]
        atomic_text(r/"tests/release/execution.json",dump(p["execution"]))
        with self.assertRaises(HarnessError):
            en.accept(r,d,self.handoff(run,{"qa_execution":"tests/release/execution.json"},[{"path":"tests/release/execution.json"},{"path":"tests/release/check.py"}]))

    def test_script_reopen_preserves_designer_plan(self):
        r,d,p=self.release_world();before=load(r,d)["qa_plan_handoff"]
        out=rel.reopen_scripts(r,d,"synthetic adapter bug")
        self.assertTrue(out["qa_plan_preserved"]);self.assertEqual(load(r,d)["qa_plan_handoff"],before)

class DashboardTests(RepoTest):
    def setUp(self):
        super().setUp();self.httpd,self.token=db.server(self.repo,0);self.base=f"http://127.0.0.1:{self.httpd.server_port}"
        self.thread=threading.Thread(target=self.httpd.serve_forever,daemon=True);self.thread.start()
        self.addCleanup(self.httpd.server_close);self.addCleanup(self.httpd.shutdown)
    def request(self,path,data=None,token=True,origin=None):
        headers={"X-Harness-Token":self.token} if token else {}
        if data is not None:headers.update({"Content-Type":"application/json","Origin":origin or self.base})
        req=urllib.request.Request(self.base+path,data=None if data is None else json.dumps(data).encode(),headers=headers)
        try:
            with urllib.request.urlopen(req,timeout=5) as r:return r.status,r.read()
        except urllib.error.HTTPError as e:return e.code,e.read()
    def test_status_matrix(self):
        code,b=self.request('/api/status');self.assertEqual(code,200);rows=json.loads(b)["rows"];self.assertEqual(len(rows),4)
        designer=next(x for x in rows if x["role"]=="feature_designer")
        self.assertEqual(designer["display_name"],"Feature Designer")
        self.assertEqual(designer["backend_binding"]["config_key"],"agents.feature_designer")
        self.assertEqual(designer["backend_binding"]["agent_file"],".codex/agents/feature-designer.toml")
        self.assertEqual(designer["backend_binding"]["dispatch_steps"],["designer","interfaces"])
    def test_token_required(self):self.assertEqual(self.request('/api/status',token=False)[0],400)
    def test_cross_origin_post_rejected(self):
        c=mo.status(self.repo)["config"];self.assertEqual(self.request('/api/preview',{"config":c},origin='https://example.invalid')[0],400)
    def test_no_file_or_command_endpoint(self):
        self.assertEqual(self.request('/../../models.toml')[0],404)
        self.assertEqual(self.request('/api/exec',{"command":"echo forbidden"})[0],404)
    def test_preview_apply_requires_confirmation(self):
        c=mo.status(self.repo)["config"];c["active_profile"]="quality"
        code,b=self.request('/api/preview',{"config":c});self.assertEqual(code,200)
        payload={"config":c,"etag":json.loads(b)["etag"]}
        self.assertEqual(self.request('/api/apply',payload)[0],400)
        payload["confirmed"]=True;self.assertEqual(self.request('/api/apply',payload)[0],200)
    def test_static_ui_headers_and_syntax(self):
        with urllib.request.urlopen(self.base,timeout=5) as r:
            self.assertIn("frame-ancestors 'none'",r.headers['Content-Security-Policy']);self.assertIn(b"Subagent",r.read())

if __name__ == '__main__':unittest.main()
