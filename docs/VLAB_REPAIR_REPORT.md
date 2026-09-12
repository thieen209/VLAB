# VLAB repair and completion report

Date: 12 September 2026. The authoritative Unity project is the `VLAB` directory. The initial user integration was preserved in commit `15d5247` before this repair.

Implementation commit: `866c644` — shared controller/UI routing, comfort and viewer lifecycle, original-lab repairs, five interactive lessons and their automated regression tests. Validation tools, reports and camera evidence are recorded in the following documentation commit.

## Scope and delivery

The implementation retains the four original lab scenes and their eleven experiments, adds five supplementary hands-on lessons, and places shared menus, controller input, comfort preferences and Android viewer mode around them. It preserves the existing built-in rendering pipeline, imported controller mesh, Vietnamese font, apparatus, microscope specimen renderer and experiment controllers.

Execution results and Android packaging are recorded separately in [VLAB_TEST_MATRIX.md](VLAB_TEST_MATRIX.md). This report does not substitute source inspection for device testing.

## Final recorded result

On September 12, all 108 EditMode tests and all 88 PlayMode tests passed. A later 46-case production subset passed after final visual adjustments; these are repeated PlayMode cases, not additional unique tests. None were skipped. The source inventory covers 40 scenes/prefabs with zero duplicate GUIDs or missing references; all 12 enabled build scenes have zero missing scripts.

The Android build succeeded with zero errors in **9 minutes 4.91 seconds** (incremental build, not a clean-build benchmark). The delivered `Builds/Android/VLAB.apk` is **72,814,223 bytes**, ARM64 IL2CPP, minimum API 26 / target API 36. APK v2 signature, 16 KB package alignment, Cardboard native plugin/subsystem manifest and IL2CPP libraries were verified with `Tools/verify_android.ps1`. Signing uses the existing **Android Debug** certificate; this is an installable test build. Unity's 1,307,771,065 reported build bytes are not the APK size.

APK SHA-256: `732bffe99f3f0ebc5c3b30358de6b5a3b790d8db3b0415d05a7e0c8d4ddb6f56`.

The Android device listing was empty. Installation, physical stereo/head tracking, BLE transport and sustained mobile performance were not tested. The interactive evidence review is `docs/Production/review.html`.

## Confirmed causes and repairs

| Confirmed problem | Repair |
|---|---|
| Each lab used its own screen or camera ray, ignoring decoded controller orientation | Optional `IVLabRayProvider` feeds the shared input manager; Physics, Chemistry, demo and supplementary adapters use that world ray |
| Pausing erased the same state needed by UI | Preserve sanitized `RawState` for UI while suppressing experiment `CurrentState` |
| Provider loss could leave held interaction active or turn a disconnect into a click | Release input edges on provider changes; cancel UI press/drag on disconnect, disabled input and module changes; require neutral release before a new press |
| UI press tests worked but controller drag/scroll did not have a complete event lifecycle | Shared module now supports initialize/begin/drag/end/drop, raw scrolling and click cancellation after drag |
| Chemistry secondary action repeated for every held frame | Debounce the semantic secondary action and reset the latch when the provider changes |
| Controller orientation inherited subsequent head yaw and rejected restarted packet sequences indefinitely | Keep a calibrated reference heading; normalize/reject invalid quaternions; accept a new sequence session after timeout |
| Pause panels and supplementary models were clipped with the existing downward-looking lab cameras | Place once along the current view ray, fit panels to the camera, and frame the combined board/apparatus without moving the head |
| Existing default TMP material has a black face color, making new labels unreadable on dark apparatus | Use the existing Vietnamese font with an explicitly white, owned shared text material |
| Original rigidbodies continued falling after the supplementary stage hid their colliders | Snapshot and freeze original bodies, suspend recovery/input, and restore pose, velocity, sleep/kinematic state and enabled flags on close |
| Native XR cached a reagent collider before it was replaced | Build the final hit volume before registering the XRI interactable; verify registration against the actual XR manager |
| Disabled demo drivers still received Reset/Drop/Menu events | Guard input callbacks; route active supplementary reset to its own lesson while keeping explicit reset APIs available |
| Original screen-space lesson dialogs did not participate in controller world-ray UI | Anchor lesson canvases in world space; retain transition veils separately; open the microscope observation board once in front of the viewer without forced head motion |
| Rear atoms in the molecule tray were obscured by front atoms | Use separate visible shelf levels, verified with real controller-ray selection |
| Retired Home scene survived in tests/build registration | Migrate assertions to the real Menu and all current lab routes; remove the dead build entry and preserve valid scenes when rebuilding Physics content |

## What each lab contains

| Lab | Retained functionality | Added functionality |
|---|---|---|
| Physics | Pendulum, projectile, friction, photogate motion, spring, air-track momentum; additive hub, resets, apparatus grabbing and measurements | Shared controller ray, fresh UI blocking, comfort locomotion, common help/pause/settings/return |
| Chemistry | Titration, Daniell cell and copper electrolysis; vessel handling, tap flow, measured transfers, original lesson logic | Water assembly with wrong-element/missing-atom/geometry checks; AgCl precipitation and negative control; common spatial chemistry controls |
| Biology | Onion slide preparation, mounting/clips, objective/focus/light controls, live specimen view and identification | Seven-structure leaf-cell cutaway, structure/function progression, wrong-answer feedback, rotation and reset |
| Engineering | LED circuit assembly, connection validation, polarity and current comparison | 12/24-tooth reduction/increase/reversal model and assembly; lever load/effort/fulcrum placement with torque balance and reset |

The supplementary lessons explicitly explain their scientific simplifications. They use discrete selection and placement; they do not claim fluid chemistry, finite-element mechanics, or microscopic realism beyond the documented models.

## Architecture and assets

- Runtime shell: `VLABMenuBootstrap`, `VLABApplicationUI`, `VLabViewerRuntime`, `VLabSharedPointer`.
- Input/comfort: shared input sanitation and edges, replay provider, canvas ray adapter, head-pose ownership, movement and turn policy, native XRI settings adapter.
- Activities: shared workbench/base and Biology/Chemistry/Mechanics implementations under `Assets/VLAB/MainMenu/Runtime/Activities`.
- Assets: reuse the installed right-controller FBX in `Resources/VLAB Controller Visual.prefab`; bind the existing Vietnamese TMP font in `VLABMenuAssets`.
- Scene content remains in the existing production scene files; the new lesson stage is created and destroyed by the scene-owned host.
- No new Unity package or rendering-pipeline migration is required. Cardboard 1.35.0, Input System 1.20.0, XRI 3.5.1 and XR Management 4.5.4 are retained for Unity 6000.5.6f1.

Detailed ownership and mappings are in [VLAB_ARCHITECTURE.md](VLAB_ARCHITECTURE.md), [VLAB_CONTROLLER_MAPPING.md](VLAB_CONTROLLER_MAPPING.md), and [VLAB_VISUAL_STYLE.md](VLAB_VISUAL_STYLE.md).

## Mobile VR and performance boundary

Android viewer switching uses the installed Cardboard loader and actual XR display/input subsystem start/stop, verifies both are running, persists only successful changes and reloads Menu to replace scene-owned pose/UI state. Normal Android mode uses touch selection/look. Mode changes are restricted to Menu.

The replay adapter accepts decoded semantic controller state; it is not BLE scanning, pairing or packet decoding. No physical controller protocol, GATT identifiers or firmware was supplied. No physical phone/headset/controller was available for sensor latency, optical alignment, reconnection, thermal, battery or sustained frame-rate measurements.

The built-in materials and render pipeline are retained. Equal-color activity parts share materials, text shares its owned font material, pointer raycast buffers are reused, native interactor references are cached, and the microscope renders when optical state changes. These are source-level measures; on-device frame time and thermal performance remain unmeasured.

## Validation evidence and limits

The repository includes repeatable EditMode/PlayMode tests, real camera captures under `TestResults/Production/Visuals`, a GUID/dependency inventory, scene validation and the Android build summary. Tests exercise both original experiments and new behavior rather than only checking that components exist. The test matrix gives actual results and individual case names.

The optional Unity AI editor package can emit an account-service timeout unrelated to the player. The local validation runner records that exact warning with its editor-package stack in `editor-environment.txt` and marks only that message expected. Application warnings/errors and assertions are not globally suppressed.

## Open and reproduce

1. Open this `VLAB` project in Unity 6000.5.6f1 and open `Assets/VLAB/MainMenu/Scenes/Menu.unity`.
2. Enter Play mode, complete the existing onboarding if needed, then choose a lab. Use its common Menu → Choose experiment to open supplementary lessons.
3. Mouse/keyboard remain available. In Editor, F9 enables decoded-controller replay; I/J/K/L aim, left click selects, and the documented controls operate movement/reset/menu. Calibrate from Settings → Controller.
4. Use the Unity Test Runner for EditMode and PlayMode suites. The local fixed-command runner also supports `edit`, `play`, `production`, `journey`, `tap`, `assets` and `validate` via `Library/VLAB-production-command.txt`.
5. Build Android with `Tools/BuildAndroid.ps1` and the installed Unity executable. Close this project's Editor first. The launcher uses ASCII temporary paths for the Android JDK without changing global environment settings.
