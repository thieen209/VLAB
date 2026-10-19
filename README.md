# Ngua Man VLAB

An offline Vietnamese/English smartphone-VR STEM laboratory built from the supplied FourLabs rooms, instrument library and experiment briefs. Android/Cardboard, touchscreen/mouse/keyboard and OS-paired HID gamepad controls share the same lesson state and local journal.

[Download the signed Android prototype APK](https://github.com/thieen209/VLAB/releases/download/v0.1.0-prototype-20261020/NguaManVLAB-0.1.0-android.apk). See the [prototype verification scope and remaining limits](docs/KNOWN_LIMITS.md).

APK SHA256: `B439B5D8221CBE4CE800131D25A218AAC3E3A74BE0204CD8DBACCE773260B45A`.

Open this directory with Unity **6000.6.4f1**, with Android Build Support, SDK/NDK and OpenJDK installed. Open `Assets/VLABProduct/Scenes/VLAB_Boot.unity`. The universal APK is `NguaManVLAB-0.1.0-android.apk`: ARMv7 + ARM64 / IL2CPP, API26 minimum /36 target, ETC2 textures. Package versions and Cardboard 1.36.0 are pinned in `Packages`.

The first launch runs Language → Privacy → Terms → Safety once; document changes renew only the affected acceptance. Progress, settings, observations and safe preparation checkpoints use checksummed local files with backup recovery. No account or laboratory server is required.

Home follows the supplied main.pdf sketch: four equal scientific silhouette cards with centred top titles, an original transparent centred logo and four round utilities. The resting background is a blurred capture of the current project's actual room. Focusing a card gently enlarges/tints its face and chooses the matching current-project laboratory photo without loading another scene; reduced motion keeps card size stable. Browser, results and linked notebook pages follow the relevant laboratory; activity and exploration show the actual 3D room and original instruments. The Home VR action enters Android Cardboard directly using saved or SDK-default viewer optics; optional QR calibration stays in Settings. Unity Editor offers the official XR Device Simulator.

Settings categories form one connected rail across the top, with one primary Apply action. They offer language, readable text, audio, graphics, themes, opacity, UI animations On/Off, gaze dwell/auto-selection and mouse sensitivity. Animation preference persists through reducedMotion; Off keeps nonessential motion disabled. Default opacity is80%, adjustable55–100%; saved choices are preserved. Controller mapping shows illustrated front/side views: select a physical control, choose its action, then Apply. Live callouts show the draft and conflicting actions swap safely. Supported input is standard OS-paired HID; the supplied custom ESP32 hardware has no documented GATT/packet implementation and is not certified by the diagram. Every lab browser shows all eight lessons, a current-room image and a dominant Start action. In-room identity uses the supplied transparent white banner and a lab name in the selected language only.

The notebook opens to writable paper pages with spiral binding, Vietnamese VR typing, optional lesson links and explicit offline saving; recorded measurements, conditions, interpretations and comparison plots have a separate section. Each of32 lessons has study material before measurement: formulas, methods, labelled illustrative examples, applications, reasoned self-checks, mistakes and source references. Answers stay hidden until the learner chooses Reveal answer. Study content is Vietnamese, explicitly labelled under the English interface. Profile schema2 migrates existing consent, settings and measurements without repeating onboarding. Guidance follows the selected stock apparatus variant; the compact titration guide leaves the actual instrument visible and hides the underlying station until closed.

## Verification and limits

The current required native receipts were checked before this publication checkout was prepared:

- `runtime-smoke.txt`: 2667 recorded checks; UTC 2026-10-19T21:37:19+00:00
- `visual-review-smoke.txt`: 2640 recorded checks; UTC 2026-10-19T19:39:26+00:00
- `learning-review-smoke.txt`: 445 recorded checks; UTC 2026-10-19T21:55:56+00:00
- `guidance-review-smoke.txt`: 1066 recorded checks; UTC 2026-10-19T21:38:35+00:00
- `appearance-smoke.txt`: 1121 recorded checks; UTC 2026-10-19T20:45:10+00:00
- `controls-review-smoke.txt`: 1214 recorded checks; UTC 2026-10-19T19:42:50+00:00
- `branding-review-smoke.txt`: 472 recorded checks; UTC 2026-10-19T18:34:29+00:00
- `apparatus-layout-smoke.txt`: 3692 recorded checks; UTC 2026-10-19T21:19:35+00:00
- `rendering-review-smoke.txt`: 103 recorded checks; UTC 2026-10-19T21:10:36+00:00
- `launcher-mesh-probe.txt`: 4 original-barrel angle checks; UTC 2026-10-19T20:48:09+00:00

Receipt counts cover32 bounded journeys and their stated Editor/synthetic scopes. They do not certify complete research coverage, user aesthetic approval or physical Android/headset behavior. Worktop checks cover bounded original apparatus layouts; four representative loose/assembled kit image pairs illustrate that scope. Every `80-*` learner view uses the enabled automatic learner camera. The earlier incorrect manual B04 capture is superseded by image83, its render-state and the rendering receipt; separate inspection views are supplemental. The launcher probe counts only its four angle PASS lines, not source/scene metadata. `docs/BUILD_STATE.md` is the authoritative artifact hash/checkpoint and supersedes historical hashes and numbered-run descriptions. See that file, `docs/KNOWN_LIMITS.md`, `docs/LEARNING_REFERENCE_CHECKS.md` and the sanitized release evidence for exact scope.

Editor checks: `VlabCoreChecks.Run`, `VlabSubjectChecks.Run`, `VlabRuntimeSmoke.Run`, `VlabRuntimeSmoke.Polish` and `VlabRuntimeSmoke.Appearance`. Checks use isolated local profiles, never a learner's production profile. `LabPanoramaCapture.Run` recaptures the Home and four subject room images. Do not run captures over unsaved scene changes.

## Android builds

Use `VlabProductBuild.AndroidDevelopment` for a development APK. For the release build, supply `VLAB_RELEASE_KEYSTORE` and `VLAB_RELEASE_PASSWORD` only in the local build process environment, with alias `nguaman-vlab`, then run `VlabProductBuild.AndroidRelease`. Signing keys/passwords and APK binaries are intentionally excluded from Git. Keep the private signing key backed up so future updates retain access to installed local progress.

The previous main (`df83980a12a241c5f23faf6827ee8c3612c9c247`) remains recoverable through Git history. No ancestor workspace or original supplied Unity project is replaced.

MIT applies to the project code; third-party assets retain their own licenses. Shipped credits: `Assets/VLABProduct/Resources/Legal/credits.txt`, font notices and package notices. Original VLAB identity and instrument provenance are retained.
