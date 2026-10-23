# VLAB Final Pass — checkpoint23Oct2026

Goal ACTIVE. This is progress, not full product/release acceptance. Previous goal turn changed authoritative source/docs; this turn fixed microscope UI/visibility and completed scoped UI evidence. Keep the original4-page PDF,11locked decisions,R01–R13 and32experiments intact.

## Latest batch — four quality tiers / observability

Progress: implemented real Low/Medium/High/Ultra URP settings, a conservative explicit hardware suggestion, manual Apply/save, and measured FPS/frame intervals/native CPU/GPU when supported. One runtime pipeline is reused and restored on teardown. Ultra keeps legacy graphics ID2 plus an extra flag; old profile payloads and notes tested without schema bump. Native downgrade not tested.

PhoneSettingsLayout now keeps tall-screen panels bounded/centered; tier titles/descriptions have distinct readable areas, selection rules stay inside targets, and the narrow EN tab says Style (fixtures updated). Counter never intercepts raycasts, stays away from actual Home/lab targets in flat/world simulation, and hides over browser/modal UI while collecting. No head pose, model geometry, package versions, legal content or signing key changed.

Evidence: `docs/QA/2026-10-23/graphics/README.md`, raw per-room baseline and Low/Ultra frame samples plus same-pose captures.183PASS quality/metrics run before final layout/occlusion refinement;95PASS final layout/persistence/counter navigation including VI/EN large text, four aspect/safe-area cases. After the final runtime edit:38input and36dialog regressions PASS,0FAIL;95final layout/persistence/counter checks PASS. Owned sequential regression process finished with all three exit codes0; no owned Editor remains. All performance is RTX4050Laptop/WindowsEditor1600×900, 10-second static windows, not Android60FPS or thermal acceptance. No motion-to-photon claim.

Publishing checkout now uses local `codex/graphics-quality` from public `052aad3` (the separate README/latest-source task pushed that baseline). New graphics work stays local; no APK or new public push. Source hashes are in the graphics QA folder. Quota92%→91% remaining in this batch. R11/Q08/PDF-16 remain IN_PROGRESS; full goal not accepted.

## Workspace / budget

- Active Unity: `E:\VLAB\4 LABS\Ngua Man VLAB`; Unity6000.6.4f1, URP17.6, InputSystem1.20, XRIT3.6.1, Cardboard1.36 pinned; Android min26/GLES3/ARMv7+ARM64.
- User JSON: `C:\Users\busyt\.codex\attachments\cd2e8244-61e1-4ecf-8009-637c3b441877\pasted-text-1.txt`. PDF ledger: acceptance.json. All4PDF pages were read/rendered earlier under `work/final-pass-20261023/bugs-page-*.png`.
- Root thread metadata confirmed gpt-6.1-sol/high; only this workflow, no agents/paid generation. Quota started95% remaining this turn; latest93%. Protect30%, wrap-up40%; recheck host limits before large work.
- Parent `E:\VLAB` Git index has unrelated staged files. Never bulk stage/reset there. Publishing checkout: `C:\Users\busyt\Documents\Codex\2026-10-19\rea\publish-vlab`, local branch `codex/graphics-quality`. Public main052aad3 includes the classifier and README work, as explicitly authorized in that separate task. The new graphics batch is LOCAL ONLY; no new public release/push.
- No owned Unity/QA operator remains after final C01 run. Revalidate real process handles before launch; UnityHub serve PID29196 is not an Editor lock. Old PID notes are obsolete. Source signing config/private keys were not copied to the publishing checkout.

## Implemented / evidence

- VlabApp: separate classifier/tool tabs; all6B01/B08 labels together and2B02 labels; image selection updates marker/count without replacing modal/image; correct filtered guide pagination; suppress accidental dwell on newly opened modal.
- PhoneDialogLayout: responsive tab caption bounds,2-column labels with height from actual preferred text, readable selected/count summary; status uses left area without reducing the controls viewport. No wholesale font shrink or model rescale.
- BiologyDriver/VlabApp: B02 inspection now overlays the supplied changing protoplast on the supplied fixed `_WallLayer_VIS` texture, same UV/rectangle and no extra ray blocking. Source model/texture/GUID unchanged. SDK states are illustrative, not calibrated osmotic kinetics.
- GuideCue: line-only source receives outline; mixed mesh/cord remains unobscured and unmodified. Original public one-argument ApparatusRig.Select API restored; internal no-notify grab path retained.
- Editor-only runner: direct C04 reagent/dropper route; bounded HUD refresh before declaring a button disabled; manual visual review commands and classifier-only mode explicitly separated from lesson completion. Selected lesson timeout accepts30–1800s (default180); C01 used1200s to allow repeated endpoints/manual review. No physical/protocol predicates relaxed.

Evidence under `Evidence/SecondTestUI/`:
- `20261023-044042`:24classifier UI PASS,0FAIL; VI1600×900 only, real queued touches, all choices directly reachable, no TMP overflow, tools preserved and B02 layer alignment.
- `20261023-041701`: C04/C07 saved/reopened,0runtime errors; C07 preparation assisted.
- `20261023-042435`: B01/B08 saved/reopened through actual image/label input (including100B08 cells). B02 BLOCKED there by missing wall; typography failures retained. Not final-source full protocols.
- `20261023-044332`: B02 baseline20cells→salt20→water recovery20→save/reopen,0runtime errors/scoped failures. Visual judgments from rendered pixels; no hidden answer lookup/state injection.
- `20261023-051833`: C01 three valid titres→record→reopen,0runtime errors/scoped failures. Actual UI valve pulses, mixing,30simulation-second indicator persistence, meniscus controls and retained titres. Bounded operator in `work/final-pass-20261023/drive_c01_ui.py`; self-check passed,3pulse cap and guards on real retained/fresh UI receipts. Earlier wrong caption/page and600s-timeout attempts remain in045515,045950,051444; they are not proven app failures.
- Aggregate32/32 saved/reopened across checkpoints is in `docs/QA/2026-10-23/ui-journeys-aggregate.json`. It includes assisted preparation and is NOT a final-revision32run, unassisted protocol or native acceptance. Original negative/scoped failure receipts remain.
- Final runtime-source regression:38input/gaze/touch PASS (`Evidence/FinalPass/interaction-checks.txt`, log`work/final-pass-20261023/classifier-input-regression-editor.log`);36dialog PASS including10mode switches (`dialog-checks.txt`, `classifier-dialog-regression-editor.log`). Only later source edit was the Editor-only timeout bound, compiled/used by final C01 run. No new compiler errors. SyntheticHMD is not phone-sensor proof.
- Earlier legal135PASS/47originalPDF pages remains scoped Editor evidence. Flat/world color swatches match in Editor; no native gamma diagnosis or compensating saturation change.
- Compact reports/screenshots/current source SHA256: `docs/QA/2026-10-23/README.md` and `classifier-source-sha256.json`. Prior long checkpoint archived in work, not deleted.

## Still required / next work

All broad gates stay OPEN/IN_PROGRESS. Only narrow explicit subcriteria were closed:
1. R05: implement/validate unassisted hands-on interactions for all32; generic assisted preparation is not the requested Guided Freedom. Check scientific formulas/units, contrast, retry/reset, saved conclusions against briefs. Run all32 again on the final source before release.
2. R02/R12: multi-aspect/safe-area and VI/EN full UI; new classifier currently only VI1600×900. Native stereo, legal and phone ergonomics still unverified.
3. R01 native washed colors/flicker; R03 menu/themes/identity; R04 all4room layout/materials/lighting; R06 VR viewpoints/navigation and small-tool visibility. Review actual runtime images, not concept renders.
4. R07 broader adaptive learning; R08 notebook/math/keyboard; R09 every input adapter and lifecycle. Preserve native head ownership; no forced VR camera/pose or animation as tracking evidence.
5. R10 real Android BLE scan/connect/subscribe, permissions/reconnect/timeouts, configurable protocol adapter/calibration/remapping/profiles/diagnostics. Source review says ESP32-S3/BNO055, leftESP-NOW→rightBLE; exact firmware/GATT/payload unknown. Read `work/final-pass-20261023/controller-source-review.json`; do not invent compatibility. Existing HID/mock is not BLE transport.
6. R11 four tiers and measured diagnostics are implemented with scoped Editor evidence above; native Android/stereo/thermal validation and final-source release profiling remain open. Profile before further optimization. UI currently serializes biology state for its summary at10Hz: inspect allocation when profiling. Preserve saved/rollback profiles and distinguish frame time from motion-to-photon latency.
7. R13 data/signing/GUID preservation and focused hygiene remain required. Earlier Windows project-Evidence File.Replace failure is still historical/unresolved; isolated QA storage does not repair it.

No new APK. Last good candidate remains0.1.7/code21 at `Builds/Android/Candidates/VLAB-0.1.7-VR-workstation-code21.apk`, SHA2565A84A18A5E99CAAEF226027D02F16E6CCAFFD80D6A11A9AD226F8F90880A34E9. Code17 rollback unchanged. Build final candidate from verified source with fresh version/output without overwriting good artifacts; test that exact APK, record SHA/source/device/performance and write honest HANDOFF. Do not mark the goal complete from these Editor checks.
