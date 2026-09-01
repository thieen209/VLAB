# XR Interaction and Responsive UI — TDD Evidence

## User journeys covered

- Enter `PhysicsLab_Hub` and immediately receive a rendered XRI camera, two simulated controllers and controller rays.
- Point a controller ray at world-space UI and use Unity XRI selection as the authoritative input path.
- Select, grab, release and activate physical apparatus without a mouse-centre crosshair or apparatus-specific keyboard polling.
- Move with WASD through the existing XRI locomotion action, relative to the active camera heading.
- Read short labels without overflow and review long Vietnamese result content inside a scrollable viewport.

## RED evidence

- The first Edit Mode run failed to compile because `XrSimpleInteractableBridge` did not exist.
- The next run exposed two integration defects: native XRI bridges were not embedded deeply enough in generated apparatus prefabs, and the responsive-layout test measured an inactive UI hierarchy incorrectly.
- A direct Game View pass then exposed the top experiment row being occluded by the physical console. A dedicated depth regression test failed with canvas Z `1.36` behind the safe console plane.

## GREEN implementation

- `XrSimpleInteractableBridge` maps native `XRSimpleInteractable` hover/select events to the existing physical `LabInteractable` contract exactly once per selection.
- Generated grabbable apparatus uses `XRGrabInteractable` plus `XrGrabEventBridge`; non-grab apparatus controls use `XRSimpleInteractable` plus the new bridge.
- The shared EventSystem uses `XRUIInputModule`; the Hub canvas is world-space and uses `TrackedDeviceGraphicRaycaster`.
- Both `NearFarInteractor` controller rays support UI and cast only against the intended interactable world layer.
- Keyboard WASD bindings feed the Starter Assets locomotion action, preserving camera-relative motion through `DynamicMoveProvider`.
- Short TMP labels auto-size within readable limits. Long results wrap inside a masked `ScrollRect` with an auto-hiding vertical scrollbar.
- The Hub canvas was moved in front of the console to keep all six experiment choices visible.

## Verification

- Edit Mode: 45/45 passed in `TestResults/XrUiEditMode.xml`.
- Play Mode: 4/4 passed in `TestResults/XrUiPlayMode.xml`, including native XRI simple-selection activation exactly once.
- Asset generation validated all 20 required Physics Lab assets without compile errors.
- Direct Play Mode inspection of `PhysicsLab_Hub` confirmed a rendered camera, both simulated controllers and rays, all six unobstructed experiment buttons, and no centre crosshair.
- The final Editor log contains no compile error, `NullReferenceException`, missing-reference exception, unassigned-reference exception or duplicate Audio Listener warning.

No checkpoint commit was created because this is a shared, already-dirty worktree containing user-owned changes.
