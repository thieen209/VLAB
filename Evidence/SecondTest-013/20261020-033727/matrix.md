# Actual queued touchscreen UI journeys
Editor synthetic input only; no physical Android/VR claim. NOT RUN fields are not passes.
Selection: P03,B03,B06,B07,M05. Per-lesson limit: 180 seconds. Scoped world regressions call fixture/math helpers separately; they are not UI journey actions.
Observed resolution: 1600 × 900; safe area: (x:0.00, y:0.00, width:1600.00, height:900.00). Requested 1600×900; Editor Game View may override the request.

| ID | State | Entry | Assembly | Run | Saved/reopen | Blocker |
|---|---|---|---|---|---|---|
| P03 | BLOCKED | True | True | True | False/False | Guided control absent/disabled: meter/pull+ |
| B03 | BLOCKED | True | True | True | False/False | Bounded UI journey timed out before saved result; observe:timer: / Chờ đủ thời gian ngâm trước khi lấy mẫu. |
| B06 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| B07 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M05 | UI JOURNEY RECORDED | True | True | True | True/True |  |

## Scoped world regressions (separate from UI journey)
PASS Isolated storage retrieval: Stored ruler can be taken into the hand
PASS Isolated storage retrieval: Taking again does not lose storage return state
PASS Isolated storage retrieval: Cancel restores stored/inactive ruler and original tray pose
PASS Isolated storage retrieval: Storage retrieval never advances preparation
PASS Isolated storage retrieval: Changing selection returns previously stored ruler to storage
PASS Isolated storage retrieval: Stored ruler still uses validated explicit installation
PASS Isolated storage retrieval: Cancel after installation cannot re-stow fitted ruler
P03: entry checks=19, assembled checks=19, failures=0 
B03: entry checks=23, assembled checks=23, failures=0 
B06: entry checks=19, assembled checks=19, failures=0 
B07: entry checks=19, assembled checks=19, failures=0 
M05: entry checks=19, assembled checks=19, failures=0 
