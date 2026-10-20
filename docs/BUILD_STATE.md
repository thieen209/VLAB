# VLAB0.1.4 / Android code5

Offline Vietnamese-first phone/Cardboard prototype. Application label VLAB, package com.nguaman.vlab, white supplied VLAB launcher artwork. ARMv7+ARM64, Android8/API26+. Artifact signature and metadata are recorded separately.

This patch addresses phone canvas scaling, movement geometry, Cardboard pose gating and a level tracking origin, missing-gyro fallback, adjustable joystick size, settings targeting/layout, and experiment usability. Refer to the committed source and check receipts for the exact delivered changes; a feature appearing in source is not hardware acceptance.

## Evidence scope

The full32 Editor UI baseline run below achieved28 saved-and-reopened journeys. Later selected runs bring historical distinct saved-and-reopened coverage to30/32; this is not a final-source full32 pass. C01 remains manually incomplete. B08 remains incomplete after its UI test selected an image and reset the page, making the page1 stage button unavailable to that navigation sequence. It predates later fixes and is not final-source32/32 certification. Later selected runs are listed separately; do not aggregate different source snapshots into a full-suite pass. Check receipts retain their original PASS/FAIL text and scope. The user did not request a test matrix in the handoff.

- Run `20261020-120415`: 28/32 selected lessons saved and reopened; statuses {'UI JOURNEY RECORDED': 28, 'BLOCKED': 4}. Scope: Editor queued touchscreen through actual UGUI. Not physical Android or VR certification.
- Run `20261020-125821`: 4/4 selected lessons saved and reopened; statuses {'UI JOURNEY RECORDED': 4}. Scope: Editor queued touchscreen through actual UGUI. Not physical Android or VR certification.
- Run `20261020-131222`: 2/2 selected lessons saved and reopened; statuses {'UI JOURNEY RECORDED': 2}. Scope: Editor queued touchscreen through actual UGUI. Not physical Android or VR certification.
- Run `20261020-131744`: 0/1 selected lessons saved and reopened; statuses {'BLOCKED': 1}. Scope: Editor queued touchscreen through actual UGUI. Not physical Android or VR certification.
- Run `20261020-132412`: 2/3 selected lessons saved and reopened; statuses {'UI JOURNEY RECORDED': 2, 'BLOCKED': 1}. Scope: Editor queued touchscreen through actual UGUI. Not physical Android or VR certification.

APK SHA256: `355DDD7E78A3064F0E8355B5F47493461025907AB029C97327C58667884F4967`; 117782805 bytes. Download URL is supplied only after verified release upload.

## Limits

Physical Android installation/input, phone sensor axes and recentering, Cardboard optical comfort, BLE/controller hardware, and device performance remain unverified unless a later explicit device receipt is added. Native Editor tests do not certify those claims. Existing profiles and supplied models are retained. Any incomplete/failed journey in the attached receipts remains an explicit limit until independently retested after its fix.
