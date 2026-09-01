# Physical Interaction Repair — TDD Evidence

## RED

The first architecture tests failed because the project had no sourced physical-progress core, no live-only trial recorder, no dynamic release mode and no automatic snap controller. A later safety test also failed because a newly created grabbable did not expose held state until its cached Rigidbody reference existed.

The RED tests were written in `Assets/VLAB/PhysicsLab/Tests/Editor/PhysicalLabArchitectureTests.cs` before the corresponding production components were completed.

## GREEN implementation

- `PhysicalExperimentCore` rejects unsourced events and preset measurements.
- `ExperimentPhysicalController` has no public `PrimaryAction`, parameter plus/minus or `PerformTrial` path.
- `LabGrabbable` supports explicit dynamic release and held-state reporting.
- `LabSnapController` accepts only compatible attachment types.
- Reset detaches snapped objects and restores their initial parent/pose.
- Result generation stays locked until the physical sequence reaches measurement and three live samples have been recorded.

## Final regression result

- Edit Mode: 41 total, 41 passed, 0 failed.
- Play Mode: 3 total, 3 passed, 0 failed.
- Persistent reports:
  - `TestResults/PhysicsLabEditMode.xml`
  - `TestResults/PhysicsLabPlayMode.xml`
  - `TestResults/PhysicsLabEditMode.log`
  - `TestResults/PhysicsLabPlayMode.log`

The Play Mode journeys load the shared Base from every content scene, cycle Hub and all six experiments, assert one camera/audio listener/EventSystem, reject wizard UI objects, exercise physical pendulum progression, snap a real mass to the friction block and glider, and select the inelastic collision accessory.
