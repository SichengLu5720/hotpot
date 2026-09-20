"""Builder adapter skeleton. NOT a real game test; unadapted execution reports ERROR.

Copy/adapt in the real project's tests/release/, pin it in the accepted QA plan,
and call through the deterministic runner only after release authorization.
"""
import json
import os
from pathlib import Path


def observe_game():
    """Builder implements this using the project's actual, isolated API/fixture."""
    raise NotImplementedError("No game adapter supplied. Do not replace this with a hard-coded PASS.")


def main():
    required = ("HARNESS_RUN_ID", "HARNESS_CHECK_ID", "HARNESS_RESULT_PATH")
    if not all(os.environ.get(k) for k in required):
        raise SystemExit("Run through harness qa run with an accepted plan; no game was tested.")
    assertion = {"id": "volume-restored", "status": "ERROR", "actual": None}
    try:
        observation = observe_game()
        # The real adapter should supply source-derived expected and observed values.
        expected = observation["saved_value"]
        actual = observation["value_after_reload"]
        assertion.update(actual=actual, status="PASS" if actual == expected else "FAIL")
    except Exception as exc:
        assertion["actual"] = {"error": str(exc)}
    payload = {"schema_version": 1, "run_id": os.environ["HARNESS_RUN_ID"],
               "check_id": os.environ["HARNESS_CHECK_ID"], "assertions": [assertion], "evidence": []}
    Path(os.environ["HARNESS_RESULT_PATH"]).write_text(json.dumps(payload, ensure_ascii=False), encoding="utf-8")
    return 0 if assertion["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
