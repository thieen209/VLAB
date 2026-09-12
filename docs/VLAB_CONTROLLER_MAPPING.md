# VLAB controller and simulator mapping

Source snapshot: 12 September 2026. These mappings describe code paths. Hardware transport and physical controller acceptance are not implied.

## User-facing controls

| Action | Desktop development | Android viewer / standard gamepad |
|---|---|---|
| Point | Mouse position; centered ray while the relevant rig locks the cursor | Head gaze, or decoded orientation ray when that provider is selected |
| Select / activate | Left click or E for world objects; click for UI | Viewer trigger or gamepad south button |
| Move where enabled | WASD; Shift requests sprint | Left stick |
| Look / turn | Right mouse in the supported lab/menu look adapter | Head rotation; analog right stick for supported locomotion adapters |
| Drop / return | Q in the retained apparatus adapters | Gamepad east button |
| Pause / back | Esc | Gamepad select/back; Cardboard close control routes to application back |
| Reset | R through shared reset input; board **Đặt lại** | Gamepad start; board **Đặt lại** |
| Recenter | Home in menu head simulation; Settings → Kính VR → Đặt lại | Hold viewer trigger or use the recenter setting |
| Enable/disable editor menu head simulation | F8 | Not a device control |
| Enable/disable decoded controller replay | F9 in the editor | Not a deployed BLE connection button |

The common simulator additionally emits digit 1 as primary, digit 2 as secondary, left/right mouse as left/right grab, and Space as jump-request state. A state field being available does not imply every lesson implements that action.

## Existing apparatus-specific actions

- Physics retains its grab, release and apparatus controls. Mouse wheel adjusts held-object distance; secondary input rotates the held object where supported.
- Chemistry retains XRI vessel ownership. Desktop instructions expose F for lid/pipette use, R for tilting while holding, wheel for distance, and click for grab/release or burette operation. Its decoded controller adapter uses primary for select, secondary for vessel use, right grab for tilt and scroll for distance. Therefore R has a chemistry manipulation meaning in the original lesson; the supplementary workbench suspends that adapter and routes shared reset to the active supplementary lesson.
- Biology/Engineering baseline demos use select-then-place interaction and Q to return a held item. Supplementary activities use select-then-target semantics: atom then socket, reagent then sample, gear then shaft, or weight/fulcrum then division. Selecting a fitted water atom removes it. Cell structures are directly selected.

Consult the visible lesson instruction for its current action. These retained adapters are not a promise that every keyboard shortcut does the same physical manipulation in every subject.

## Decoded controller state

`IVLABInputProvider` supplies a provider name, `ReadState()`, `InteractionPressed` and `ResetPressed`. `VLABInputState` includes movement/look axes, scroll, primary/secondary, left/right grab, sprint, jump, drop, pause and two analog trigger values. `IVLabRayProvider.TryGetRay(Camera, out Ray)` is the optional orientation/ray extension.

The project deliberately does not assign physical ESP32 A/B/C buttons, BLE UUIDs, packet offsets, baud rates or ESP-NOW payloads without verified firmware. Map the real controller's controls to the semantic fields after obtaining that contract. Native XRI input action assets remain the authority for native tracked devices.

## F9 replay reproduction

1. Enter Play Mode from the Menu scene and reach an interactive screen or lab.
2. Press **F9** to substitute `VLabControllerReplayProvider`. This preserves the previous provider for restoration.
3. **J/L** turn the simulated controller left/right; **I/K** tilt it up/down. The replay changes controller orientation independently of mouse look. Presses, movement and scroll are copied from the previous provider; the right-stick look field comes from a connected standard gamepad in this replay mode.
4. Use primary/select to activate the pointed UI or apparatus. The provider sends one increasing sequence number each frame. R is passed as reset.
5. In Settings → Tay cầm → Hiệu chỉnh hướng, **Đặt lại** calibrates the active replay orientation. This is distinct from head recenter.
6. Press **F9** again to restore the previous provider and disconnect replay.

The editor replay continuously submits frames, so stopping transmission is best exercised through `Disconnect()` or a test calling `Submit` once and waiting beyond the timeout. F9 does not communicate with a physical controller.

## Submission, freshness and calibration

API: `VLabControllerReplayProvider.Submit(VLABInputState input, Quaternion orientation, uint packetSequence, bool reset = false)`.

- Default movement deadzone: **0.12**. Magnitudes outside it are rescaled and clamped.
- Default freshness timeout: **0.5 seconds**, measured using unscaled time. Stale state reads as neutral; the ray becomes unavailable.
- While connected, an equal or older sequence number is rejected using signed sequence-difference comparison. After timeout/disconnect, a new stream can start.
- Non-finite or near-zero quaternion magnitude is rejected; accepted quaternions are normalized.
- `Calibrate()` stores inverse current orientation and the last view heading, making the current aim the forward reference.
- Ray origin uses a view-relative hand anchor influenced by handedness. No hand position is inferred by integrating IMU acceleration.
- Application pause and component disable disconnect the replay provider.

`InputManager` sanitizes state, publishes `RawState`, then applies experiment blocking to `CurrentState`. Menu UI uses raw state during pause. When input/ray validity changes, `VLabGazeInputModule` cancels pending UI press/drag and waits for release; reconnecting a held button should not complete the old click. Disconnected controller visuals are hidden.

## Hardware acceptance still needed

Using an actual ESP32/controller pair, verify the real transport, packet ordering, disconnect timeout, both controller roles, physical button mapping, orientation coordinate system, recenter, left-handed anchor, Android background/foreground behavior and duplicate selection against native XRI. Use the same submission boundary rather than adding per-lab BLE parsing. Device performance and physical stereo/head-tracking evidence must be recorded separately.
