# VLAB

Offline Vietnamese-first smartphone STEM laboratory for Unity6000.6.4f1. Open `Assets/VLABProduct/Scenes/VLAB_Boot.unity`. Current Android prototype: **0.1.2/code3**, ARMv7+ARM64/IL2CPP, API26+, ETC2. [Download APK](https://github.com/thieen209/VLAB/releases/download/v0.1.2-patch1-20261020/VLAB-0.1.2-android.apk) · [Release](https://github.com/thieen209/VLAB/releases/tag/v0.1.2-patch1-20261020).

Language is in Settings; privacy and terms are in About. Startup no longer requires setup screens or records acceptance. Touch/mouse, optional flat-phone gyro, Cardboard and OS-paired HID share local lesson state and notebook. Phone controls support simultaneous lower-left movement/right-side look, inversion, sensitivity, FOV and recentering. Manual equipment controls expose Take/Place/Return. The notebook includes writable notes, saved observations and35 typeset reference-formula pages across32 lessons.

[PATCH1 changes, exact SHA and validation](docs/PATCH1_012.md) · [Known limits](docs/KNOWN_LIMITS.md) · [Windows persistence diagnostic](docs/VERIFICATION_119_WINDOWS_PERSISTENCE.md). Current Core333, queued UGUI taps, phone/HID/settings/manual UI and32 bounded SDK journeys/47 save-reopens passed in isolated Editor QA contexts. Physical Android/headset/gyro/BLE acceptance and complete research protocols are not certified. The historical project-Evidence file-replacement error remains unresolved.

Android updates retain package/signing identity. Windows Editor product label is now VLAB, selecting a new Editor data directory; old Ngua Man VLAB data remains on disk and is not automatically loaded. See PATCH1 notes before testing.

Android Build Support, SDK/NDK and OpenJDK required. `VlabProductBuild.AndroidRelease` receives the private keystore/password only through process environment; keys and APKs stay out of Git. Open the release to download the signed APK. MIT covers project code; supplied assets/packages and Noto fonts retain their own notices.
