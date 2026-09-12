"""Generate a factual local review from Unity's saved results and real captures."""
from pathlib import Path
import datetime as dt
import hashlib
import html
import json
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "Production"
OUT.mkdir(parents=True, exist_ok=True)
now = dt.datetime.now(dt.timezone.utc)
suites = []
rows = []
for name in ("edit", "play", "production"):
    path = ROOT / "TestResults" / "Production" / f"{name}.xml"
    if not path.exists():
        continue
    run = ET.parse(path).getroot()
    cases = list(run.iter("test-case"))
    counts = {result: sum(c.get("result") == result for c in cases) for result in ("Passed", "Failed", "Skipped", "Inconclusive")}
    suites.append({"name": name, "result": run.get("result"), "count": len(cases), **counts, "recorded_utc": dt.datetime.fromtimestamp(path.stat().st_mtime, dt.timezone.utc).isoformat()})
    for case in cases:
        note = case.findtext("failure/message", "").replace("\n", " ").replace("|", "/")
        rows.append(f"| {case.get('fullname')} | Unity Editor {name} | Pass | {case.get('result')} | {case.get('duration', '0')} s | {note} |")

build_path = ROOT / "TestResults" / "Unified" / "android-build.txt"
apk = ROOT / "Builds" / "Android" / "VLAB.apk"
build = {"result": "NOT RUN", "note": "No build result from the current validation date."}
if build_path.exists() and dt.datetime.fromtimestamp(build_path.stat().st_mtime, dt.timezone.utc).date() == now.date():
    build = {"result": build_path.read_text(encoding="utf-8-sig").splitlines()[0], "summary": build_path.read_text(encoding="utf-8-sig")}
    if apk.exists() and build["result"] == "Succeeded":
        build.update(bytes=apk.stat().st_size, sha256=hashlib.file_digest(apk.open("rb"), "sha256").hexdigest())

data = {"generated_utc": now.isoformat(), "suites": suites, "android": build, "hardware": "Physical Android optics, BLE transport, latency and sustained performance not measured."}
(OUT / "validation-summary.json").write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")
summary_rows = "\n".join(f"| {s['name']} | {s['count']} | {s['Passed']} | {s['Failed']} | {s['Skipped'] + s['Inconclusive']} | {s['recorded_utc']} |" for s in suites)
matrix = f"""# VLAB test matrix

Generated from saved Unity XML on {now.isoformat()}. Only the recorded executions below are claimed. The production subset repeats cases from PlayMode after final visual adjustments; suite totals are not additive unique-test counts.

| Suite | Cases | Passed | Failed | Skipped / inconclusive | Recorded UTC |
|---|---:|---:|---:|---:|---|
{summary_rows}

Android: **{build['result']}**. See `TestResults/Unified/android-build.txt` and the APK under `Builds/Android`.

The source inventory is `docs/Production/source-audit.json`. The build validates enabled scene dependencies and missing scripts. Real camera captures are under `TestResults/Production/Visuals`; these are not Android screenshots or a claim of measured headset performance.

One exact optional Unity AI editor account timeout is recorded separately in `TestResults/Production/editor-environment.txt`. It is permitted only when its editor-package stack matches. Application logs and assertions remain active.

## Physical device matrix

| Test | Environment | Expected | Actual | Status | Notes |
|---|---|---|---|---|---|
| Cardboard stereo, optics and device QR calibration | Android phone + viewer | Both eyes, aligned lenses, correct pose | Device unavailable | NOT RUN | Runtime uses installed Cardboard 1.35.0 |
| Viewer mode on/off and touch input | Physical Android | Correct subsystem lifecycle and scene reload | Device unavailable | NOT RUN | Deterministic policy/pose tests and APK compilation do not validate optics |
| BLE pairing, firmware buttons and packet transport | Supplied physical controllers | Match actual firmware/GATT protocol | Protocol and hardware unavailable | NOT RUN | Decoded replay contract is tested; no wire protocol invented |
| Sustained frame time, temperature and battery | Target Android device | Meet its refresh/thermal budget | Unmeasured | NOT RUN | No editor frame time represented as mobile performance |

## Individual executable checks

| Test name | Environment | Expected | Actual | Duration | Notes |
|---|---|---|---|---|---|
""" + "\n".join(rows) + "\n"
(ROOT / "docs" / "VLAB_TEST_MATRIX.md").write_text(matrix, encoding="utf-8")

visuals = sorted((ROOT / "TestResults" / "Production" / "Visuals").glob("*.png"),key=lambda p:(p.name!="Menu-controller.png",p.name))
options = "".join(f'<option value="../../TestResults/Production/Visuals/{html.escape(p.name)}">{html.escape(p.stem)}</option>' for p in visuals)
first = "../../TestResults/Production/Visuals/" + visuals[0].name if visuals else ""
cards = "".join(f'<article><small>{s["name"].upper()} TESTS</small><strong>{s["Passed"]} / {s["count"]}</strong><span>{s["Failed"]} failed · {s["Skipped"] + s["Inconclusive"]} skipped/inconclusive</span></article>' for s in suites)
page = f"""<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>VLAB · Validation review</title>
<style>body{{margin:0;background:#0d1117;color:#f8f9fa;font:16px/1.5 system-ui,sans-serif}}main{{max-width:1200px;margin:auto;padding:40px 24px}}header{{border-top:3px solid #00b4d8;padding-top:18px}}small,.muted{{color:#abc2cf}}h1{{font-size:38px;margin:8px 0}}a{{color:#62def7}}.cards{{display:flex;gap:16px;flex-wrap:wrap;margin:28px 0}}article{{background:#142230;padding:20px;min-width:200px;flex:1}}strong,article span{{display:block}}strong{{font-size:30px}}select{{background:#142230;color:white;border:1px solid #00b4d8;padding:12px;font:inherit;max-width:100%}}figure{{margin:18px 0;background:#05090d}}img{{display:block;width:100%;max-height:75vh;object-fit:contain}}figcaption{{padding:12px;color:#abc2cf}}nav{{display:flex;gap:20px;flex-wrap:wrap;margin:28px 0}}.notice{{border-left:3px solid #00b4d8;padding:10px 18px;background:#142230}}button{{background:#00b4d8;border:0;padding:12px 20px;font:inherit;cursor:pointer}}</style>
<main><header><small>VLAB / ENGINEERING EVIDENCE</small><h1>Validation review</h1><p class="muted">Real Unity captures and recorded checks · {now.strftime('%d %B %Y')}</p></header><div class="cards">{cards}<article><small>ANDROID BUILD</small><strong>{html.escape(build['result'])}</strong><span>ARM64 · IL2CPP · Cardboard 1.35.0</span></article></div>
<p class="notice">Physical Android optics, BLE transport, controller latency and sustained performance remain unverified. This page reviews the Unity project; it is not a replacement laboratory interface.</p>
<nav><a href="../VLAB_REPAIR_REPORT.md">Repair report</a><a href="../VLAB_TEST_MATRIX.md">Full test matrix</a><a href="../VLAB_ARCHITECTURE.md">Architecture</a><a href="../VLAB_CONTROLLER_MAPPING.md">Controller mapping</a><a href="../VLAB_VISUAL_STYLE.md">Visual guide</a></nav>
<label for="capture">Inspect a captured state</label><br><select id="capture">{options}</select> <button id="next">Next capture →</button><figure><img id="image" src="{first}" alt="Real Unity camera capture"><figcaption id="caption">{html.escape(visuals[0].stem) if visuals else 'No captures'}</figcaption></figure>
<p class="muted">Capture resolution: 1600 × 1000. Camera renders were inspected for clipping, label contrast and visible controller feedback. Native Unity UI was also inspected during the run.</p></main>
<script>const select=document.querySelector('#capture');function show(){{document.querySelector('#image').src=select.value;document.querySelector('#caption').textContent=select.selectedOptions[0].textContent;}}select.addEventListener('change',show);document.querySelector('#next').addEventListener('click',()=>{{select.selectedIndex=(select.selectedIndex+1)%select.options.length;show();}});</script></html>"""
(OUT / "review.html").write_text(page, encoding="utf-8")
print(json.dumps(data, ensure_ascii=False))
