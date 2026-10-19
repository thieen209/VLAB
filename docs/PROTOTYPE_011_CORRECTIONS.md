# 0.1.1 prototype corrections (2026-10-20)

The published0.1.0 prototype/tag remains available unchanged. This follow-up retains the same package/signing identity and uses version0.1.1/code2.

## Finished readings after a failed local save

Previously, measuring advanced the session to Measured before file saving. A failed save removed the in-memory record and cleared the checkpoint, while the activity button had no Measured retry path. The learner could not save that finished observation without repeating the experiment.

The app now retains the exact pending AttemptID/readings for this session, restores the prior checkpoint/in-memory record list after a failed save, and presents an explicit Retry saving reading action. A retry saves that same snapshot once without measuring again. Generic navigation/save paths cannot silently overwrite the pending sample; explicitly restarting requires discarding it. The pending snapshot is session memory, not a guarantee against force-closing the app when storage is unavailable.

Storage failure/recovery messages are localized and keep exception details in diagnostic logs. They say the save/reset outcome is unconfirmed: a backup-write failure can follow a committed primary write, and reset failure can follow a changed generation marker/primary. The UI does not promise that every failed reset leaves old records available. The file persistence algorithm itself is unchanged.

Native isolated tests inject both primary-temp and backup-temp write failures, invoke the visible UI retry, require one identical finished record, verify old committed bytes for the prewrite case, and cover partial primary commit, settings rollback/Cancel, partial reset and protected-load copy. New UI captures are supplementary to these assertions; no real-device storage durability is certified.

## P03 graph from recorded forces

The local P03 brief at research/experiments.py31-32 requires sliding force versus normal force. The notebook now defaults to actual saved normalForce(N) versus slidingForce(N), retaining mass as a selectable axis. It never substitutes mass*g for a missing observation. Only the P03 normalForce axis omits mass from controlled-group keys; friction coefficients/model conditions remain separate. Existing mass-axis grouping is preserved.

Two-mass grouping/fit and missing-data tests use explicitly synthetic saved-record fixtures. A separate native check reopens the existing profile's one actual P03 record and requires a valid stored N/F pair and a plotted point for every valid pair. This does not claim two new native apparatus trials, calibrated friction or new selectable physical surfaces.

## B04/B05 gap investigated and retained

B05's local brief explicitly controls reaction temperature; both current lessons instead transfer an equilibrated aliquot to an external flask, which then cools. This remains a research limitation. The original bath holder bore is85.26mm while the supplied reaction flask is86mm, the native bath mounting API validates VlabBeaker only, and no supplied gas-tight70mm beaker closure was found. A truthful correction requires a supported flask/holder integration with real water-contact/clearance and gas-route checks. No external flask is falsely treated as remaining in the bath, and no mesh-fit guard was relaxed.
