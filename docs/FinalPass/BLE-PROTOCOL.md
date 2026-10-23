# BLE controller configuration

Hardware interoperability is unverified. The supplied documents describe ESP32-S3/BNO055 and a right-hand BLE gateway, but do not specify GATT UUIDs or the wire payload. Scanning and inspecting services does not prove compatibility.

In Settings → Controls → BLE controllers, scan, select the device, refresh the channel list and select a notify/indicate characteristic. No commands are written to the controller: the app subscribes to notifications. Pairing a HID controller remains available through System Bluetooth / HID.

Profiles / calibration supports up to eight profiles. Copy JSON exports the selected profile; Import copied JSON validates the clipboard contents and asks before replacing that profile. Import the full exported shape with values supplied by the firmware developer. Changing a profile disconnects the transport; reconnect to apply it. Learner notebooks are stored separately.

## Adapter fields

- `service`, `characteristic`: full 128-bit UUIDs discovered from the actual device; `address`: its Bluetooth address. Neither is evidence of matching firmware.
- `packetLength`: exact bytes per notification, 4–128; zero disables input decoding. Fragmented or concatenated frames require a matching firmware/adapter implementation.
- `littleEndian`, `prefixHex`, `crc16`: byte order, fixed prefix, optional CRC16/CCITT-FALSE over all but the final two bytes. CRC bytes use the selected byte order.
- `sequenceOffset`, `sequenceBytes`: required unsigned sequence, 2 or 4 bytes. Duplicate/out-of-order packets are rejected with modular wrap handling.
- Optional offsets use -1 for absent fields. `buttonsOffset`/`buttonsBytes` read an unsigned 2/4-byte mask. `buttonBits` maps 12 canonical slots: A, B, X, Y, LB, RB, LT, RT, Menu, Select, left-stick click, right-stick click. -1 disables a slot. User action remapping is shared with HID.
- `stickXOffset`, `stickYOffset`: signed 16-bit axes; both must exist together. `x`/`y` store min/centre/max calibration; `deadzone` ranges .05–.5.
- `triggerOffset`, `gripOffset`: unsigned 16-bit inputs, bounded by the corresponding configured maximum. Press/release thresholds are .55/.45. Trigger maps to RT and grip to RB.
- Quaternion offsets X/Y/Z/W must all exist or all be absent. Encoding 0 is float32; 1 is signed int16 multiplied by `quaternionScale`. Invalid/nonfinite norms are rejected. Signed axis permutation describes the verified coordinate basis; `useOrientation` is off by default. It changes the controller ray, never head tracking. No 6DoF position is inferred.
- `timeout`: .15–2 seconds. A timeout cancels holds. Native arrival timestamps include queue delay; this is not motion-to-photon latency.

## Optional proposal, not existing firmware

`BleControllerProfile.Proposal()` describes a 36-byte little-endian frame: bytes0–2 `56 4C 01`; byte3 hand ID1; uint32 sequence at4; uint16 button mask at8; int16 stick axes at10/12; uint16 trigger/grip at14/16, max32767; float32 quaternion XYZW at18/22/26/30; CRC16 at34. UUIDs intentionally remain empty. Do not load this proposal unless the firmware explicitly implements it.

## Verification still needed

Android device permission denial/regrant, scan results, GATT discovery and CCCD subscription, reconnect and pause/resume must be tested with the actual controller. Editor fake-transport and packet checks cannot establish those results. Confirm sensor axes against physical rotations before enabling controller orientation.
