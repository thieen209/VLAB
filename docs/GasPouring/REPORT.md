# C07 manual acid loading — code45

The learner opens the actual flask stopper, holds the HCl bottle and drags its mouth over the flask. Accepted flow reduces the finite source and increases the receiver by the same amount. Releasing stops flow and puts the bottle down; regrabbing preserves a partial dose. The prescribed dose must be complete before the existing weighing, sealing and contact sequence can advance. The guide switches from opening the stopper, to pouring, to confirming the completed step.

The stopper-opening control moves the supplied stopper onto clear, validated support. The existing seal step reseats its real socket before connecting the gas circuit. The phone gesture assists bottle tilt, height and flow rate; the user controls horizontal placement and start/stop. No supplied models are replaced or rescaled.

## Evidence

- `gas-before.txt`: failing baseline; preparation silently filled a stoppered flask.
- `gas-ui-first.txt`: C07 geometry, closed states, solid obstruction, finite volume, partial dose, native Editor touch/release/regrab, supported stopper control and later gas measurement passed. This combined run then failed C08's final timed observation assertion; the receipt is deliberately retained as a failed overall run.
- `integration-checks.txt`: all eight chemistry SDK fixtures after manual C07 loading. Instrument positioning and reaction-clock advancement are explicit fixtures, not device interaction evidence.
- `pour-paused-diagnostic.txt`: separate C08 attempt identifies the session as Paused during the observation wait. The test now resumes through the visible UI if interrupted; production focus-loss safeguards are unchanged.
- `gas-ui-after.txt`: final combined C07 and C08 native-input regression result, including any pause/resume recovery. All 176 assertions passed; the separate chemistry integration receipt contains 250 passing assertions.
- `images/`: actual C07 Editor runtime captures.

## Limits

Phone bottle tilt, height and rate are assisted. The liquid mesh follows the bottle's local axis while tilted; it is not a gravity-level fluid surface or spill simulation. Flow rate and reaction kinetics are illustrative. Later weighing, sealing and chip contact retain existing assistance. Unrecorded preparation restarts safely on reopening; recorded notebook data remains. Physical Android, Cardboard and BLE behavior was not verified in this patch. Other outstanding learner-experience requirements are not claimed complete.
