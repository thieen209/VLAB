"""Bounded C01 UI operator for the already observed default-recipe QA fixture.

Uses only the existing touch-command protocol and observed UI action receipts.
Never reads the chemistry state or sets a titre/completion value.
"""
import json
import sys
import time
from pathlib import Path


def pulse(request_id):
    button = lambda name: {"kind": "button", "button": name}
    actions = [button("+") for _ in range(8)]
    actions += [{"kind": "wait", "seconds": 30}, {"kind": "wait", "seconds": 15.9}]
    actions += [button("−") for _ in range(8)]
    actions += [button("Quay lại")]
    return {"requestId": request_id, "reviewBasis": "Valve pulse calibrated from prior rendered UI volume observations for this default 0.1M/10mL fixture; close and return to the normal guide for the actual final reading. Mixing, 30-second persistence, final meniscus and titre retention remain the app's actual UI steps.", "actions": actions}


sample = pulse("self-check")
assert len(sample["actions"]) == 19
assert sum(a.get("seconds", 0) for a in sample["actions"]) == 45.9
assert sum(a.get("button") == "+" for a in sample["actions"]) == 8
assert sum(a.get("button") == "−" for a in sample["actions"]) == 8
if sys.argv[1:] == ["--check"]:
    print("PASS bounded pulse plan; eight opens/eight closes, 45.9 real seconds, return to the actual guide and its meniscus/indicator controls.")
    raise SystemExit(0)

folder = Path(sys.argv[1]).resolve()
assert folder.parent.name == "SecondTestUI" and folder.is_dir()
seen = set()
issued = 0
deadline = time.monotonic() + 1200
while time.monotonic() < deadline:
    try:
        report = json.loads((folder / "matrix.json").read_text(encoding="utf-8-sig"))
        assert report["selection"] == "C01" and report["lessonTimeout"] == 1200
        lesson = report["lessons"][0]
        if lesson["status"] in ["UI JOURNEY RECORDED", "BLOCKED"]:
            print("TERMINAL", lesson["status"], lesson["blocker"], flush=True)
            break
        request = json.loads((folder / "visual-request.json").read_text(encoding="utf-8-sig"))
        rid = request["requestId"]
        if rid not in seen:
            assert request["experimentId"] == "C01" and "Độ mở khóa" in request["task"]
            assert sum(b["name"] == "+" for b in request["buttons"]) == 1
            trace = (folder / "touch-trace.txt").read_text(encoding="utf-8-sig")
            retained = trace.count("TAP Giữ lượt chuẩn độ |")
            fresh = trace.count("TAP Mẫu mới / lượt kế |")
            if issued and (retained < issued or fresh < issued):
                print("MANUAL REVIEW REQUIRED: previous pulse did not lead to retained titre/fresh aliquot", request["screenshot"], flush=True)
                break
            if issued >= 3:
                print("MANUAL REVIEW REQUIRED: three-pulse cap reached", request["screenshot"], flush=True)
                break
            command = pulse(rid)
            temporary = folder / "operator-command.tmp"
            temporary.write_text(json.dumps(command, ensure_ascii=False), encoding="utf-8")
            temporary.replace(folder / "visual-command.json")
            issued += 1
            seen.add(rid)
            print("SUBMITTED", issued, rid, "retained UI actions", retained, "fresh UI actions", fresh, flush=True)
    except (FileNotFoundError, json.JSONDecodeError):
        pass
    time.sleep(1)
else:
    raise SystemExit("Operator deadline reached; no success inferred.")
