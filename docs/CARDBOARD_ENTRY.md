# Cardboard entry and optional viewer calibration

Current status: entry lifecycle changes are integrated. Synthetic lifecycle checks passed in Controls40 and full Polish47; physical Android/viewer acceptance remains pending.

## Integrated product behavior

`VlabInput.SetVr(true)` initializes the Cardboard loader and starts stereo directly; it has no saved-profile or QR-scan prerequisite. Failed startup returns to flat mode. Exit stops/deinitializes XR; returning to VR uses the same direct entry route.

Viewer QR scanning is an explicit Settings/gear action. From flat Settings it initializes the loader without starting stereo, because the installed scan API requires initialization. The product does not save a replacement/default URI automatically. While in VR it checks for changed viewer parameters, reloads them when needed, and updates screen parameters each frame. This follows the [official Unity API](https://developers.google.com/cardboard/reference/unity/class/Google/XR/Cardboard/Api).

## Optical parameter provenance

The installed package is Google Cardboard XR Plugin **1.36.0**, cache `com.google.xr.cardboard@7fb2c0e438d2`; inspected local sources are `package.json`, `Runtime/Api.cs`, and the Hello Cardboard sample startup. The sample's automatic first-launch scan is sample policy, not a mandatory product entry gate.

Google's [native Unity display source](https://github.com/googlevr/cardboard/blob/master/sdk/unity/xr_unity_plugin/cardboard_display_api.cc), function `CardboardDisplayApi::UpdateDeviceParams`, reads saved viewer parameters and selects built-in Cardboard V1 parameters when none exist. Both paths create lens distortion and its rendering resources. The product preserves this SDK optical correction; it does not fabricate optical values or bypass distortion. This source inspection used current upstream `master`, not an independently pinned native source revision matching the installed binary.

The [official QR API](https://developers.google.com/cardboard/reference/c/group/qrcode-scanner) documents built-in V1 parameters separately from saved parameters. The installed Android AAR exports the saved-parameter, V1-parameter and lens-distortion creation APIs. Symbol presence corroborates availability, not executed device behavior. Built-in V1 optics are a fallback; viewer-specific QR calibration remains optional and can improve fit. No claim is made that one profile fits every viewer.

## Verification boundary

`CardboardEntryChecks.Run` exercises the actual product entry/exit/scan coroutines against an injected synthetic SDK boundary: fresh entry without saved parameters, repeated/return entry, exit, optional scan cancellation, accepted parameter reload, failed start and retry. These synthetic lifecycle checks passed in Controls40 and Polish47. The actual Home mode button also passed its Editor simulation enter/exit callback roundtrip; no phone execution is implied.

Synthetic checks cannot establish Android scanner permissions/cancellation, real saved-profile persistence, stereo output, lens fit, head tracking, trigger input, suspend/resume, comfort or device performance. These require an APK installed on a physical phone/viewer. The source review alone is not hardware acceptance.


<!-- RELEASE CHECKPOINT BEGIN -->
Current release 2026-10-20T05:01:36+07:00: controls 1214 recorded checks and visual review 2640 recorded checks. Exact receipt timestamps are in BUILD_STATE.md. These are native Editor/synthetic checks; Android/viewer installation, optical fit and physical acceptance remain pending.
<!-- RELEASE CHECKPOINT END -->
