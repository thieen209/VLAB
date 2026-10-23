# BLE checkpoint — 23 October 2026

Implementation includes foreground Android GATT transport, bounded configurable packet decoder, shared input action provider, independent profiles, calibration and UI. Firmware UUIDs/payload remain unknown. The transport is real Android API code; tests below use a simulated transport and do not establish hardware interoperability.

- `ble-core-checks.txt`: 18 passing checks, zero failures: lifecycle, sequence ordering/wrap, invalid/stale packets, profile persistence/backup/corruption protection/reset archives.
- `ble-ui-checks.txt`: 18 passing checks, zero failures: VI/EN connection/profile/calibration modals at1600x900, text overflow and close callback cleanup. Runtime images reviewed; paired profile buttons improved. Not queued-touch or multi-aspect certification.
- Existing interaction regression:38PASS after provider integration; later changes concern BLE UI and decoder validation.
- Android Java transport compiled against SDK36. Signed code22 build is still running at this checkpoint. No new APK acceptance.
- Physical BLE, permissions/GATT lifecycle on a device, controller pose axes, source handoff integration matrix, wider layout coverage and full32 final-revision journeys remain unverified.

See `../../../FinalPass/BLE-PROTOCOL.md` for the configurable adapter and explicitly labelled protocol proposal. Learner storage and signing files were not copied or changed by this checkpoint.
