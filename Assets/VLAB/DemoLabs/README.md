# VLAB Engineering + Biology demos

Two independent scenes inside the existing Unity 6000.5.6f1 project. The current built-in render pipeline is preserved (URP is installed but is not assigned in Graphics/Quality settings).

- `Assets/VLAB/DemoLabs/Scenes/EngineeringLab.unity`
- `Assets/VLAB/DemoLabs/Scenes/BiologyLab.unity`

Open either scene and press Play. The existing Home screen routes the Biology and Mechanical/Engineering selections to these scenes. Physics continues through its existing base/hub flow. No Chemistry scene existed in the inspected build settings; its pre-existing routing is retained.

Editor shortcuts: **F6** opens/plays Engineering; **F7** opens/plays Biology. Both are also under **Tools → VLAB → Demo Labs**. Rebuilding is optional; the scenes are already serialized, with references and imported equipment.

## Player controls

- Click a tool/component to pick it up; click its labeled destination to place it. Drag and release over a destination also works.
- Click physical switches, stage clips, eyepiece and the microscope nosepiece to activate them.
- Hover a focus knob and scroll to rotate it. Scroll while holding the LED to reverse its polarity.
- Hold right mouse to look; WASD moves along the accessible side of the workstation.
- Q returns a held object to its tray. R or **Đặt lại thí nghiệm** restarts the experiment.
- In the eyepiece, drag or scroll the labeled rotary dials. **Rời thị kính** / Escape returns to the room.

## Engineering route

Start, pick the 220 Ω resistor and place it in the resistor dock. Place the LED. Attach both ends of each of the three flexible wires:

1. Source +5 V → R.A.
2. R.B → LED A.
3. LED K → source −/GND.

Choose **Kiểm tra mạch**, then activate the physical power switch. Read the current and observe the LED, then choose **Kết quả** on the desk. Try 100 Ω and 1 kΩ, or reverse the LED, to compare outcomes. Modifying the circuit cuts power and invalidates its previous check.

The electrical model is a fixed 5 V source with an approximate 2 V LED drop and a series resistor. It rejects shorts, bypasses, extra/duplicate links, missing components and reversed polarity. It is deliberately not a general circuit solver. The 100 Ω overcurrent state is a safe software illustration, not a physical lab recommendation.

## Biology route

Place the slide on the preparation pad. Bring the water dropper to **NHỎ NƯỚC**, place the onion sample at **MẪU**, then the coverslip at **LAMEN**. Pick up the completed slide, mount it at **BÀN KÍNH**, and close both clips. Select the 10× objective and activate the eyepiece.

Rotate coarse focus toward clarity and then fine focus. Switch to 40× after observing a sharp 10× image; make a small fine-focus correction. Click the nucleus, cell wall and cytoplasmic region directly in the specimen, in the requested order. **Kiểm tra** displays the result.

The dedicated specimen camera renders an original baked onion-epidermis illustration into a 768×768 render texture. Magnification changes the camera's field of view by 4×; defocus affects the rendered specimen. The illustration distinguishes a large pale vacuole from peripheral cytoplasm and explicitly labels the enhanced nuclear contrast. It is not a photomicrograph. Coverslip angle/bubble simulation is intentionally omitted as an optional detail.

## Ownership and reuse

- `Runtime/Shared`: semantic ray selection, reusable grab/snap/rotary controls, contextual feedback and reset/recovery.
- `Runtime/Engineering`: circuit connectivity model, physical endpoint wires and experiment progression.
- `Runtime/Biology`: preparation/progression model, optical rendering and specimen identification.
- `Editor`: reproducible scene construction, art/material assignment, Vietnamese UI and additive build-list registration.
- `Tests`: model checks, serialized-reference checks and Play Mode interaction routes.
- `Art/Models`: original Blender-generated metric FBX equipment, with separate mechanical parts and pivots.
- Source Blender library: `SourceAssets/DemoLabs/VLAB_DemoAssets.blend`.
- Reproduction script: `Tools/Blender/generate_demo_lab_assets.py` (uses a separate Blender scene and restores the previous scene).

The existing `VLAB.Core.Input.InputManager` and `IVLABInputProvider` contract are reused. Device reads stay in `VLabDemoInputProvider`; the experiments receive semantic actions. Future controller adapters can submit world rays through `VLabInteractionDriver.SelectRay` and rotate the shared control components. Smartphone stereo rendering and real BLE/hardware behavior require device validation; this delivery targets Editor/Device Simulator interaction.

The existing Physics experiment table and installed Liberation Sans font are reused. All new equipment geometry and the cell illustration are original, with no downloaded third-party models or additional packages. Existing Physics scene assets are untouched.

Validation evidence is recorded in `TestResults/DemoLabs-*.xml`, `TestResults/DemoLabs/`, and `Logs/DemoLabs-*.log`. See the final QA record there for the actually completed checks.
