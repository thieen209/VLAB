# Integration validation — 10 September 2026

Authoritative project: `VLAB`, Unity 6000.5.6f1. Menu source: `VLAB(UI)`. Chemistry source: `VLABChemistry`. Root Physics, Biology and Engineering retained.

## Executed checks

| Check | Observed result |
| --- | --- |
| Compilation in Unity | Passed after integration, pause safeguards and spatial Chemistry controls |
| Edit Mode suite | **107 passed, 0 failed** (`TestResults/Unified/final-editmode.xml`) |
| Full Play Mode suite | **41 passed; 1 failed because Unity AI's account service logged an unrelated timeout warning** (`final-playmode.xml`) |
| Affected Biology test, isolated | **Passed**, with its original assertions and log checks unchanged (`final-biology.xml`) |
| Final all-lab journey | **Passed in 54.68 seconds**, including pause safeguards, Editor mouse/simulator ownership and all three spatial Chemistry lesson panels (`final-journey.xml`) |
| GUID dependency audit | **0 duplicate GUIDs; 0 missing GUID references** across all 13 configured scenes (`audit.json`) |
| Scene component audit | **0 missing scripts** (`TestResults/Unified/scene-validation.txt`) |
| Android device available for deployment | None listed by `adb devices -l` |

All 42 distinct Play Mode cases have passed across the full and focused runs. The full-run account-service warning remains visible in the original XML; no test assertions or log checks were disabled to hide it. Simulated-controller haptic warnings and Editor cloud-service diagnostics are not evidence of a connected physical controller.

The journey launches Physics, Chemistry, Biology and Engineering, pauses/resumes, opens shared settings, returns to the menu and re-enters Chemistry. It checks single UI/EventSystem/AudioListener ownership, stationary menu translation, Editor head rotation/recenter, paused locomotion and rejected Chemistry commands while paused. The new Chemistry controls invoke the actual lesson controllers: the test accepts PPE in titration and advances/resets both Daniell and electrolysis lessons on each Chemistry visit. Dedicated suites exercise Physics mechanics, Chemistry calculations/pouring/grabbing/teleporting and simulated controller input, Biology specimen preparation/focus/identification/reset, and Engineering circuit assembly/resistance behavior.

Chemistry input failures were reproduced in the donor project. The test fixture routes synthetic keyboard/mouse devices to an unfocused Game view and restores the original Editor input settings afterward. A separate real crouching bug was repaired by synchronizing the resized upright capsule before movement. Two pre-existing Physics scene failures were repaired by restoring three shared-material glass panes and the missing console base, without regenerating the laboratories.

## Rendered review

Reviewed the home screen, four lab cards, legal onboarding and document views, all four lab views, pause/settings, audio/viewer/graphics tabs, and the titration/Daniell/electrolysis spatial controls. Captures are in `TestResults/Unified`, `TestResults/MenuUI` and `TestResults/SpatialUI`. The spatial UI has cyan gradient headers, dark surfaces, clear selected states and no observed text clipping or control overlap in these captures. Camera captures do not include every legacy IMGUI overlay.

A real mouse interaction in the Unity window exposed the package controller simulator disabling menu mouse input. The Editor menu now suspends that simulator, hides its unused controller visuals and explicitly enables mouse input. The updated automated journey passed, and a subsequent computer-control click on “Vào phòng thí nghiệm” opened the four lab cards successfully.

## Android build

**Succeeded with 0 errors** through `Tools/BuildAndroid.ps1` and `VLabUnifiedBuild.BuildAndroid`. The final cached build took **1 minute 24.44 seconds**; this is not a clean-build benchmark. The APK is `Builds/Android/VLAB.apk`, **72,706,243 bytes** (72.7 MB). Unity's reported 1,304,534,616 build bytes include build content/intermediates and are not the APK file size.

Verified the delivered APK's v2 signature, 16 KB package alignment, application ID `com.DefaultCompany.VLAB`, API 26 minimum / API 36 target, ARM64 ABI, Cardboard native plugin and subsystem manifest, and IL2CPP runtime. Signing uses the existing **Android Debug** certificate; this is an installable test/demo APK, not a store-signed release. Evidence: `apk-verification.txt`, `apk-manifest.txt`, `apk-native-libraries.txt`, `apk-sha256.txt` and `android-build.txt` under `TestResults/Unified`.

SHA-256: `A7ACBA46CD1B9EBAC942A8E25099E434544A1EFE2309CEC0A857F4C0E7488B56`.

The build used the synchronized source copy at `C:\VLAB-build-20260910` after E: ran out of space. An initial copy inside the accented Windows profile was rejected by Android tooling. Java then failed its local socket connection; an ASCII temporary directory inherited before Unity starts fixed the complete build. The launcher scopes TEMP/TMP/Java options to its own process tree. Final build log: `Logs/Integration-history/android-build-success.log`. The authoritative source and delivered APK remain in `VLAB` on E:.

## Performance and device limits

The menu uses one static mesh for distant points, three thin orbit lines, disabled room renderers/lights and no HDR/postprocessing pass. Graphics presets control antialiasing, shadow quality/distance and pixel lights. Android is configured for ARM64 IL2CPP, OpenGLES3 and Cardboard; the menu shader is explicitly retained for player builds. These are implementation changes, not measured device FPS improvements.

No smartphone stereo alignment, sensor/recenter behavior, thermal stability, controller latency or BLE connection has been physically tested. No production BLE transport or ESP32 firmware exists in the inspected sources; the existing input abstraction and command receiver are preserved. Complete the device checklist in `README.md` before declaring hardware release readiness.
