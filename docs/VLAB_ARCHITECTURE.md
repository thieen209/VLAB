# VLAB runtime architecture

Source snapshot: 12 September 2026. This document describes implemented code, not a test or release certification. See the repair report and test matrix for separately recorded execution results.

## Project and scene ownership

The project targets Unity **6000.5.6f1**, Input System **1.20.0**, XR Interaction Toolkit **3.5.1**, XR Management **4.5.4**, and Google Cardboard XR Plugin **1.35.0**. URP is installed, but `ProjectSettings/GraphicsSettings.asset` currently has no custom render pipeline assigned; the supplementary apparatus uses built-in `Standard` materials.

`VLABMenuBootstrap` subscribes to scene loading and creates one scene-owned host containing `VLABApplicationUI`, `VLabViewerRuntime`, and `VLabSharedPointer` in `Menu`, `PhysicsLab_Base`, `ChemistryLab`, `BiologyLab`, and `EngineeringLab`. It checks the loaded scene for an existing application UI before creating another host. This shell is not a new persistent camera or player rig.

`PhysicsLab_Base` retains the physics room, camera, input and scene flow. Its hub or one experiment loads additively. The other subjects retain their existing scene composition and lesson controllers. Application navigation uses single-scene loading between subjects; it checks whether the destination is in the build before starting. Pause state is restored before a successful scene transition. A failed destination offers a recoverable error screen.

`VLabMobileVrMode` is the intentionally persistent, duplicate-guarded owner of Android viewer mode. Its role is separate from the scene-owned UI and pointer.

## Shared input boundary

`Assets/VLAB/Core/Input/InputManager.cs` consumes `IVLABInputProvider`. Providers expose a semantic `VLABInputState`, an interaction button and reset input. The manager sanitizes axes and trigger values, produces button edges, and releases previous input when a provider changes.

Two state views serve different consumers:

- `RawState` preserves sanitized provider state before experiment blocking, allowing pause menus to remain operable.
- `CurrentState` suppresses experiment input while `BlockExperimentInput` is set. `TranslationLocked` separately suppresses movement, for example in the main menu.

`IVLabRayProvider` is an optional extension for a decoded controller orientation. `InputManager.PointerRay` requests its world ray when available; otherwise it uses the camera screen ray or the phone viewer's forward gaze. Existing physics, chemistry and demo adapters consume this common boundary rather than each inventing controller packets.

The project still contains different apparatus adapters, intentionally retained with their working lesson logic:

| Consumer | Responsibility |
|---|---|
| Physics `InteractionRaycaster` / `GrabController` | Select, manipulate and release physics apparatus through the existing interaction contracts |
| Chemistry `DesktopLabNavigator` / `DesktopLabGrabber` | Adapt desktop, phone and decoded controller actions to real XRI vessel selection, use and pouring |
| Demo `VLabInteractionDriver` | Microscope and LED workstations, placement zones and existing lesson progression |
| `VLabSharedPointer` | Common visible pointer and supplementary activity activation |
| `VLabComfortLocomotion` / `VLabComfortTurn` | Planar speed and analog turning policy consumed by the lab movement adapters |

This is shared input with adapters around existing experiments; it is not a claim that every original rig has been replaced with one identical prefab.

## UI routing and cancellation

The application canvas and activity instruction board use **world-space canvases**. Placement occurs when opening a panel or workstation, not continuously every frame. `VLabPointerUi` projects a controller ray onto each active world-space canvas and delegates the hit test to its graphic raycaster.

Native tracked UI uses the installed XRI UI components. When an optional ray provider owns UI input, `VLabSharedPointer` records and disables the other input modules and installs `VLabGazeInputModule`. It restores the recorded module states when that route ends. Phone viewer mode also uses this explicit-selection module.

`VLabGazeInputModule` sends press/release, drag, drop and scroll events. Provider changes, invalid rays, disabled input and module deactivation cancel a pending press/drag without completing a click. A release is required before accepting a new press after such a transition. Looking at a control alone does not activate it.

Pause sets `Time.timeScale` to zero, blocks experiment input, suspends the relevant locomotion/interaction adapters, removes native interactable layers, and hides other canvases/raycasters. Resume restores the recorded states. The menu continues to receive raw controller state while the lesson is paused. Duplicate Cancel delivery in the same frame is guarded in `VLABApplicationUI.NavigateBack`.

## Supplementary activity lifecycle

`VLabActivity` supplies `Title`, `Objective`, `Theory`, `Instruction`, `Result`, `Completed`, `Changed`, `Build()` and `ResetActivity()`. Its helpers create original geometry, readable labels and interactive parts. Parts use `LabInteractable`; `XRSimpleInteractable` plus `XrSimpleInteractableBridge` forwards native XRI selection into the same action. A part's collider must exist before XRI registration.

`VLabActivityWorkbench` is an overlay lesson in the current subject scene:

1. It snapshots original renderer, collider and behaviour states, preserves camera/controller subtrees, suspends conflicting lesson input and recovery, and freezes original rigidbodies.
2. It places an upright workbench along the current view ray without changing the camera pose. The board and apparatus remain fixed after opening.
3. It builds the selected activity, subscribes to `Changed`, and routes shared reset input to that activity.
4. It shows a short theory synopsis with a route to full Help, current instructions/results, reset, lesson selection and menu controls.
5. Closing removes subscriptions, destroys its own stage/materials, restores original rigidbody poses/velocities and flags, and restores the original lesson components. An explicitly created fallback XR manager belongs to this stage if the scene had no manager.

Derived gear activities also destroy their generated meshes. Original imported meshes are retained rather than regenerated or deleted.

## Lesson inventory

| Subject | Retained lessons | Added supplementary lessons |
|---|---|---|
| Physics | Pendulum, projectile, friction, photogate motion, spring, air-track momentum | None; six existing stations remain behind the physics hub |
| Chemistry | Titration, Daniell cell, copper electrolysis | Water molecule assembly; qualitative AgCl precipitation with a negative control |
| Biology | Onion specimen / microscope workstation | Seven-structure photosynthetic plant-cell cutaway |
| Engineering | LED circuit assembly | 12/24-tooth gear transmission; lever torque balance |

These are 11 retained lessons and 5 supplementary lessons. The new lessons validate composition, geometry, observations or mechanical relationships and expose reset and explanatory results. Their original simplified models deliberately do not simulate molecular dynamics, concentration-dependent precipitation equilibria, all cell ultrastructure, gear friction or full rigidbody lever dynamics.

## Android, native XR and desktop

`VLabMobileVrMode` controls the installed Cardboard loader through XR Management. A user may change viewer mode from Android's main menu; the application reloads that menu after a successful change to rebuild scene-owned input/pose components. The controller verifies the Cardboard display and input subsystems, has bounded startup waits, and reports failure instead of claiming stereo is running. The preference key is `VLAB.MobileVR.Enabled`.

In viewer mode, Cardboard supplies stereo and sensor tracking. `VLabHeadPose` applies a normalized XR head rotation with a neutral heading; recenter delegates to Cardboard. The viewer runtime removes competing pose ownership and exposes the viewer trigger, QR-device-parameter and close/recenter actions. A disconnected orientation is not treated as a positional tracking measurement.

Android normal-screen mode uses touch selection and drag-to-look beginning outside UI. The desktop editor keeps mouse/keyboard development paths and the installed XRI simulation assets. Menu head simulation uses right mouse, Home and F8. Native XRI controller visuals remain available when an enabled native pointer owns interaction.

## Controller transport boundary and persistence

`VLabControllerReplayProvider.Submit(VLABInputState, Quaternion, uint, bool)` accepts **already decoded** state. It normalizes orientation, rejects non-increasing sequence numbers while connected, applies a movement deadzone, and expires stale input. Calibration establishes orientation relative to the view heading; its hand position is an approximate view-relative anchor, not measured six-degree-of-freedom tracking.

No ESP32 BLE discovery/connection/GATT transport or verified firmware packet decoder is supplied by that API. The retained `ChemistryControllerBridge.ReceiveCommand(string)` is a separate legacy chemistry command entry point, not a new general BLE protocol. An actual controller integration must use the real firmware definition and invoke decoded-state submission from an appropriate Unity-thread boundary.

Preferences are stored in PlayerPrefs through `VLabComfortSettings`, chemistry `LabPreferences`, physics preferences, onboarding and viewer mode. The shared shell does not persist new lesson progress across leaving a lab. Do not interpret UI preferences as saved experimental measurements.

## Reproduction and remaining validation boundary

Open `Assets/VLAB/MainMenu/Scenes/Menu.unity` with the recorded Unity version. Complete onboarding if shown, enter a subject, and use **Menu → Chọn bài** to open its supplementary lessons. Use **Đặt lại**, **Lý thuyết đầy đủ**, pause/resume and **Về menu chính** to exercise lifecycle paths. The editor's F9 decoded-input replay is described in `VLAB_CONTROLLER_MAPPING.md`.

Physical lens distortion/IPD, sustained Android frame time and thermal behavior, Android pause/resume on hardware, native tracked-controller coexistence, BLE transport and ESP32 button/orientation calibration require the corresponding devices. Automated test/build outcomes belong in the test matrix and repair report, not this architecture description.
