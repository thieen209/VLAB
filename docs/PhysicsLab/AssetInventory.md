# Physics Lab Functional Asset Inventory

The inventory is derived from the six requested experiments. It intentionally excludes decorative laboratory props.

| Stable asset name | Category | Experiment use | Source plan |
|---|---|---|---|
| `Laboratory_Retort_Stand` | Common | Pendulum, spring, force meter, photogate mounting | Repair supplied GLB |
| `Adjustable_Clamp` | Common | Movable stand attachment | Generate separate interactive part |
| `Experiment_Table` | Common | Stable experiment work surface | Reuse existing Unity table visual with generated physics-safe wrapper |
| `Laboratory_Meter_Ruler` | Measurement | World-space distance reference | Generate |
| `Digital_Timer_MC964` | Measurement | Photogate timing modes and display | Generate body, buttons, display, ports separately |
| `Physics_Photogate` | Measurement | Beam interruption and velocity timing | Generate body, beam opening, mounting foot, sensor marker |
| `Photogate_Flag` | Measurement | Known-width beam occluder | Generate |
| `Pendulum_Bob` | Oscillation | Dynamic pendulum mass | Generate |
| `Pendulum_String_Anchor` | Oscillation | Runtime string/joint anchor | Generate compact anchor hardware; string remains runtime Unity geometry |
| `Physics_Coil_Spring` | Oscillation | Endpoint-driven Hooke spring | Generate clean coil and separate endpoint markers |
| `Mass_Set` | Oscillation | Configurable hanging/block masses | Generate slotted masses and hanger |
| `Spring_Force_Meter` | Measurement | Tension measurement | Generate body, hook and indicator separately |
| `Physics_Projectile_Launcher` | Projectile | Angle/velocity controlled launch | Generate base, rotating barrel, socket and trigger separately |
| `Projectile_Steel_Ball` | Projectile | Continuous-collision projectile | Generate |
| `Physics_Friction_Block` | Mechanics | Configurable friction and mass | Generate block, pull eye and mass tray separately |
| `Physics_Air_Track` | AirTrack | Low-friction one-axis guide | Generate rail, feet and photogate mount positions |
| `Air_Track_Glider_A` | AirTrack | First configurable collision body | Generate |
| `Air_Track_Glider_B` | AirTrack | Second configurable collision body | Generate color variant from the same working geometry |
| `Collision_Bumper` | AirTrack | Elastic collision attachment | Generate |
| `Inelastic_Collision_Attachment` | AirTrack | Latching collision attachment | Generate |

No additional physical prop is required for the first six experiments. Dynamic string, trajectory traces, cables, vectors and measurement guides are runtime Unity visuals rather than baked model geometry.
