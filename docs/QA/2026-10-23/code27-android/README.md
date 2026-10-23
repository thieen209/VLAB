# Code27 native keyboard cancellation FAIL

Build finished21:45:16VN,0errors43warnings, v2signature verified. Version27/min26/target36 installed over code26, coldlaunchStatusok. APK126563604bytes; SHA256 2B910F068D45BCAE7627A3B14C8F606090A9D106769161B76ACBDEF3DA14E3F4. Source79f9c7f plus version27 builder.

Actual Android inline keyboard displayed QA27-Cancel-kept directly in the journal title (typing screenshot), then native Back erased it (cancel screenshot). Inline edit alone does not fix cancellation. No notebook cancellation PASS claim. Original saved QA014-P01 and QA25-Saved-note remain visible.

Installed TMP source LateUpdate copies m_SoftKeyboard.text before handling Canceled; disabling restoreOriginalTextOnEscape only fixes the later restoration path. Next source change caches accepted field edits and rejects the canceled keyboard buffer in onValueChanged, retaining the profile draft and field text. This new branch still needs native APK verification; not included in code27. Scoped Editor compilation/save regression is running as PID17864, log work/final-pass-20261023/notebook-cancel-buffer.log. Emulator34212 stopped normally. No physical-device test.

PID17864 finished;14 Editor checks PASS,2 explicit Android UNVERIFIED. Native canceled-buffer guard not covered by Editor; next APK must validate it. No processes owned by this check remain.
