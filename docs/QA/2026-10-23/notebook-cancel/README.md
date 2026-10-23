# Notebook keyboard cancellation

Source fix after code25: title/body TMP inputs now retain drafts on Cancel instead of restoring original text. Installed TMP source confirms Android keyboard Canceled sets wasCanceled; default restoreOriginalTextOnEscape=true discarded edits.

FinalPassHudChecks.NotebookExit ran in Unity 6000.6.4f1, PID46484 terminated. 14 PASS, 0 FAIL: includes actual title/body activation and OnCancel, Back disk persistence and failed-write recovery. This is Editor callback evidence, not Android IME verification. APK code25 does not contain this fix. Next: build a new candidate and verify native keyboard Back with the keyboard open; do not overwrite code25 or claim this source fix shipped.
