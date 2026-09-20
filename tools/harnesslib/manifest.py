"""Inventory only: byte facts are computed, meanings come from the asset contract."""
from __future__ import annotations
import struct
from pathlib import Path
from .common import *


def metadata(path):
    p = Path(path)
    with p.open("rb") as f:
        h = f.read(32)
    result = {"extension": p.suffix.lower(), "width": None, "height": None,
              "alpha_channel": None, "inspection": "header only; not image integrity or visual QA"}
    if len(h) >= 26 and h[:8] == b"\x89PNG\r\n\x1a\n" and h[12:16] == b"IHDR":
        result.update(format="PNG", width=struct.unpack(">I", h[16:20])[0], height=struct.unpack(">I", h[20:24])[0],
                      alpha_channel=True if h[25] in (4, 6) else None)
    elif len(h) >= 10 and h[:6] in (b"GIF87a", b"GIF89a"):
        result.update(format="GIF", width=int.from_bytes(h[6:8], "little"), height=int.from_bytes(h[8:10], "little"))
    else:
        result["format"] = "unknown; metadata adapter required for this format"
    return result


def generate(root, task, entries, run_id):
    require(isinstance(entries, list) and entries, "Art must provide explicit asset paths and semantic mapping")
    owned = task["contract"]["owners"]["design_art_agent"]
    known_specs = {x["id"]: x for x in task["contract"]["asset_contract"] if isinstance(x, dict) and "id" in x}
    paths = set(); out = []
    for e in entries:
        path = e.get("path", "")
        require(path not in paths, "Duplicate asset path")
        paths.add(path)
        require(within(path, owned) and not path.startswith(".harness/"), "Formal assets must be in art-owned production paths")
        require(e.get("asset_id") in known_specs, "Asset must reference a defined contract ID")
        p = safe_path(root, path)
        action = e.get("action", "upsert")
        require(action in ("upsert", "delete"), "Use upsert/delete; a rename is deletion plus upsert")
        row = {"asset_id": e["asset_id"], "path": path, "action": action,
               "declared": known_specs[e["asset_id"]]}
        if action == "delete":
            require(not p.exists(), "Deleted asset still exists")
            row["sha256"] = None
        else:
            row.update(record_file(root, path)); row["observed_metadata"] = metadata(p)
        out.append(row)
    ident = uid("manifest")
    value = {"schema_version": 1, "task_id": task["id"], "task_revision": task["task_revision"],
             "run_id": run_id, "approved_concepts": task["approvals"].get("concept"),
             "assets": out, "game_qa": "NOT_RUN", "at": now()}
    path = f".harness/artifacts/{task['id']}/{ident}.json"
    atomic_text(safe_path(root, path), dump(value))
    return record_file(root, path, artifact_id=ident, kind="asset_manifest", task_revision=task["task_revision"], run_id=run_id)
