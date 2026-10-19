# Release evidence

Selected Unity Editor receipts and actual rendered images for the signed Android release. Raw Unity logs, isolated test profiles, learner data and signing credentials are excluded. `source-manifest.json` inventories the exact published source.

Subject checks exercise all32 bounded SDK journeys and saved/reopened attempts. Core checks cover model/lifecycle/file persistence. Visual review covers current UI, all-eight browser geometry and connected Settings rail. Learning checks cover32 guides and native text pagination. Runtime checks cover all32 UI launch/equipment routes and direct study entry/return. Controls checks cover diagram assignment, Cancel/Apply, persistent animation On/Off and synthetic input. Branding checks cover the original transparent white banner and single-language names. Layout checks cover all32 kits; the eight tabletop images depict four representative kits before/after assembly. `checks/android-artifact-checks.txt` identifies the universal ARMv7/ARM64 APK and verifies its package and signature. Receipt timestamps and counts are recorded in the release documentation; no historical run number represents current validation.

Images80 show all32 installed learner kits through the actual UI assembly route and enabled automatic learner camera, with no duplicate isolated fixture. Images81 provide two separate inspection angles per kit. Images79 are supplemental apparatus inspection views from a separate untracked camera; they preserve learner camera and source apparatus poses and do not represent the in-game viewpoint. The earlier manually requested B04 image was incorrect and is superseded by `83-B04-automatic-user-view.png`, `checks/Runtime/83-B04-render-state.txt` and `checks/Runtime/rendering-review-smoke.txt`: normal B04 rendering was proven with the enabled automatic camera and unchanged room materials. The launcher probe contributes exactly four angle PASS entries; scene/source metadata are not extra assertions. Its geometry gate does not imply unrestricted trajectory coverage. The P02 native check separately verifies a clear learner sightline to the actual floor target. Images are monoscopic Unity renders, including the official Editor XR simulation; they do not establish physical phone/headset comfort, stereo performance, optical fit or device installation. Research/model limits are recorded in `docs/KNOWN_LIMITS.md` and learning content is Vietnamese under both interface languages. Assertion counts do not imply user approval of visual quality.

Current receipt inventory; BUILD_STATE.md's current artifact hash/checkpoint supersedes historical hashes and run labels:

| Receipt | Count and scope | Modified UTC |
| --- | --- | --- |
| `core-checks.txt` | 333 checks | 2026-10-19T19:51:27+00:00 |
| `stock-stand-audit.txt` | 11 recorded checks | 2026-10-19T19:29:51+00:00 |
| `Subjects/first-integration.txt` | 23 PASS entries | 2026-10-19T21:11:28+00:00 |
| `Subjects/offline-records.txt` | 95 PASS entries | 2026-10-19T21:11:28+00:00 |
| `Biology/integration-checks.txt` | 1630 assertions | 2026-10-19T21:11:23+00:00 |
| `Chemistry/integration-checks.txt` | 231 PASS entries | 2026-10-19T21:11:23+00:00 |
| `Input/gamepad-integration.txt` | 29 PASS entries | 2026-10-19T22:46:52+00:00 |
| `Runtime/runtime-smoke.txt` | 2667 recorded checks | 2026-10-19T22:53:18+00:00 |
| `Runtime/visual-review-smoke.txt` | 2640 recorded checks | 2026-10-19T19:39:26+00:00 |
| `Runtime/learning-review-smoke.txt` | 495 recorded checks | 2026-10-19T22:54:56+00:00 |
| `Runtime/learning-guides.txt` | 360 recorded checks | 2026-10-19T22:54:56+00:00 |
| `Runtime/appearance-smoke.txt` | 1121 recorded checks | 2026-10-19T20:45:10+00:00 |
| `Runtime/additional-ui.txt` | 2615 recorded checks | 2026-10-19T22:52:48+00:00 |
| `android-release-build.txt` | successful universal build receipt | 2026-10-19T23:00:44+00:00 |
| `android-artifact-checks.txt` | 1 PASS entries | 2026-10-19T23:01:13+00:00 |
| `Runtime/guidance-review-smoke.txt` | 1066 recorded checks | 2026-10-19T21:38:35+00:00 |
| `Runtime/local-storage-ui.txt` | 49 PASS entries | 2026-10-19T22:54:39+00:00 |
| `Runtime/notebook-data-checks.txt` | 2 PASS entries | 2026-10-19T22:54:32+00:00 |
| `Input/controller-bindings-native.txt` | 25 PASS entries | 2026-10-19T19:41:46+00:00 |
| `Input/controller-bindings-persistence.txt` | 27 PASS entries | 2026-10-19T19:41:46+00:00 |
| `Runtime/controls-review-smoke.txt` | 1214 recorded checks | 2026-10-19T19:42:50+00:00 |
| `Runtime/branding-review-smoke.txt` | 472 recorded checks | 2026-10-19T18:34:29+00:00 |
| `Runtime/apparatus-layout-smoke.txt` | 3692 recorded checks | 2026-10-19T21:19:35+00:00 |
| `Runtime/observation-card-layout.txt` | 12 PASS entries | 2026-10-19T19:41:43+00:00 |
| `Runtime/rendering-review-smoke.txt` | 103 recorded checks | 2026-10-19T21:10:36+00:00 |
| `launcher-mesh-probe.txt` | 4 original-barrel angle checks | 2026-10-19T20:48:09+00:00 |
| `Runtime/83-B04-render-state.txt` | render-state metadata; no assertion count | 2026-10-19T21:14:05+00:00 |


APK structure check: `checks/android-page-size-checks.json` matches the final APK SHA256 and confirms ZIP16KB alignment plus required ARM64 ELF LOAD/RELRO alignment. This is structural compatibility evidence; no physical install or runtime/performance test is implied.
