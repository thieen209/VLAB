# VLAB Physics Lab Scene Architecture

## Runtime composition

`PhysicsLab_Base` is the only scene that owns the room, lighting, desktop player, camera, audio listener, EventSystem, shared input and fade transition. It remains loaded while exactly one content scene is loaded additively.

Content scenes:

- `PhysicsLab_Hub` — compact six-experiment selector and main-menu route.
- `Physics_Pendulum` — pendulum workstation.
- `Physics_Projectile` — projectile workstation.
- `Physics_Friction` — friction workstation.
- `Physics_PhotogateMotion` — photogate motion workstation.
- `Physics_Spring` — spring workstation.
- `Physics_AirTrackMomentum` — air-track momentum workstation.

Each experiment scene contains only `_STATION`, its apparatus prefab instances, station UI and reset manager. It contains no duplicate room, camera, audio listener or EventSystem.

## Input and interaction boundary

`InputManager` owns the current `IInteractionInput` provider. `SimulatorInputProvider` translates keyboard and mouse for Editor testing. Apparatus implements device-independent interaction contracts (`IInteractable`, `IGrabbable`, `IResettable`, `IActivatable`), so a future BLE/XR provider can replace the desktop provider without rewriting experiment behavior.

Desktop mapping:

- WASD: move; Shift: sprint.
- Right mouse: lock cursor / look; Escape: release cursor.
- Left mouse or E: interact / grab; right mouse while holding: rotate.
- Mouse wheel: held-object distance; Q: drop/cancel; R: reset station.

The imported XRI Device Simulator remains available as a project asset but no longer auto-instantiates beside the Physics Lab desktop rig.

## Regeneration

Use `Tools > VLAB > Physics Lab > Build Polished Experience` to rebuild the shared materials, base, hub, six station scenes and Build Settings from the editor tooling. Existing model source files are not modified.
