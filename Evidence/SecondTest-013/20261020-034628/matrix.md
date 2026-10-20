# Actual queued touchscreen UI journeys
Editor synthetic input only; no physical Android/VR claim. NOT RUN fields are not passes.
Selection: P03,B03,M01,M02,M03. Per-lesson limit: 210 seconds. Scoped world regressions call fixture/math helpers separately; they are not UI journey actions.
Observed resolution: 1600 × 900; safe area: (x:0.00, y:0.00, width:1600.00, height:900.00). Requested 1600×900; Editor Game View may override the request.

| ID | State | Entry | Assembly | Run | Saved/reopen | Blocker |
|---|---|---|---|---|---|---|
| P03 | BLOCKED | True | True | True | False/False | Bounded UI journey timed out before saved result; instrument:meter:pull+ / Lực kế 10 N → Tăng lực kéo 0,05 N |
| B03 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M01 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M02 | UI JOURNEY RECORDED | True | True | True | True/True |  |
| M03 | BLOCKED | True | False | False | False/False | Place UI did not install meter |

## Scoped world regressions (separate from UI journey)
PASS Isolated storage retrieval: Stored ruler can be taken into the hand
PASS Isolated storage retrieval: Taking again does not lose storage return state
PASS Isolated storage retrieval: Cancel restores stored/inactive ruler and original tray pose
PASS Isolated storage retrieval: Storage retrieval never advances preparation
PASS Isolated storage retrieval: Changing selection returns previously stored ruler to storage
PASS Isolated storage retrieval: Stored ruler still uses validated explicit installation
PASS Isolated storage retrieval: Cancel after installation cannot re-stow fitted ruler
PASS Isolated friction recovery: P03 retry rejects reset before tile end
FAIL Isolated storage retrieval exception: System.InvalidOperationException: Run the prepared pull before changing force.
  at SecondTestWorldChecks.FrictionRetry (System.Action`2[T1,T2] check) [0x00166] in <Unity project>\Assets\VLABProduct\Scripts\SecondTestWorldChecks.cs:24 
  at SecondTestUiProbe+<Start>d__59.MoveNext () [0x005ba] in <Unity project>\Assets\VLABProduct\Scripts\SecondTestUiProbe.cs:528 
P03: entry checks=19, assembled checks=19, failures=0 
B03: entry checks=23, assembled checks=23, failures=0 
M01: entry checks=19, assembled checks=19, failures=0 
M02: entry checks=19, assembled checks=19, failures=0 
M03: entry checks=19, assembled checks=0, failures=0 
