# Actual queued touchscreen UI journeys
Editor synthetic input only; no physical Android/VR claim. NOT RUN fields are not passes.
Selection: P01,P02,P03,P04,P05,P06,P07,P08,C01,C02,C03,C04,C05,C06,C07,C08,B01,B02,B03,B04,B05,B06,B07,B08,M01,M02,M03,M04,M05,M06,M07,M08. Per-lesson limit: 180 seconds. Scoped world regressions call fixture/math helpers separately; they are not UI journey actions.
Observed resolution: 1600 × 900; safe area: (x:0.00, y:0.00, width:1600.00, height:900.00). Requested 1600×900; Editor Game View may override the request.

| ID | State | Entry | Assembly | Run | Saved/reopen | Blocker |
|---|---|---|---|---|---|---|
| P01 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P02 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P03 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P04 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P05 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P06 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P07 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| P08 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C01 | BLOCKED | True | True | True | False/False | Visible enabled button missing: Equipment item burette |
| C02 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C03 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C04 | BLOCKED | True | True | False | False/False | Same guide task exceeded 100 attempts; inspect visible readings before judging science progress: prepare:reagent: / Dán nhãn NaCl, NaNO3 và nước. Nhấn Thực hiện bước này. |
| C05 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C06 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| C07 | BLOCKED | True | True | True | False/False | Primary task control disabled: record:: / Nhấn Ghi số đo để lưu lượt thực hành. |
| C08 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B01 | BLOCKED | True | True | True | False/False | VISUAL REVIEW TIMEOUT: no matching manual command within 120 seconds; request=015c776bd615456988037b44bc59ec85 |
| B02 | BLOCKED | True | True | True | False/False | Bounded UI journey timed out before saved result; instrument:microscope:cell / Chọn Chưa co hoặc Đã co cho mã ô đang được đánh dấu. |
| B03 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B04 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B05 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B06 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B07 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B08 | BLOCKED | True | True | True | False/False | Bounded UI journey timed out before saved result; instrument:microscope:stage: / Chọn nút tên kỳ vừa quan sát; chưa chắc thì chọn Chưa rõ. |
| M01 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M02 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M03 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M04 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M05 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M06 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M07 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M08 | UI JOURNEY RECORDED | True | True | True | True/True |  |

## Scoped world regressions (separate from UI journey)
PASS Bench displays and line hints: Only two named active-bench display props are hidden, including their colliders
PASS Bench displays and line hints: Worktop, sink, active lesson equipment, other benches and prior inactive states survive
PASS Bench displays and line hints: Leaving restores original active states
PASS Bench displays and line hints: Disabling scope restores props
FAIL Bench displays and line hints: Line-only cord receives a real model outline without changing source
PASS Isolated storage retrieval: Stored ruler can be taken into the hand
PASS Isolated storage retrieval: Taking again does not lose storage return state
PASS Isolated storage retrieval: Cancel restores stored/inactive ruler and original tray pose
PASS Isolated storage retrieval: Storage retrieval never advances preparation
PASS Isolated storage retrieval: Changing selection returns previously stored ruler to storage
PASS Isolated storage retrieval: Stored ruler still uses validated explicit installation
PASS Isolated storage retrieval: Cancel after installation cannot re-stow fitted ruler
PASS Isolated friction recovery: P03 retry rejects reset before tile end
PASS Isolated friction recovery: P03 missed steady-pull window stays unrecordable and offers retry
PASS Isolated friction recovery: P03 explicit retry succeeds
PASS Isolated friction recovery: P03 retry clears old force, peak and steady measurements
PASS Isolated friction recovery: P03 waiting after reset cannot fabricate a new measurement
PASS Isolated friction recovery: P03 repeat must genuinely break away before steady control
PASS Isolated friction recovery: P03 new steady action alone is insufficient
PASS Isolated friction recovery: P03 slow-steady hand control preserves visible motion at at most 0.05 m/s
PASS Isolated friction recovery: P03 repeat records only after new peak and measured steady interval
P01: entry checks=22, assembled checks=22, failures=0
P02: entry checks=22, assembled checks=22, failures=0
P03: entry checks=22, assembled checks=31, failures=0
P04: entry checks=22, assembled checks=22, failures=0
P05: entry checks=22, assembled checks=22, failures=0
P06: entry checks=22, assembled checks=22, failures=0
P07: entry checks=22, assembled checks=22, failures=0
P08: entry checks=22, assembled checks=22, failures=0
C01: entry checks=21, assembled checks=21, failures=0
C02: entry checks=21, assembled checks=21, failures=0
C03: entry checks=25, assembled checks=25, failures=0
C04: entry checks=21, assembled checks=21, failures=0
C05: entry checks=21, assembled checks=21, failures=0
C06: entry checks=21, assembled checks=21, failures=0
C07: entry checks=25, assembled checks=25, failures=0
C08: entry checks=21, assembled checks=21, failures=0
B01: entry checks=22, assembled checks=22, failures=0
B02: entry checks=22, assembled checks=22, failures=0
B03: entry checks=26, assembled checks=26, failures=0
B04: entry checks=22, assembled checks=22, failures=0
B05: entry checks=22, assembled checks=22, failures=0
B06: entry checks=22, assembled checks=22, failures=0
B07: entry checks=22, assembled checks=22, failures=0
B08: entry checks=22, assembled checks=22, failures=0
M01: entry checks=22, assembled checks=22, failures=0
M02: entry checks=22, assembled checks=22, failures=0
M03: entry checks=22, assembled checks=22, failures=0
M04: entry checks=22, assembled checks=22, failures=0
M05: entry checks=22, assembled checks=22, failures=0
M06: entry checks=22, assembled checks=22, failures=0
M07: entry checks=22, assembled checks=22, failures=0
M08: entry checks=22, assembled checks=22, failures=0
