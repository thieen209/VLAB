# Unified VLAB

The authoritative Unity project is `VLAB`, using Unity **6000.5.6f1** and the existing Built-in renderer. Start with `Assets/VLAB/MainMenu/Scenes/Menu.unity`. The sibling UI and Chemistry projects are source archives; the application does not load assets from them.

## Integration decisions

- Retained root Physics, its additive Base/Hub/six-experiment scene architecture, and the existing Biology microscope and Engineering LED circuit lessons.
- Imported the existing `VLAB(UI)` spatial menu, Vietnamese/English onboarding and original legal pages. All four lab cards now launch their own scenes, with shared pause/settings/return navigation.
- Imported Chemistry's scene, scripts, experiment definitions, materials, prefabs and transitive GUID dependencies. Its Resources-loaded desktop action asset is also included. Existing root asset identities win when a shared GUID exists. `audit.json` records the source/dependency map.
- Unified Input System on **1.20.0**, the version already resolved in Chemistry. Automated input tests explicitly route synthetic devices to the unfocused batch Game view; the original Chemistry project reproduced the same focus-related failure. Product input is not changed to bypass Input Actions.
- Android uses the new Input System backend only. Removed the unused `com.unity.ai.inference` runtime after confirming no inference APIs or model assets exist in VLAB; this avoids compiling and shipping its machine-learning shader resources. The Editor assistant remains installed.
- Preserved Chemistry's `ChemistryControllerBridge.ReceiveCommand` adapter and root `IVLABInputProvider`/`InputManager` abstraction. No BLE service UUID, packet layout or firmware behavior was invented.
- Recovery copies of pre-existing changes are outside the project in `.vlab-recovery-current`; the original `.vlab-recovery-20260910` remains intact. No source project was reset or overwritten.

## Viewing and interaction

Android uses pinned **Google Cardboard XR Plugin 1.35.0** for the display, stereo, viewer lens parameters and sensor fusion. `VLabHeadPose` consumes the XR head orientation once; competing camera pose drivers are disabled on that camera. No extra raw gyroscope rotation is layered on the XR pose. The application uses rotational phone tracking, not positional head tracking.

In the Editor menu, hold the right mouse button to look, press **Home** to recenter, or **F8** to toggle the head simulator. Translation is gated independently from head pose. The package's controller simulator is suspended only while in the menu so it cannot disable mouse clicks or compete with the head simulator; it is restored when leaving. Existing desktop and XR simulator lab controls remain available. The phone fallback uses gaze, touch/viewer button and standard gamepad input; it is not a replacement for the missing ESP32 transport.

The shared settings panel provides learning guidance, audio mute/volume, viewer recenter/help, graphics presets and language. Graphics reuse Chemistry's working quality controls across the application. Gaze selection requires an explicit press and release, including consent and exit actions. Chemistry crouching synchronizes the resized upright capsule before its movement sweep to prevent the camera being pushed above the intended seated height.

On the phone, long-hold the Cardboard trigger to recenter; use its gear control to scan viewer parameters. The microscope's desktop camera flight is suppressed on phone so inspection cannot rotate the user's head view. Desktop overlay canvases become spatial canvases for the viewer.

Chemistry's phone HUD opens a spatial control board for titration, Daniell cells and electrolysis. Its preparation, dosing, recording and reset controls call the existing lesson controllers. This makes the lessons accessible in stereo while the simulation continues; the desktop Chemistry interface remains available in the Editor.

## Validation and build

Use **Tools → VLAB → Unified** to configure Android, validate production scenes, and build the APK. Automated checks and rendered captures are under `TestResults/Unified`; full logs are under `Logs`.

On this Windows account, use `Tools/BuildAndroid.ps1` for batch builds. Java's local sockets fail when its temporary directory uses the accented user path, and Unity caches the child-process environment before a build method runs. The launcher supplies an ASCII project-local Java temporary directory before starting Unity; it does not change machine or user settings. Close the Editor for the project being built, then run:

```powershell
.\Tools\BuildAndroid.ps1 -UnityEditor 'E:\Unity\6000.5.6f1\Editor\Unity.exe'
```

`-ProjectPath` optionally selects a source copy. Android tooling requires an ASCII-only project path. The final integration build used `C:\VLAB-build-20260910` after the E: drive ran out of build space; `VLAB` on E: remains authoritative.

Android configuration: landscape left, ARM64, IL2CPP, OpenGLES3, Cardboard XR loader, minimum API 26. The custom Gradle templates include Cardboard's Android dependencies. Existing signing credentials and application identity are preserved.

The build/test results for this integration are recorded in `validation.md` when the final checks finish. A successful Editor simulation is not evidence of physical phone tracking or BLE connectivity.

## Hardware limitations and final device checks

The inspected workspace contains **no production BLE transport, native Bluetooth plugin, service/characteristic UUIDs, packet parser, or ESP32 firmware**. Chemistry contains a command receiver intended for a future native bridge; root exposes an input-provider interface. These are preserved, but an actual connected-controller product cannot be validated or claimed from these sources.

No Android device was available through `adb` during integration. On representative hardware:

1. Install the APK; scan the actual viewer QR code and confirm correctly aligned/distorted stereo in both eyes.
2. Check left/right/up/down rotation, recenter and pause/resume without jumps or duplicate pose application.
3. Navigate each lab and return repeatedly; check selecting, holding, releasing, pouring and microscope inspection.
4. Integrate the actual BLE transport through the preserved adapters, then verify both controllers, disconnect/reconnect and background/foreground behavior.
5. Measure frame time, battery/thermal behavior and controller latency. No device FPS, latency or comfort claim is made without those measurements.

Reference: [Google's Cardboard Unity setup](https://developers.google.com/cardboard/develop/unity/quickstart).
