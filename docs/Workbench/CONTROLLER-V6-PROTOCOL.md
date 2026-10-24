# VLAB direct GATT V6 protocol 1

This assignment is implemented by this firmware and the matching Unity decoder. It is incompatible with the earlier HID/20-byte pose extension and the legacy 36-byte BLE proposal.

Service `f1ab6001-6b4c-4a1e-9c32-564c41420001`; notification `f1ab6002-6b4c-4a1e-9c32-564c41420001`; write-with-response commands `f1ab6003-6b4c-4a1e-9c32-564c41420001`. Roles advertise as VLAB-Left / VLAB-Right; explicit selection binds their addresses, then packet hand IDs are checked. Names alone are not a cryptographic identity. GATT links are unbonded and limited to one phone connection per controller. Connect in a controlled environment; the firmware does not authenticate a nearby competing central.

## Notifications

Exactly 56 bytes, little-endian, nominal 50 Hz. ATT MTU must be at least 59; Android requests 100, and may negotiate a different larger value. No fragmentation or concatenation is accepted. CRC16/CCITT-FALSE, polynomial 0x1021, initial 0xFFFF, no reflection/XOR; CRC covers bytes 0–53.

| Bytes | Meaning |
|---|---|
| 0–2 | `56 36 01` (`V6`, protocol 1) |
| 3 | Hand 1 left, 2 right |
| 4–5 | uint16 notification sequence, wraps modulo 65536 |
| 6 | Flags: bit0 fresh valid BNO fusion, bit1 joystick calibrated, bit2 Hall calibrated, bit3 diagnostic build, bit4 fully calibrated IMU; others zero |
| 7 | Native BNO CALIB_STAT: system/gyro/accel/mag, 2 bits each |
| 8–11 | uint32 BNO acquisition milliseconds (controller clock, wrap allowed) |
| 12–13 | Button mask: Menu 0x0100, Sys 0x0200, Mode 0x0002, Grip 0x0020, stick switch 0x0400 |
| 14–17 | int16 joystick X,Y, -32767…32767, radial magnitude ≤1; firmware owns calibration and deadzone |
| 18–19 | uint16 calibrated Hall trigger, 0…65535 |
| 20–27 | int16 quaternion X,Y,Z,W /16384, native BNO sensor frame |
| 28–33 | int16 linear acceleration X,Y,Z /100, m/s², native sensor frame |
| 34–39 | int16 gyro X,Y,Z /16, °/s, native sensor frame |
| 40–45 | int16 gravity X,Y,Z /100, m/s², native sensor frame |
| 46–51 | uint16 raw ADC X,Y,Hall, 0…4095 |
| 52–53 | uint16 nominal battery divider estimate, mV; unverified calibration, no state-of-charge claim |
| 54–55 | uint16 CRC |

Joystick deadzone is .15 in firmware; Unity does not apply a second one. Hall edges use .55 press/.45 release; raw analog intensity is preserved. Controls remain disarmed until a neutral, calibrated packet is observed. A calibrating firmware reports control-calibration flags clear and neutral controls.

Unity rejects incorrect role/version/length/CRC/flags, unknown button bits, ADC overflow, invalid radial axes and implausible valid-IMU quaternion/gravity/acceleration. Duplicate/backward sequence deltas are rejected; accepted forward delta is 1…32767. Sequence baseline resets on connection generation. Estimated gaps are sequence gaps, not an RF measurement. Queue age >300 ms is rejected; 300 ms without fresh accepted packets cancels that hand. Host arrival uses a monotonic phone clock and native queue age. Firmware timestamps are not subtracted from phone timestamps. A nonadvancing sensor timestamp invalidates orientation after 150 ms while fresh controls may continue.

Raw BNO quaternion is right-handed. The default Unity basis reflects Z: quaternion XYZW becomes `(-x,-y,z,w)`, linear vectors `(x,y,-z)`. Per-hand local mounting rotation is then applied. Recenter computes head yaw times inverse of the measured hand orientation. This gives a reference pose, not proof of correct sensor mounting. Check each physical yaw/pitch/roll sign and mounting angle. Orientation requires system≥1, gyro≥2, accel≥2, mag≥1; these are this implementation's readiness thresholds. No controller orientation is applied to the head camera.

## Commands

Eight bytes: `56 43 01`, opcode byte, uint16 argument, CRC16 over first 6 bytes. Opcode 1/arg0 requests recenter diagnostics (actual view-relative recenter is owned by Unity, leaving raw IMU unchanged); 2/arg0 starts 10-second ADC calibration; 3/arg0,1,2 sets LED off,on,auto; 4/arg20…100 changes notification Hz. Invalid or malformed requests are discarded. Commands are serialized with GATT write callbacks. A successful write confirms transport delivery, not completion of calibration; inspect calibration flags and USB status. The public Unity command API supports opcodes1–4. No erase, battery charging or arbitrary GPIO commands exist.

References checked during implementation: [Bosch BNO055 datasheet](https://www.bosch-sensortec.com/media/boschsensortec/downloads/datasheets/bst-bno055-ds000.pdf), [Android BluetoothGatt API/MTU behavior](https://developer.android.com/reference/android/bluetooth/BluetoothGatt), [NimBLE-Arduino source](https://github.com/h2zero/NimBLE-Arduino/tree/2.3.6).
