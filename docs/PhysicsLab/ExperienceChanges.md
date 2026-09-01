# Physics Lab Physical-First Repair Report

## Product rule enforced

The laboratory no longer uses UI buttons to perform a physical experiment step. Progress accepts only events carrying a live Unity `Component` source from apparatus interaction or simulation. Removing the contextual UI leaves locomotion, grabbing, adjustment, snapping, triggering, sensing, measurement and reset operational.

## Root causes repaired

- The XRI camera existed, but desktop locomotion, world ray interaction and grabbing were not attached to the active `XR Origin`. They now share the single XRI camera and the existing centralized `InputManager`.
- A large screen-space learning workspace captured attention and pointer input. It was removed and replaced by a compact world-space context panel with passive text raycasts disabled.
- The previous learning controller could submit deterministic trials from UI actions. Those controller/model classes were removed; live apparatus events now drive progression and trial recording.
- Released experiment bodies were being restored to a staged kinematic state. Pendulum bob, projectile, friction block, masses and gliders now use explicit physical release modes.
- Attachment points had no automatic compatible snap path. `LabSnapController` now detaches on grab and snaps compatible released apparatus within a bounded radius.
- Fixed furniture and room props previously carried general interaction components. Interaction is now limited to purposeful apparatus, instrument controls and physical surface samples.

## Physical experiment foundation

- Pendulum: physical bob displacement/release, live configurable joint length and period crossings.
- Projectile: visible angle handle, physical projectile socket, physical trigger, Rigidbody launch and live landing range.
- Friction: physical mass mount, three physical surface samples, hand-held force meter pull, real static/kinetic friction and one live force record per sliding run.
- Photogate: movable/snap-mounted gates, real trigger beams, timer ports and elapsed-time recording with derived velocity in the result sheet.
- Spring: physical mass attachment, real restoring force/damping, visual extension and live period peaks.
- Air track: axis-constrained low-friction gliders, physical mass and collision-accessory mounts, elastic/inelastic collision mode, photogates and live momentum.

## UI and recovery

- The contextual panel only shows experiment name, current cue, adaptive hint and live measurements.
- Settings are Physics-Lab-only: guidance level, action cue, interaction outline, auto-return and mouse sensitivity.
- Result UI remains hidden until three valid live measurements exist after the required physical sequence. It summarizes actual samples, actual parameters, average, available theory and deviation.
- `R` or the small reset utility releases held objects, detaches snaps, clears transient physics and restores the current trial without restarting the app.
- Lost objects are ignored while held and recover only after a delay; auto-return can be disabled.

## Scene and simulator flow

- One reusable `PhysicsLab_Base` supplies the room, XRI rig, camera, EventSystem and services.
- Hub and six experiment scenes load additively and can also bootstrap the Base when played directly.
- Unity XR Device Simulator remains enabled. Its native controller rays are now authoritative for world and UI interaction; the old mouse-centre crosshair, desktop raycaster and custom grab-controller path are no longer attached to the Base.
- WASD now feeds the XRI Starter Assets locomotion action and follows the active camera heading through `DynamicMoveProvider`, without adding a second camera rig.
- Tables are scaled to 148% width/depth and 115% height; apparatus is uniformly enlarged to 128% and spaced for manipulation.

## XR UI and readability

- The shared EventSystem now uses `XRUIInputModule`; the Hub selector is a world-space canvas with `TrackedDeviceGraphicRaycaster` so simulated controller rays can focus it.
- Every physical `LabInteractable` is backed by either `XRGrabInteractable` or `XRSimpleInteractable` and a small event bridge into the existing apparatus contract.
- Hub labels auto-size within readable bounds. Long Vietnamese result content wraps inside a masked, vertically scrollable viewport while its header and actions remain fixed.
- A Game View regression exposed the selector being behind the physical console. Its depth is now guarded by an Edit Mode test and all six experiment buttons remain visible.

## Verification

- Asset generation and validation passed for all 20 required prefabs.
- Edit Mode: 41/41 passed (`TestResults/PhysicsLabEditMode.xml`).
- Play Mode: 3/3 journeys passed (`TestResults/PhysicsLabPlayMode.xml`), covering direct scene bootstrap, hub/all-scene navigation, singleton camera/audio/EventSystem checks, removal of wizard controls, pendulum physical progression, friction/glider mass snaps and inelastic accessory mode.
- Final Play Mode log contains no `NullReferenceException`, missing-reference exception, duplicate-listener warning or runtime `No cameras rendering` message.
- A visual Play Mode pass on `Physics_Friction` confirmed the Base and camera render from a direct experiment launch; it also found and drove the fix for a mirrored world-space context panel.
- XR/UI Edit Mode: 45/45 passed (`TestResults/XrUiEditMode.xml`).
- XR/UI Play Mode: 4/4 passed (`TestResults/XrUiPlayMode.xml`), including native XRI selection activating a physical apparatus exactly once.
- A final visual Play Mode pass on `PhysicsLab_Hub` confirmed camera rendering, both simulated controller rays, all six unobstructed choices and removal of the centre crosshair.

## Still device-dependent

Headset/phone comfort, BLE-controller mapping and final hand/controller visual tuning require the target hardware. Formal scoring, teacher analytics and curriculum progression remain outside this repair.

## Shared environment polish follow-up

- Rebuilt `PhysicsLab_Base` as an organized reusable environment with dedicated Architecture, Windows, Furniture, Cabinets, Lighting, ReflectionProbes, LightProbes, Exterior and Branding groups.
- Integrated the supplied Qwantani sky as a memory-safe 2K runtime EXR while preserving the original 4K source outside Unity's import tree.
- Normalized the supplied window model through a non-destructive Blender export and placed three consistent north-wall windows with a single glass layer each.
- Preserved the two equipment cabinets and removed their decorative equipment models so only active experiment apparatus appears in the room.
- Added mobile-conscious lighting: one soft-shadow key, three shadow-free task/accent lights, two baked low-resolution reflection probes and one light-probe group.
- Visual QA corrected dark/undersized cabinet objects and source-pivot errors that placed the pendulum bob and spring near the ceiling.
- Every Physics Lab world-space canvas now uses the UI layer and a tracked-device raycaster; both Near/Far interactors cast against Default, Interactable and UI layers.
- Remote grabs now use dynamic attach, preserve the contact offset, keep object rotation independent from controller pose, use native depth translation and apply rotation only from deliberate manipulation input. Pendulum bobs and air-track gliders retain their constrained rotation.
- The dedicated panoramic sky material is serialized in Base, Hub and all six experiment scenes, preventing an additive content scene from replacing it with Unity's default procedural sky.
- Final focused regression verification passed 19/19 Edit Mode tests and 5/5 Play Mode journeys. Four cardinal sky renders plus the existing Hub, cabinet and six-station views provide visual evidence of the continuous 360-degree HDRI.
