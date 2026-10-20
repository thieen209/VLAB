# Actual queued touchscreen UI journeys
Editor synthetic input only; no physical Android/VR claim. NOT RUN fields are not passes.
Selection: P01,C03,M01,B01. Per-lesson limit: 180 seconds. Scoped world regressions call fixture/math helpers separately; they are not UI journey actions.
Observed resolution: 640 × 480; safe area: (x:0.00, y:0.00, width:640.00, height:480.00). Requested 1600×900; Editor Game View may override the request.

| ID | State | Entry | Assembly | Run | Saved/reopen | Blocker |
|---|---|---|---|---|---|---|
| P01 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C03 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M01 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B01 | BLOCKED | True | True | True | False/False | VISUAL CLASSIFICATION REQUIRED: image and labels need human visual judgment; no model-answer lookup performed |

## Scoped world regressions (separate from UI journey)
P01: entry checks=19, assembled checks=19, failures=0 
C03: entry checks=22, assembled checks=22, failures=0 
M01: entry checks=19, assembled checks=19, failures=0 
B01: entry checks=19, assembled checks=19, failures=0 
