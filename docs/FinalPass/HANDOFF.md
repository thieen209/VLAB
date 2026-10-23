# VLAB 0.1.9 / code24 candidate

Built 2026-10-23 20:27–20:39 Vietnam time using Unity 6000.6.4f1, IL2CPP release. Result: 0 errors, 43 warnings. Package com.nguaman.vlab; label VLAB; Android API26+, ARMv7 + ARM64. APK signature v2 verified, one signer.

APK: E:/VLAB/4 LABS/Ngua Man VLAB/Builds/Android/Candidates/VLAB-0.1.9-finalpass-candidate-code24.apk
Size: 126563384 bytes
SHA-256: EC2FEB23C65F4C41650D052C860AD21F21CA202B09BD2ECBEF53A05EEE33B5E6

Built from the working Unity project. Owned-change checkpoint: 960fcb0, following ac01302 (home alignment) and 3b491ba (pendulum visibility). This is not a clean-clone reproducibility claim; unrelated older working-project differences remain outside that checkpoint.

Includes centered safe-area home and five-button row, canceled-touch recovery, unoccluded model cues, compact pendulum/timer framing and coated gear/support readability. Focused Editor evidence: home19, pendulum22, gear17 assertions passed. Existing code23 emulator interaction evidence does NOT validate this APK.

Code24 packaging, identity and signature verified. Installed over code23 with adb install -r on existing API35 emulator (ARM64 translation): Success, versionCode24 confirmed, cold launch Status ok. Actual Android screenshot at2340x1080 confirms centered home and VR as fifth bottom-row utility. Native tap opens Physics browser; five previous P01 attempts and one P02 attempt remain visible. This confirms visible records, not a full save-content integrity audit.

Emulator showed a System UI not responding dialog after boot/launch; Wait dismissed it and app navigation then responded. This was a System UI dialog, not proof of a VLAB ANR; cause not isolated. Log retains optional AssetPackManager ClassNotFoundException already seen oncode23. Full log saved outside Git at E:/VLAB/4 LABS/work/final-pass-20261023/code24-launch-logcat.txt. No physical VR/BLE/performance or full32 acceptance claim; no public upload or push. Owned emulator shut down after this short smoke check.

Quick user check: install over the previous signed VLAB without uninstalling; confirm notebook remains. Inspect centered menu/VR bottom row; complete a pendulum measurement; enter/exit VR on your phone. Keep code23 available as fallback; do not uninstall to downgrade without backing up local data.
