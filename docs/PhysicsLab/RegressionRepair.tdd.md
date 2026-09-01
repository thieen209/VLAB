# Physics Lab XR Regression Repair

## Scope

This repair preserved the existing XRI Device Simulator, controller models, scene-flow architecture and physical-first experiment systems. It changed only the decorative display, XR UI ray layers, native remote-grab policy and sky-environment persistence.

## RED evidence

`TestResults/RegressionRepairRed.xml` initially ran 19 focused Edit Mode tests: 14 passed and 5 failed. The failures reproduced the five requested regressions:

- decorative `EquipmentDisplay` remained in the Base;
- the Qwantani sky was not guaranteed when a content scene became active;
- controller-ray masks excluded UI and Default layers;
- world-space canvases were not consistently placed on the UI layer;
- native grabs snapped to the controller pose and followed controller rotation.

## GREEN repair

- Cabinets remain; the decorative display hierarchy and its generated models are gone.
- Near/Far controller rays cover Default, Interactable and UI layers. Every generated world-space canvas uses `TrackedDeviceGraphicRaycaster` on the UI layer.
- Native XR grabs use dynamic attach, preserve position offset, do not copy controller rotation, disable throw, and use kinematic smoothing. Native manipulation Y controls depth; manipulation X deliberately rotates unconstrained objects.
- Pendulum bobs and air-track gliders exclude free rotation so their physical constraints remain authoritative.
- Base, Hub and all six experiment scenes serialize the same `Skybox/Panoramic` Qwantani material. The flat exterior horizon prop is removed.

## Verification evidence

- Focused Edit Mode GREEN: 19/19 passed in `TestResults/RegressionRepairGreen.xml`.
- Full Edit Mode suite: 52/52 passed in `TestResults/RegressionRepairFullEditModeFinal.xml`.
- Play Mode journeys: 5/5 passed in `TestResults/RegressionRepairPlayMode.xml`.
- Build/generation: completed with no C# errors or exceptions in `TestResults/RegressionRepairBuildFinal.log`.
- Visual QA: 14 GPU-rendered views in `TestResults/EnvironmentVisuals`, including four cardinal directions from one position to verify the panoramic wrap.

The Play Mode suite covers direct launch of every content scene, Base bootstrap, Hub-to-all-six navigation, a real UI pointer click, completed fade release, physical experiment interactions and singleton camera/audio/EventSystem checks.

## Hardware boundary

The Unity XR Device Simulator path is validated. Phone head tracking, the custom BLE controller and headset comfort still require their target hardware and are not claimed as hardware-verified by this desktop run.
