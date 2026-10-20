# Actual queued touchscreen UI journeys
Editor synthetic input only; no physical Android/VR claim. NOT RUN fields are not passes.
Selection: P01,P02,P03,P04,P05,P06,P07,P08,C01,C02,C03,C04,C05,C06,C07,C08,B01,B02,B03,B04,B05,B06,B07,B08,M01,M02,M03,M04,M05,M06,M07,M08. Per-lesson limit: 180 seconds. Scoped world regressions call fixture/math helpers separately; they are not UI journey actions.
Observed resolution: 1600 × 900; safe area: (x:0.00, y:0.00, width:1600.00, height:900.00). Requested 1600×900; Editor Game View may override the request.

| ID | State | Entry | Assembly | Run | Saved/reopen | Blocker |
|---|---|---|---|---|---|---|
| P01 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P02 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P03 | BLOCKED | True | True | True | False/False | Repeated unchanged UI task: instrument:meter:pull+ / Lực kế 10 N → Tăng lực kéo 0,05 N |
| P04 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P05 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P06 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P07 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P08 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C01 | BLOCKED | True | True | True | False/False | Guide requires manual recovery/configuration: variables:burette: / Mở Thông số, tăng Độ mở khóa ít; lắc bình, đóng khóa để đọc lại. |
| C02 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C03 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C04 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C05 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C06 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C07 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C08 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B01 | BLOCKED | True | True | True | False/False | VISUAL CLASSIFICATION REQUIRED: image and labels need human visual judgment; no model-answer lookup performed |
| B02 | BLOCKED | True | True | False | False/False | VISUAL CLASSIFICATION REQUIRED: image and labels need human visual judgment; no model-answer lookup performed |
| B03 | BLOCKED | True | False | False | False/False | Take UI did not retain equipment ruler |
| B04 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B05 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B06 | BLOCKED | True | False | False | False/False | Take UI did not retain equipment ruler |
| B07 | BLOCKED | True | True | True | False/False | Guided control absent/disabled: rod/spool |
| B08 | BLOCKED | False | False | False | False/False | Subject tap did not reach browser |
| M01 | BLOCKED | False | False | False | False/False | Visible enabled button missing: Browser lesson M01 |
| M02 | BLOCKED | False | False | False | False/False | Visible enabled button missing: Browser lesson M02 |
| M03 | BLOCKED | False | False | False | False/False | Entry failed, route=readiness lesson= |
| M04 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M05 | BLOCKED | True | False | False | False/False | Take UI did not retain equipment rod100 |
| M06 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M07 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M08 | UI JOURNEY RECORDED | True | True | True | True/True |  |

## Scoped world regressions (separate from UI journey)
P01: entry checks=19, assembled checks=19, failures=0 
P02: entry checks=19, assembled checks=19, failures=0 
P03: entry checks=19, assembled checks=19, failures=0 
P04: entry checks=19, assembled checks=19, failures=0 
P05: entry checks=19, assembled checks=19, failures=0 
P06: entry checks=19, assembled checks=19, failures=0 
P07: entry checks=19, assembled checks=19, failures=0 
P08: entry checks=19, assembled checks=19, failures=0 
C01: entry checks=18, assembled checks=18, failures=0 
C02: entry checks=18, assembled checks=18, failures=0 
C03: entry checks=22, assembled checks=22, failures=0 
C04: entry checks=18, assembled checks=18, failures=0 
C05: entry checks=18, assembled checks=18, failures=0 
C06: entry checks=18, assembled checks=18, failures=0 
C07: entry checks=22, assembled checks=22, failures=0 
C08: entry checks=18, assembled checks=18, failures=0 
B01: entry checks=19, assembled checks=19, failures=0 
B02: entry checks=19, assembled checks=19, failures=0 
B03: entry checks=23, assembled checks=0, failures=0 
B04: entry checks=19, assembled checks=19, failures=0 
B05: entry checks=19, assembled checks=19, failures=0 
B06: entry checks=19, assembled checks=0, failures=0 
B07: entry checks=19, assembled checks=19, failures=0 
B08: entry checks=0, assembled checks=0, failures=0 
M01: entry checks=0, assembled checks=0, failures=0 
M02: entry checks=0, assembled checks=0, failures=0 
M03: entry checks=0, assembled checks=0, failures=0 
M04: entry checks=19, assembled checks=19, failures=0 
M05: entry checks=19, assembled checks=0, failures=0 
M06: entry checks=19, assembled checks=19, failures=0 
M07: entry checks=19, assembled checks=19, failures=0 
M08: entry checks=19, assembled checks=19, failures=0 
