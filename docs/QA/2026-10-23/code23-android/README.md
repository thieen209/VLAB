# Code23 Android candidate — scoped evidence

APK: VLAB-0.1.9-finalpass-candidate-code23.apk; 126561492 bytes.
SHA256: 05668858DC94513AB34349EC5A855A8E98D92733DCB65A6A455670728B853DB6.
Unity6000.6.4f1 build completed2026-10-23 19:44VN;0errors,43warnings. Signing verification passedv2. Manifest:VLAB/com.nguaman.vlab,0.1.9/code23,min26,target36,armeabi-v7a+arm64-v8a.
Local source checkpointbf13cc9 includes the latest owned fixes; build uses the working Unity project, not a clean clone build.

API35 x86_64 emulator with ARM64 translation, existing VLAB_014_API35 AVD. Installed with adb install-r; no wipe. Cold launch succeeded. Home and browser rendered at2340x1080. Existing four P01 saved attempts remained visible. Through actual Android taps: home→Physics→P01→readiness→station; stand→bob→equilibrium; swipe bob started oscillation and cycle counting. Record after10cycles displayed saved result T1.555s,total15.551s,g9.794m/s². UI was not driven by test hooks or direct session state changes.

Evidence under E:/VLAB/4 LABS/work/final-pass-20261023:code23-home.png,code23-lab.png,code23-station.png,code23-touch.png,code23-bob.png,code23-ready.png,code23-running.png,code23-results.png,code23-pendulum-emulator.mp4,code23-emulator-logcat.txt.
Clip records Android input and oscillation; not a complete all-experiment acceptance recording.

Open findings: home shifted right with asymmetric safe area, VR utility wrongly at top (user confirmed); contextual model label partly occluded by stand; close-up can put timer beneath right HUD. Menu fixes after code23 are not in this APK. Dark gear/stand appearance still needs improvement.
Runtime log includes optional AssetPackManager ClassNotFoundException and stripped URP postprocess shader messages; UI continued to render. Do not call the log clean or claim physical-phone performance/tracking. Hardware BLE remains deferred. This is a candidate, not full32-experiment acceptance or public release.
