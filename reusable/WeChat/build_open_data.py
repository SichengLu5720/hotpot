"""Generate a single WeChat open-data entry; standard Python 3.11+, no packages."""
import argparse
import json
from pathlib import Path
import re

def build(cloud_key="harness_campaign_rank_v1", protocol="harness-friends-v1", maximum_score=20):
    if not cloud_key or not protocol or not 1 <= maximum_score <= 2147483647:
        raise ValueError("Nonempty key/protocol and a positive int32 maximum required")
    root = Path(__file__).resolve().parent
    rules = (root / "Source/model.js").read_text(encoding="utf-8")
    entry = (root / "Source/index.js").read_text(encoding="utf-8")
    needle = "const model = require('./model');"
    if entry.count(needle) != 1 or re.search(r"\brequire\s*\(|\bimport\s", rules):
        raise ValueError("Unexpected module dependency; export stopped")
    rules = rules.replace("const KEY = 'islands_campaign_rank_v1';", "const KEY = " + json.dumps(cloud_key) + ";")
    rules = rules.replace("v.completedCount <= 20", "v.completedCount <= " + str(maximum_score))
    entry = entry.replace("'islands-friends-v1'", json.dumps(protocol))
    entry = entry.replace(needle, "const model = (() => { const module = {exports: {}};\n" + rules + "\nreturn module.exports; })();")
    if re.search(r"\brequire\s*\(", entry):
        raise ValueError("Unresolved require in final export")
    output = root / "open-data/index.js"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(entry, encoding="utf-8")
    return output

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--cloud-key", default="harness_campaign_rank_v1")
    parser.add_argument("--protocol", default="harness-friends-v1")
    parser.add_argument("--maximum-score", type=int, default=20)
    args = parser.parse_args()
    print(build(args.cloud_key, args.protocol, args.maximum_score))
