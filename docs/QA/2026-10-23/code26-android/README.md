# Code26: build passes, native keyboard cancellation FAIL

APK VLAB-0.1.9-finalpass-candidate-code26.apk, 126562404 bytes, SHA256 532A68B4E98055BD2A532043A7AC26464CFBF489C3181403022651000ED88A4E. Unity release completed21:27:46 Vietnam, 0errors43warnings, v2 signature valid, com.nguaman.vlab/code26/min26/ARMv7+ARM64. Source fix450ddca plus version26 builder.

Installed-r on API35 VLAB_014_API35 emulator, cold launchStatusok, centered home intact. New journal title QA26-Cancel-kept was visibly typed in Android native overlay (code26-typing.png). System Back dismissed keyboard and title became empty (code26-cancel.png). Repeated with a two-second delay before Back; same failure. The Editor14PASS result did not cover the native overlay buffer behavior. Do NOT call code26 a verified fix for this bug.

Next minimal hypothesis: inline mobile field editing (shouldHideMobileInput=true, soft keyboard still enabled) avoids the separate native edit overlay. Source modified after this APK; targeted Editor test PID33720/log notebook-inline.log is running. Must test a new APK on native Android before closing. No physical phone evidence. Emulator PID10244 was gracefully stopped after test.

PID33720 finished:14 existing checks passed, two new mobile-configuration assertions failed because TMP Editor accessors force desktop values. Replaced those invalid assertions with explicit UNVERIFIED entries; native APK testing remains required. No live Unity/emulator remains.
