# Physics Lab Environment, Lighting and Display TDD

## Scope

This pass upgraded the shared `PhysicsLab_Base` only. Hub and all six experiment scenes continue to load additively over the same room. Scientific experiment logic, XR interaction contracts and the physical-first progression rule were preserved.

## Source assets and import safety

- The supplied Qwantani 4K EXR is preserved outside Unity's `Assets` import tree under `SourceAssets/PhysicsLab/Environment/Exterior`.
- A 2048 x 1024 linear EXR is the runtime sky source. Importing the full 4K EXR caused a reproducible Unity out-of-memory failure during visual QA, so the smaller runtime derivative is intentional.
- The supplied GLB window was normalized non-destructively in Blender and exported as `VLAB_Architectural_Window.fbx`. The source file remains untouched.

## Test-driven checkpoints

- RED: the focused environment suite initially passed 2 of 6 tests and failed the four intended requirements: organized HDRI, normalized window FBX, probe coverage and equipment-display hierarchy.
- GREEN: the focused environment suite passed 6 of 6 after implementation.
- FINAL: the complete Edit Mode suite passed 49 of 49 and the complete Play Mode journey suite passed 5 of 5.
- The added Hub pointer test sends an actual UI click event through the Button, verifies the Pendulum scene loads, the Hub unloads, the shared Base remains, and the completed fade releases raycast blocking.

## Visual QA findings and fixes

- Ten deterministic camera views cover Hub entry, the window wall, both display cabinets and all six apparatus stations.
- The first capture found dark cabinets, undersized display pieces, and hanging pendulum/spring models near the ceiling because their imported pivots were not centered.
- Display items were enlarged and given clearer cabinet backing/shelf contrast.
- Hanging apparatus now aligns from renderer bounds instead of source pivots; pendulum bob, string and spring remain inside the intended manipulation zone.
- The central experiment table remains enlarged to 148 percent footprint and 115 percent height; apparatus remains enlarged to 128 percent for VR readability.

## Performance guardrails

- Lighting uses one shadow-casting directional light plus three non-shadow task/accent lights.
- Two baked 128-resolution reflection probes and one light-probe group cover the room.
- Each supplied window instance uses one transparent glass pane.
- Decorative display objects have no Rigidbody, collider or interaction component and matching duplicates hide when their experiment scene is active.

## Evidence

- `TestResults/EnvironmentFullEditMode.xml`
- `TestResults/EnvironmentFullPlayMode.xml`
- `TestResults/EnvironmentVisualCapture.log`
- `TestResults/EnvironmentVisuals/`

Actual phone head tracking, BLE-controller mapping and headset comfort remain hardware-dependent and were not claimed by this Editor-only pass.
