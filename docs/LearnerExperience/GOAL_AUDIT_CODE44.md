# Learner-experience goal audit at code44

Audited 26 October 2026 against source commit `2321a27aa8c31ed16dc4b8d53ecb4897dabc1b06`, the active Unity checkout, and retained test receipts. This is an evidence review, not a new runtime or device test. APK code44 remains unchanged.

**Overall: incomplete.** An APK build and passing narrow tests do not prove the whole learner-experience goal.

| Requested outcome | Current implementation and evidence | Remaining boundary |
|---|---|---|
| VLAB boot with exact team credit | `VlabApp.Startup.cs`; `guide-checks.txt` checks visible startup and the exact text before Home. | Editor evidence; device launch is not verified here. |
| Separate complete procedure, current action, success and next action | `VlabApp.LessonProcedure.cs`, `LessonGuide.cs`; `../ProcedureCoverage/guide-checks.txt` has 165 passing assertions and retains all Vietnamese research steps across 32 lessons. Three procedure windows were exercised through native Editor UI. | Content coverage is not proof that all 32 research procedures are fully executable or understandable to learners. |
| Appropriate loose tools settle where released | `ReleasedEquipment.cs` uses gravity and a measured support surface, with upright constraints. `placement-checks.txt` has 178 passing assertions; later `../PhonePlacement` evidence covers phone pointer movement and release. | Deliberately no tipping, throwing or spills. Fixed assemblies and calibrated pipette supports retain their mounting rules. |
| Hand-performed aspiration, transfer, pouring and drops | C01 pipette and indicator, C04 qualitative drops, C05 initial pipette/water fill, C08 incremental pouring are implemented. `../TitrationIndicator/indicator-after.txt` has 158 passing assertions; `pour-checks.txt` has 92. | **Incomplete across chemistry.** Initial burette preparation, C05 serial dilution, C06 dilution and C07 acid preparation still include automatic transfers. Timing/drop amounts are illustrative, not calibrated real reaction kinetics. |
| Closer phone inspection | `VlabApp.cs` clamps phone inspection FOV down to 35 degrees; retained guide/batch checks exercise the closer view. | Not evidence of target readability on every phone or physical headset. |
| Dark circular microscope eyepiece with optical controls and hide option | `VlabApp.MicroscopeView.cs` has black backing, objective/coarse/fine focus and hide/show controls. Retained review checks cover B01/B02/B08 before an unrelated tray failure. | A lesson visualization, not a physical optical instrument validation. The review run as a whole failed; do not label that entire file PASS. |
| Drawing inside the notebook, offline | `VlabApp.Whiteboard.cs`, `VlabWhiteboard.cs`, notebook persistence and native Editor input checks; `batch-checks.txt` has 386 passing assertions. | Synthetic VR input is not a physical controller test. |
| Contrasting support for transparent instruments | `ApparatusRig.ContrastTrays.cs`, `BenchContrastTray.cs`; the dedicated corrected `trays-checks.txt` has 668 passing assertions across all 16 chemistry/biology lessons, including support and teardown. | This does not establish visual suitability for every other subject, lighting setup or device. |

## Next unresolved interaction: C07 acid preparation

Current `ChemistryDriver.Prepare`, C07 operation 1, still performs `WeighChips(); ConsumeBottle("acid", P("volume")); flask.TrySetVolume(P("volume"))` immediately. This is direct source evidence of an automatic transfer, despite the eight-lesson chemistry SDK integration suite passing. That suite verifies supported calculations/preparation fixtures; it does not verify that learners physically pour C07 acid.

The real integration boundary is not just another button:

- `LiquidPourTransfer.Vessel` currently accepts `VlabBeaker`, while the C07 reagent bottle and receiving flask use `VlabFlask`.
- `VlabFlask.TrySetVolume` rejects tilted vessels. Do not bypass that validation by secretly rotating a vessel upright to mutate its volume and then rotating it back.
- C07 installs its stopper at the flask neck before preparation. Manual acid loading must require an open receiver, then an explicit sealed gas circuit before contact and timing.
- Preserve finite source/receiver volume, acid concentration and amount, chip mass, stopped/repeated partial transfers, valid gas collection and invalidation after opening a reacting circuit.
- A passing geometry/quantity unit fixture must remain separate from an actual native-input pouring journey and physical-device results.

## Other unresolved transfers

`ChemistryDriver.SerialDilution()` and `DilutePhSample()` still prepare dilution through assisted instrument manipulation. C01 burette charging and priming also remain assisted. Their existing tests must not be presented as manual interaction coverage. These paths need their own input, geometry, conservation, interruption and guidance changes; changing a label to "manual" is insufficient.

No new build was needed for this audit. No physical Android, Cardboard or BLE test was performed. Keep the full goal active; do not mark it complete from code44 publication.
