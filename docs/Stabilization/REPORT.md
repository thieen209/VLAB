# VLAB stabilization and verification

13 September 2026 · Unity 6000.5.6f1 · branch `fix/vlab-final-unification`

## Result and scope

The repair restores the original Physics NearFar controller concept across the application, keeps experiment content on permanent laboratory benches, and gives the menu and instruction panels a shared Vietnamese VLAB presentation. The furnished Chemistry, Biology and Engineering rooms remain visible during supplementary lessons. The final full suite passed 95 PlayMode tests; the production suite passed all 46 checks after the final text-rendering change. All 108 EditMode tests passed. Android packaging is recorded separately below.

Physical Android, stereo comfort and ESP32/BLE behavior require a connected phone and controllers. None was attached during this run. Editor replay and simulated XRI devices establish software behavior only.

## Evidence before changing the project

The project was clean at `199f1cc` on `integration/unified-vlab-20260910`. Historical recovery folders were not changed. All seven attached reference images and all five images in `bugthatneeedtobefixed` were inspected. The original `d0c8c78` Physics rig uses XRI Starter Assets **NearFarInteractor**, not XRRayInteractor. The installed XRI 3.5.1 source was used to verify its APIs; no package upgrade was needed.

The fresh baseline passed 108 EditMode and 46 production PlayMode tests. Two new regression tests failed against that baseline: the molecule workbench disabled the real floor collider, and pause failed to recenter after a 90° head turn. The preserved baseline reports, failing regressions and screenshots are in `TestResults/Stabilization`.

## Root causes and repairs

| Cause | Repair and principal files |
|---|---|
| The fallback pointer recognized only XRRayInteractor, so it missed the original NearFar hands. Other labs had unrelated camera ownership. | `VLabCanonicalRig`, `VLabSharedPointer`, `VLabViewerRuntime` and `VLAB Player Rig.prefab` share the original native rig and select one active input route. |
| Enabling prefab interactors before a scene manager could create an automatic persistent XRI manager. | Create the scene manager first, bind the native groups/hands, and use manual manager creation in the XRI runtime settings. |
| Scene teardown restored interactors after their manager had begun destruction. | Application and pointer teardown no longer re-enable dying scene objects. Explicit resume/close still restores suspended state. |
| Supplementary activities disabled all room renderers/colliders and locomotion, then created an isolated stage along the gaze direction. | `VLabExperimentStation` defines serialized apparatus boundaries and a fixed content anchor. `VLabActivityWorkbench` suspends only that apparatus, preserves floor/movement/furniture and restores original rigidbody state. |
| Pause tested only a center ray and placed the panel only on opening. | `VLABApplicationUI` checks the complete panel volume plus center/corner sight lines, reduces distance/scale near geometry, and softly recenters outside the view cone using unscaled time. Camera near clip permits the close-wall placement. |
| Guided native selections did not feed Biology/Engineering's original semantic lesson actions. | `VLabNativeLessonBridge` attaches native XRI selection to the existing pick/place/activate logic, with native axis rotation and tracking-loss cleanup. |
| Physics entry loaded a giant hub console and each additive experiment supplied another table. | The base scene now owns one permanent workbench. The hub presents a compact prompt; all six apparatus scenes are selected through the shared menu. |
| Oversized legacy panels obscured apparatus, while supplementary scenes hid furnished rooms. | Compact, collapsible fixed instruction panels reuse live lesson callbacks; shared button styling, cyan ribbons and licensed Vietnamese fonts unify presentation. Original room assets remain visible. |

## Final architecture

`VLABMenuBootstrap` creates the scene-owned application, viewer and pointer. `VLabCanonicalRig` reuses the existing Physics origin or instantiates the shared variant of that same Starter Assets origin and adopts the calibrated camera without changing its world pose. Normal runtime has one active XR Origin, EventSystem and XRInteractionManager, with two original NearFar hands.

Native tracked hands use XRUIInputModule and tracked graphic raycasters. Desktop, phone and the existing decoded-controller provider share the fallback ray route; that route suspends native input while active. Cardboard and the existing phone head/input providers remain supported. No ESP32 firmware or BLE packet transport exists in this repository to validate or rewrite.

Pause suspends experiment input, releases active native selections, prioritizes menu interaction and preserves the player pose. Nearby geometry is checked with reusable non-allocating physics buffers. Resume restores the suspended state. Experiment changes retain the room and locomotion root; apparatus and its guide change at the station. Physics uses additive content scenes; the other supplementary activities use the shared workbench lifecycle.

## Laboratory and visual changes

- **Main Menu:** prominent elevated VLAB branding, four immediate laboratory entries, restrained cyan spatial ribbons, dark environment and thin pointer. Existing settings, help, information and navigation actions remain functional.
- **Physics:** clean permanent bench on entry; six shared-menu selections; compact side instructions retain current cues, measurements, hints, reset and the measured-results sheet.
- **Chemistry:** preserved the original room, titration apparatus and existing lesson logic. Water assembly and precipitation use the permanent bench with floor and movement intact. World signage is depth-aware.
- **Biology:** the original microscope, cabinets and room remain present during cell exploration. Intro/task/result guidance now occupies a compact collapsible side panel; the microscope view remains available.
- **Engineering:** original workbench, instruments and room remain present during gears and lever lessons. Guided circuit apparatus and its wires are suspended together, so they do not overlap supplementary apparatus.

The implementation uses existing assets and fonts. Reference imagery informed hierarchy, depth and cyan emphasis; no reference logo or branded artwork was copied. Optional free mode was not added during this mandatory stabilization pass.

## Acceptance checklist

See [the 20-item bug matrix](bug-matrix.json) for individual outcomes and test references, including the disposition of every source screenshot. The [interactive before/after review](review.html) uses actual Unity camera captures, with a selector and comparison slider. All 19 local links across its seven comparisons were checked. The browser tool blocked its local-file preview; camera images were inspected directly, so browser interaction validation is not claimed.

## Verification

| Verification | Result | Local completion time |
|---|---|---|
| Complete PlayMode suite | 95 passed, 0 failed | 01:41:24 |
| Final production journeys after bitmap-font fix | 46 passed, 0 failed | 01:46:49 |
| EditMode models/assets/contracts | 108 passed, 0 failed | 01:44:41 |

The 46 production cases are a subset of the full suite, not 46 additional unique tests. Authoritative XML and summary files are in `TestResults/Production`. The final material correction was followed by production rerun and visual inspection. An earlier test-runner launch aborted following compilation and produced no result; it is not counted as a pass. A first native-object test attempted Biology water placement before preparing its slide; the corrected test follows the actual lesson prerequisite.

Coverage includes actual native left/right UI presses in all five scenes, native menu actions, original guided object selection, decoded-controller UI and apparatus interaction, supplementary lesson completion/reset/restore, repeated lab return/reentry, all six Physics selections twice, floor/locomotion preservation, player-pose invariants, close-wall pause placement and head-turn recentering. Runtime exceptions fail the tests. A narrowly identified Unity AI account-service warning is retained separately as Editor environment evidence.

## Performance and hardware limits

`TestResults/Stabilization/editor-profile.json` records nine warmed Editor samples (menu plus each lab active/paused), mean/P95 frame intervals, Editor allocation counters and 400 repeated UI raycasts per sample. The warmed UI raycast path measured **0 managed bytes per call**. This profile was recorded with the full 95-test run, before the final legacy-font material correction. These measurements include test-runner and Editor overhead; they are not Android frame-rate or player-memory measurements. Biology and Engineering retain their existing 60 FPS cap.

The draw-call counter was unavailable, so the JSON's zero placeholder is not an actual draw-call measurement. Scene renderer totals include disabled objects and are not a proxy for draw calls. Physical stereo readability, sustained device FPS/thermals, BLE latency and dual ESP32 tracking remain unverified. `adb devices -l` returned an empty device list.

Device acceptance still requires: install the development APK on the intended phone; scan/calibrate the actual viewer; connect both real controllers through the existing transport; check each hand's menu/select/movement/rotation actions in all four labs; run repeated lab and experiment transitions; and record a sustained on-device Profiler session including CPU/GPU frame time, allocations, thermal behavior and controller latency. Inspect both eyes for text readability, clipping and comfort near benches and walls. Editor simulation cannot replace these checks.

## Android build

The final development build **succeeded with zero build errors** in 10 minutes 6 seconds. The APK was written at 01:59:10 local time on 13 September 2026.

- Artifact: `Builds/Android/VLAB.apk` — **86,358,152 bytes** (82.36 MiB).
- Packaged manifest: `com.DefaultCompany.VLAB`, version 1.0 / code 1, debuggable, minimum API 26, target API 36, ARM64 only.
- APK Signature Scheme v2 verification passed; ZIP integrity passed.
- SHA-256: `79ed4c09cf8694bcfbebcfb3c01f04dce9b8aef975fca1dc787f13292800f271`.
- No device was attached, so installation and execution on a phone are not claimed.

The APK's measured file size above is separate from Unity's aggregate build-size statistic. Machine-readable identity, packaged manifest evidence and signature verification are in `TestResults/Stabilization/android-apk.json`, `apk-badging.txt` and `apk-signature.txt`.

 `Tools/BuildAndroid.ps1 -UnityEditor <Unity.exe> -Development` invokes the existing validated build path with an explicit development flag. Configuration is Cardboard, landscape, ARM64 IL2CPP, OpenGLES3 and API 26–36. The launcher scopes ASCII Java temporary paths to the build process to support the accented Windows account name.

## Files and handoff

Shared runtime changes are under `Assets/VLAB/MainMenu/Runtime`; scene setup is in `Assets/Editor/VLabStabilizationSetup.cs`. Chemistry and DemoLabs adapters preserve their existing lesson APIs. Tests under MainMenu and Physics retain scientific lesson checks and add the missing lifecycle/input regressions. No dependencies were introduced.

Runtime, scene, test and build changes are committed as `f175fcbe3ae5514ce06f77fa7c59b98ba45c453c` (`Unify VLAB controllers, permanent workstations and spatial guidance`). The subsequent evidence commit contains this report, the acceptance matrix, XML results and camera captures. [Changed files](changed-files.txt) lists the exact 59 source/configuration/test paths from the runtime commit.

Generated test scenes were removed, incidental font/quality test changes were restored, and added YAML whitespace was cleaned. Detailed Editor/build logs and the APK remain available locally under ignored paths; test XML, summarized build evidence and review images are version controlled. No remote push was performed.
