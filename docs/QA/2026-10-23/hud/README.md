# Experiment HUD checkpoint — 23 October

The task placard now fits its instruction height; the center stays available for apparatus. Observation/help controls share a right rail. Home and activity use the existing circular utility style for VR. Global hide disables the HUD subtree and its graphics, while Show UI and VR exit remain reachable; placard collapse is separate. Manual guidance remains available with automatic hints off.

Camera framing now uses the center between the guide and rail, mapped through the phone safe area. This changes the viewpoint; comparison images are entry views, not identical-pose before/after photography. No model scaling or sensor/head-pose modification was introduced.

Validation: 87 Editor HUD checks, 0 failures; four prepared station entries (P01/C04/B01/M01), phone/world/phone cycles, hidden graphics, caption state, and actual Game View layouts at 1280x720,2340x1080,2048x1536. 38 existing Editor interaction checks also passed after the change. See attached plain-text receipts. These are not full-flow certification for32 experiments or physical Android/Cardboard/BLE verification.

Working screenshots: Evidence/FinalPass/Hud in the Unity project. Isolated QA profiles were used. No public push or release. APK0.1.8/code22 predates this HUD checkpoint; do not label that artifact as containing these changes. Room lighting, curtains, broader device/UI/learning acceptance remain unfinished.
