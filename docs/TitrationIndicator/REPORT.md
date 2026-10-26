# C01 manual indicator — code44

After manually transferring the 10 mL aliquot, hold the preloaded phenolphthalein dropper and move its tip down over the actual flask mouth. Two simulated drops are required. Release stops dispensing; a partial dose can be resumed. A miss or blocked mouth cannot consume a dose, and the prescribed dose cannot overrun. Preparation and repeat-trial controls validate actual accepted drops instead of adding indicator silently.

The existing phone gesture assists orientation, height and pulse timing. On repeat trials, move the flask clear if the burette blocks its mouth. The original dropper model and squeeze feedback are retained. The receiving liquid display has a full-vessel collider: it is excluded from obstruction checks after checking the actual cavity and calculated liquid surface; glass and foreign obstacles still block dosing.

## Evidence

- `indicator-before.txt`: baseline fails because preparing the next step silently adds indicator.
- `indicator-after.txt`: actual-mouth geometry, missed target, blocking object, partial dose/release/regrab, dose cap, unchanged aliquot volume, queued native Editor touch, camera stability, and C01/C05 pipette/wash regressions pass.
- `integration-checks.txt`: all eight chemistry SDK fixtures pass, including three concordant C01 titres. These fixtures position instruments directly; they are not physical-device interaction evidence.
- `images/C01-manual-indicator-complete.png`: actual runtime after the queued-touch indicator sequence.

## Limits

The supplied teaching dose is two preloaded, uncalibrated simulated drops per trial. No mL, mol or real drop-size calibration is inferred; trace indicator volume is excluded from the model's aliquot volume. This is qualitative indicator handling, not fluid dynamics or a calibrated indicator chemistry model. Droplet flight is not physically simulated. C01 unfinished preparation still restarts on reopening, while retained titres remain saved. Burette charging, initial preparation in other chemistry lessons, and some placement/flow operations remain assisted. Physical Android, Cardboard and BLE validation was not performed. This change does not complete every outstanding learner-experience requirement.
