# VLAB 0.1.9 / code25 candidate

APK: E:/VLAB/4 LABS/Ngua Man VLAB/Builds/Android/Candidates/VLAB-0.1.9-finalpass-candidate-code25.apk
Size: 126562044 bytes
SHA256: 7F9B676C81E8D81478258195A43B890B6472E9F26FF0F47761E7DA41BBCF64BF

Unity 6000.6.4f1 IL2CPP release build completed 2026-10-23 21:06 Vietnam time: 0 errors, 43 warnings. APK identity com.nguaman.vlab, VLAB, version0.1.9/code25, minAPI26, ARMv7+ARM64. v2 signature verification passed, one signer. Built from working Unity project; owned-change checkpoint c8d6953 (not a clean-clone reproducibility claim).

Adds notebook Back/Menu save guards to code24: leaving now saves; write failure keeps the draft and shows recovery actions. Analysis Back returns to notebook. Targeted Editor checks passed12/12 (four exit/persistence assertions plus eight existing contrast assertions). Test used real TMP note input, real disk reload and isolated deliberate write failure. Analysis Back was not independently exercised.

Code25 installed over code24 on VLAB_014_API35 emulator (emulator-5558); package version25 confirmed and cold launch succeeded. Native Android keyboard entered QA25-Saved-note, keyboard OK accepted it, on-screen Quay lai exited, then force-stop/cold launch retained the new title and existing QA014-P01 note. See code25-saved.png. This validates on-screen Back persistence only; the initial system-Back/IME attempt did not retain the new page, so native system-Back keyboard interaction remains unresolved and must not be claimed PASS. Controller Back/Menu save-failure behavior is covered by the separate Editor checks only. Physical Cardboard/BLE/performance and complete32-experiment acceptance remain unverified/incomplete. Existing optional AssetPackManager warning and emulator System UI interruption are documented in code24 evidence; do not call all logs clean. No public push or release.

Install over the previous signed VLAB without uninstalling. Check menu alignment/VR row, retained notebook data, a pendulum run, and edited notes after Back/Menu plus restart. Keep previous APK as fallback; preserve local data before any uninstall/downgrade.
