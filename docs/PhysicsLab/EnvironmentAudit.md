# Physics Lab Environment and Interaction Audit

Audit date: 2026-08-30

## Preserve

- Unity 6000.5.6f1 project, Input System, XR Interaction Toolkit 3.5.1 and the existing `VLAB.Core` provider/manager boundary.
- The `_SYSTEMS`, `_PLAYER`, `_ENVIRONMENT`, `_EXPERIMENTS`, `_UI`, `_DEBUG` separation introduced by `PhysicsLab_Rebuild.unity`.
- The reusable reset, attachment, measurement and 20 apparatus prefabs under `Assets/VLAB/PhysicsLab`.
- The existing `Home.unity` main menu and its Physics Lab / Join button flow.
- The existing `Interactable` layer (layer 7).

## Replace or refactor

- `PhysicLab.unity` contains only a cube, plane and light and is not a usable lab environment.
- `PhysicsLab_Rebuild.unity` has a sound hierarchy but only a flat ground plane and one light; the Scene view is overexposed and has no staged apparatus.
- The rebuild scene instantiates the XRI sample rig and XR Interaction Simulator directly. That is useful for package verification but is not a clean desktop simulator or future phone-head/BLE boundary.
- `PlayerController.cs` uses the legacy static `Input` API directly, locks the cursor unconditionally and couples movement/look to one device.
- `SimulatorInputProvider` currently exposes only E, R, 1 and 2. It does not centralize move, look, grab, drop, scroll or cursor state.
- `LabInteractable` also polls a keyboard key itself, duplicating the input path.
- The rebuild scene enables a permanent debug/FPS overlay by default, contrary to the student-facing UI direction.
- The main-menu join flow displays a fake hardware connection sequence and never loads a scene.
- Build Settings currently contains only `Home.unity`.

## Rendering and performance constraints

- URP 17.5 is installed, but no render-pipeline asset is assigned in Graphics or active Quality settings; the project currently renders with the Built-in pipeline.
- New environment materials must therefore use Built-in-compatible shaders and shared materials.
- Android quality defaults to Medium. The environment should use one shadow-casting key light, a small number of non-shadow accent lights, primitive/compound colliders and no mandatory post-processing.

## Architecture verdict

Upgrade the clean rebuild foundation into one `PhysicsLab_Base` scene containing the room, lighting, desktop rig, common input, interaction, reset and scene-flow services. Load exactly one lightweight hub or experiment content scene additively. This preserves the good hierarchy and core systems while preventing seven duplicated rooms, cameras, listeners or EventSystems.
