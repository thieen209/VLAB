# Actual queued touchscreen UI journeys
Editor synthetic input only; no physical Android/VR claim. NOT RUN fields are not passes.
Selection: P03,M03. Per-lesson limit: 180 seconds. Scoped world regressions call fixture/math helpers separately; they are not UI journey actions.
Observed resolution: 1600 × 900; safe area: (x:0.00, y:0.00, width:1600.00, height:900.00). Requested 1600×900; Editor Game View may override the request.

| ID | State | Entry | Assembly | Run | Saved/reopen | Blocker |
|---|---|---|---|---|---|---|
| P03 | BLOCKED | True | True | True | False/False | Bounded UI journey timed out before saved result; instrument:meter:pull+ / Lực kế 10 N → Tăng lực kéo 0,05 N |
| M03 | NOT RUN | False | False | False | False/False |  |

## Scoped world regressions (separate from UI journey)
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
P03: entry checks=19, assembled checks=19, failures=0 
M03: entry checks=0, assembled checks=0, failures=0 
