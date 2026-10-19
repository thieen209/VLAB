# Ngua Man VLAB delivery state

Authoritative release checkpoint: 2026-10-20T06:01:51+07:00, Asia/Saigon. **The artifact hash and receipt table in this file supersede every earlier build hash, numbered-run checkpoint and release-state description.** Earlier audit entries remain historical evidence, not current acceptance. Unity6000.6.4f1, universal Android ARMv7+ARM64/IL2CPP, min API26 / target36, ETC2 textures, Cardboard1.36.0. The separate Ngua Man VLAB project preserves the original FourLabs project, rooms and supplied apparatus geometry.

## Final source and artifact

Signed universal release APK: `Builds/Android/NguaManVLAB-0.1.1-android.apk`, 114,396,821 bytes. SHA256 `A7E485C58EF5D2D1E28BEEED003E15EB3F952ADE2060E6FE24A146046C3FAEF0`. The build completed with zero errors; `Evidence/android-artifact-checks.txt` records aapt package details, both ABI/IL2CPP entries and apksigner verification. The completed Unity build receipt records both target architectures and ETC2. This artifact supersedes earlier ARM64-only APKs and includes the current menu, notebook, settings, controller diagram and input changes. Private signing material remains outside the project/repository; custom signing settings are restored after building.

The publication checkout mirrors Assets, Packages, ProjectSettings and docs. Its sanitized `Evidence/Release20261019` contains selected receipts/screenshots and a hash inventory, not learner/test-profile JSON or private credentials. Git publication success is reported separately after remote verification; this file alone is not a remote-push receipt.

## Native evidence and scope

- Offline apparatus evidence records all32 bounded native SDK journeys, eight per lab, and 47 coexisting saved/reopened attempts with bidirectional reading snapshot isolation. See EXPERIMENT_FINAL_AUDIT.md.
- Core: 333 checks; separate Biology integration: 1630 assertions. These are model/lifecycle/SDK evidence, distinct from native UI or physical hardware.
- Visual review: 2640 recorded checks including saved-profile preservation, actual controller-ray Home routes, bilingual title fit, settings, personal writing and readable B01 outputs. All eight lessons are visible together in each laboratory browser.
- Learning: 495 recorded checks across32 lesson-specific Vietnamese guides, formula/pagination checks and normal/large text. Illustrative examples are labelled and historical readings preserved; English UI labels explicitly identify Vietnamese content.
- Appearance: 1121 recorded checks for themes, opacity, saved comparisons, current-project panoramas and guidance cues.
- Runtime: 2667 recorded checks including first launch, all32 UI launch/equipment paths, direct study entry and correct return to the selected browser item.
- Guidance: 1066 recorded checks for actual P01/C01 instruction callbacks, manual preparation targets and paused-clock restoration. See GUIDANCE_QUALITY_AUDIT.md.
- Controls: 1214 recorded checks, 25 native HID PASS entries and 27 persistence PASS entries. The illustrated front/side controller lets learners choose a physical control then assign an action, safely swapping its prior owner. Apply/Cancel remains explicit. UI animations On/Off persists through the existing reducedMotion setting; Off disables nonessential movement.
- Branding: 472 recorded checks for the original transparent white banner, ceiling/board fit and one laboratory name in the selected language. Layout: 3692 recorded checks for real-worktop support; four representative loose/assembled kits are photographed, while native fixture coverage is broader.
- Rendering: 103 recorded checks for normal B04 enabled-camera rendering, unchanged original room material/texture references and preservation of learner data. The launcher probe separately passes 4 original imported-barrel angle checks (0/45/85/90 degrees); its scene/source lines are metadata and are not counted as assertions. These geometry checks do not expand the bounded scientific trajectory model.
- Title font: finite Vietnamese/English title glyphs verified in native TMP and baked into a static Chakra Petch Bold atlas. Its OFL notice and source are included.

Exact receipt file timestamps and mechanically derived counts (not historical run labels):

| Receipt | Count and scope | Modified UTC |
| --- | --- | --- |
| `Runtime/local-storage-ui.txt` | 49 PASS entries | 2026-10-19T22:54:39+00:00 |
| `Runtime/notebook-data-checks.txt` | 2 PASS entries | 2026-10-19T22:54:32+00:00 |
| `core-checks.txt` | 333 checks | 2026-10-19T19:51:27+00:00 |
| `Biology/integration-checks.txt` | 1630 assertions | 2026-10-19T21:11:23+00:00 |
| `Runtime/visual-review-smoke.txt` | 2640 recorded checks | 2026-10-19T19:39:26+00:00 |
| `Runtime/learning-review-smoke.txt` | 495 recorded checks | 2026-10-19T22:54:56+00:00 |
| `Runtime/guidance-review-smoke.txt` | 1066 recorded checks | 2026-10-19T21:38:35+00:00 |
| `Runtime/appearance-smoke.txt` | 1121 recorded checks | 2026-10-19T20:45:10+00:00 |
| `Runtime/runtime-smoke.txt` | 2667 recorded checks | 2026-10-19T22:53:18+00:00 |
| `Runtime/controls-review-smoke.txt` | 1214 recorded checks | 2026-10-19T19:42:50+00:00 |
| `Runtime/branding-review-smoke.txt` | 472 recorded checks | 2026-10-19T18:34:29+00:00 |
| `Runtime/apparatus-layout-smoke.txt` | 3692 recorded checks | 2026-10-19T21:19:35+00:00 |
| `Runtime/rendering-review-smoke.txt` | 103 recorded checks | 2026-10-19T21:10:36+00:00 |
| `launcher-mesh-probe.txt` | 4 original-barrel angle checks | 2026-10-19T20:48:09+00:00 |
| `Runtime/additional-ui.txt` | 2615 recorded checks | 2026-10-19T22:52:48+00:00 |
| `Runtime/observation-card-layout.txt` | 12 PASS entries | 2026-10-19T19:41:43+00:00 |
| `Input/controller-bindings-native.txt` | 25 PASS entries | 2026-10-19T19:41:46+00:00 |
| `Input/controller-bindings-persistence.txt` | 27 PASS entries | 2026-10-19T19:41:46+00:00 |

## Current visual contract

The user's main.pdf sketch and supplied Beat Saber screenshot are compared in MENU_ART_DIRECTION.md. Home has four equal upright portrait cards, top-centred titles, scientific contour cuts that open to the exterior and remove both surface and frame behind overflowing figures, restrained focus tint/growth and four outlined round utilities. Default panel opacity is80%, adjustable55–100%; titles and scientific strokes retain contrast. Previously saved explicit settings are preserved. Home is placed facing the opening pose once and remains anchored under subsequent head movement. Reduced motion disables focus growth. The original transparent centred VLAB logo is lowered slightly and has no black backing. Home has no lesson-resume button.

The blurred Home/hover photographs come from this project's actual rooms. Activity/exploration use the actual3D apparatus; menus/results/notebook follow laboratory context. The eight-item browser has a clearly labelled current-room image and a dominant Start action. Settings use connected top navigation, large row switches, explicit stepper buttons and level indicators, and one primary Apply action; generic duplicated strokes are disabled to remove visible seams. Study pages remove the extra measurement-tab row, enlarge the reading area and show self-check answers only after the learner chooses Reveal answer. The physical-paper notebook preserves personal writing, Vietnamese VR typing, linked lessons and saved observations. Existing consent/settings/measurements migrate without repeating onboarding. See SOURCE_TO_PRODUCT.md, LEARNING_SOURCE_AUDIT.md, LEARNING_REFERENCE_CHECKS.md and UI_ASSET_PROVENANCE.md.

Release evidence includes controller front73/side77, white room banners71/72, loose/assembled worktop kits75/76 and focused apparatus setups78/79. All32 Vietnamese learner-view images80 use the actual enabled automatic learner camera; two separate inspection angles81 per kit remain supplemental views. The earlier manually requested B04 capture was incorrect and is superseded: normal B04 rendering is demonstrated by `83-B04-automatic-user-view.png`, `83-B04-render-state.txt` and the required rendering receipt. This capture correction is not a claim that the original room materials needed replacement. Placement is before manual preparation; state-specific contact tests and prepared C02/C03/B06/B07 views82 support the applicable scientific setup. These are monoscopic native renders; no assertion count or image inventory constitutes aesthetic approval or hardware certification. Separately derived supports are documented in DERIVED_M06_FIXTURE.md and DERIVED_SCIENCE_FIXTURES.md; original apparatus meshes and scales remain unchanged.

## Remaining acceptance

No Android phone or Cardboard viewer was attached. Installation/update, airplane-mode use on a phone, real stereo/head tracking/trigger/QR permissions, suspend/resume, comfort and device performance remain unverified. The supported SDK optical fallback retains distortion; it does not guarantee fit for every viewer. See CARDBOARD_ENTRY.md.

The complete research assignment is broader than these32 bounded journeys. P02 geometry-dependent launch height and the real room/ceiling/worktop limits on supported speed-angle combinations, B01 schematic optics, B02 untimed image states, B04/B05 cooling transfer, B07 crude precipitation and M06 fixed cam variants remain material limits in KNOWN_LIMITS.md. Some SDK diagnostics remain English; study content is Vietnamese under both interface languages with an explicit notice. The original source URLs are provenance, not a claim that every link was live-verified. Standard paired HID is integrated. The supplied ESP32-S3/BNO055 hardware folder contains no implemented custom BLE GATT/packet contract; the illustrated hand selection does not certify firmware hand separation, custom BLE transport or physical calibration. No flawless/10-out-of-10 or unrestricted production-readiness claim is made.


## Final native page-size structure

`Evidence/Release20261019/checks/android-page-size-checks.json` identifies this same final APK SHA256 and verifies ZIP16KB plus required ARM64 ELF LOAD/RELRO alignment. This check does not establish physical installation, launch, head tracking, comfort or performance.
