# Physics Lab Validation Report

Validation date: 2026-08-30  
Unity: 6000.5.6f1

## Automated evidence

- EditMode: **28/28 passed**, zero failures (`Temp/PhysicsLabEnvironmentEditMode.xml`).
- PlayMode regression after direct-scene bootstrap fix: **2/2 passed**, zero failures (`Temp/PhysicsLabFixPlayMode.xml`).
- PlayMode path exercised: load `PhysicsLab_Base`, auto-load Hub additively, cycle through all six experiments and back to Hub, and preserve one camera/audio listener/EventSystem at every stage.
- Direct-launch path exercised: open Hub and every experiment scene individually, automatically load `PhysicsLab_Base`, and verify camera, audio listener, EventSystem, input manager and desktop rig are present.
- Scene tests cover all eight Physics Lab scenes, Build Settings, missing scripts, missing renderer materials and disabled XRI simulator auto-start.
- Prefab tests cover the 20 required apparatus prefabs, runtime component presence and invalid dynamic non-convex MeshCollider combinations.

## Interactive Editor verification

- Opened `PhysicsLab_Base` and entered Play Mode.
- Confirmed the polished room and compact Vietnamese six-experiment selector render correctly.
- Confirmed selector button loads `Physics_Pendulum` with fade and the same base room.
- Confirmed pendulum workstation, string, ruler, reset button and Hub route are visible and staged on the experiment table.
- Confirmed right-click mouse-look locks input and Escape releases it; WASD input was exercised in Game view.
- Confirmed direct Play Mode from `PhysicsLab_Hub` now loads `PhysicsLab_Base` before the first playable frame, removing the `No cameras rendering` state.
- Confirmed the temporary XRI Device Simulator now auto-starts in Editor Play Mode as requested.
- No recurring gameplay exception was observed during the verified flow.

## Remaining validation for the next scientific pass

Mobile Android frame profiling, phone head-tracking, BLE hardware input and quantitative experiment accuracy require their target hardware and the deferred scientific systems.
