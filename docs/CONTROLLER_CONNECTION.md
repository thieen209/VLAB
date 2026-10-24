# Controller connection — code32

Code32 includes the direct dual-controller V6 integration already present in the working project. The 56-byte contract, roles, calibration and limits are in [V6 protocol](Workbench/CONTROLLER-V6-PROTOCOL.md). The earlier controller fixes and deferred F06/F07 work are preserved in [prior handoff](Workbench/CONTROLLER-V6-PRIOR-FIXES.md). This UI upgrade does not alter firmware or certify hardware.

The legacy HID path below is retained. Statements about the missing custom protocol describe the older pre-V6 snapshot, not code32.

## Historical connection notes

# Optional controller connection

Display mode and input source are separate. Phone uses tap selection and empty-background swipe-look; Cardboard uses head direction and trigger or optional dwell. In Controls, Phone/Viewer changes immediately. Pair controller opens Android Bluetooth Settings; press a paired HID controller button after returning. The detected HID name appears there. The app does not claim a custom BLE connection from this shortcut.

Default HID mapping: A/RT selects, shoulders grip, Start opens the menu, Select recenters, B goes back. Controls includes illustrated left/right and front/side views. Select a visible button or its labelled callout, then choose its action. Changes stay in the Settings draft until Apply; Cancel preserves the saved profile. A conflicting single assignment swaps with the previous binding; ambiguous composite conflicts are rejected without changing the draft. Flat left stick moves the cursor and right stick looks in the laboratory. In VR, head tracking determines the pointer and sticks do not rotate the camera. D-pad adjusts a carried object's distance, not experiment parameters. Illustrations locate physical controls; they do not establish the hardware's HID identifiers.

Custom ESP32 BLE remains unconfigured: the supplied hardware folders contain no firmware, GATT service/characteristic UUIDs, payload format, or axis convention. The newer controller README identifies ESP32-S3 SuperMini with BNO055 (address 0x28, SDA12/SCL13), while older project papers describe MPU6050/MPU9250. The usage document describes one right-hand BLE gateway named VLAB CONTROLLER, with left-to-right radio transport; a saved Bluetooth entry does not prove a live data connection. The hardware README states that the PCB is not yet working and breadboard tests worked. The existing decoded ControllerSignal boundary validates quaternion, sequence and freshness, but its mock tests do not establish a physical transport. The linked HadesVR/ATmega/NRF24 project is a shell/PCB reference, not compatible firmware proof.

Documented GPIOs are joystick X/Y 1/2 and press10, trigger4, MENU6, SYS7, MODE8, GRIP9 (active low), LED11 (active low), battery ADC5. The illustrated calibration guide reproduces the project's documented button combinations and timings, labels them firmware-dependent and sends no unverified calibration command. Controller yaw calibration and headset recenter are separate actions. No position tracking, haptics or analogue grip force is claimed.

Firmware handoff needed: source revision, service/notify/write UUIDs, sample packets, quaternion convention and units, buttons, timestamps/sequences, reconnect behaviour and physical calibration. Touch/Cardboard remain usable without a controller.

Synthetic Input System checks cover edges, release/cancel, UI exclusion, disconnect and fallback. Physical Android Bluetooth, touch, headset optics, tracking and performance remain unverified. A signed APK proves packaging, not these device behaviours.

Platform sources: [Bluetooth Settings action](https://developer.android.com/reference/android/provider/Settings#ACTION_BLUETOOTH_SETTINGS), [GATT lifecycle](https://developer.android.com/develop/connectivity/bluetooth/ble/connect-gatt-server), [GATT permissions and notifications](https://developer.android.com/reference/android/bluetooth/BluetoothGatt).
