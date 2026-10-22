# Settings verification ledger — current follow-up
Only runtime evidence is called PASS. Historical tests and code inspection are not device verification.

| Setting | Editor component | Final APK emulator | Physical phone / Cardboard / BLE |
|---|---|---|---|
| Panel opacity | PASS 55/80/100 immediate draft alpha; reopen after Apply | code21 PASS phone55/80/100, Cancel55→80, Apply+restart80 (78–86,93–94); VR80 observed, VR min/max editing UNTESTED | UNTESTED |
| UI theme | draft preview binding checked | code21 PASS bounded Navy/Paper/Graphite preview + Cancel restoration (81–83); restart/cross-screen UNTESTED | UNTESTED |
| Vietnamese / English | UNTESTED in this pass | UNTESTED | UNTESTED |
| Large text | UNTESTED in this pass | UNTESTED | UNTESTED |
| Step hints | UNTESTED in this pass | UNTESTED | UNTESTED |
| UI animation / reduced motion | UNTESTED in this pass | UNTESTED | UNTESTED |
| Volume | UNTESTED in this pass | UNTESTED | UNTESTED |
| Mute | UNTESTED in this pass | UNTESTED | UNTESTED |
| Graphics preset | UNTESTED in this pass | UNTESTED | UNTESTED, FPS/thermal cannot be inferred |
| Phone / VR mode | PASS nearby workstation on entry and re-entry in four flagship lessons | code21 PASS stereo entry/view and exit across P01/C04/B01/M01; full recipes UNTESTED | UNTESTED |
| Recenter | PASS parent position unchanged; UI alignment deferred until native pose update | code21 native UI alignment observed (91); native translation not instrumented | UNTESTED |
| Five controller action mappings | UNTESTED in this pass | UNTESTED | BLOCKED: no controller connected |
| QR viewer scan | UNTESTED | UNTESTED | BLOCKED: no physical viewer/phone camera |
| Bluetooth connection button | UNTESTED | UNTESTED | BLOCKED: no controller protocol/device |
| Gaze auto-select | PASS initial-page target requires look-away or explicit trigger; explicit trigger still works | code18 chain reproduced; code19 browser target remained pending until trigger (45–46); code21 full dwell matrix UNTESTED | UNTESTED |
| Gaze dwell duration | UNTESTED in this pass | UNTESTED | UNTESTED |
| Look sensitivity | UNTESTED in this pass | UNTESTED | UNTESTED |
| Invert X | UNTESTED in this pass | UNTESTED | UNTESTED |
| Invert Y | UNTESTED in this pass | UNTESTED | UNTESTED |
| Phone gyro | UNTESTED in this pass; separate from SDK Cardboard head tracking | UNTESTED | UNTESTED |
| Phone field of view | unchanged; excluded from VR workstation fix | UNTESTED | UNTESTED |
| Focus assistance | unchanged phone behavior; VR uses rig translation | UNTESTED | UNTESTED |
| Show virtual movement stick | UNTESTED in this pass | UNTESTED | UNTESTED |
| Movement speed | UNTESTED in this pass | UNTESTED | UNTESTED |
| Joystick size | UNTESTED in this pass | UNTESTED | UNTESTED |

Do not summarize this table as all Settings passed. No new setting was added. No physical device was available.

Current artifact is code21. Full native press/hold/move/release and physical BLE are UNTESTED. A visible toggle or persisted value alone does not prove its feature. See REPORT.md for scope and exact screenshot interpretation.
