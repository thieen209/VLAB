# Authored roller blinds and indoor lighting

Four rooms now use an attributed CC0 blind with a separate fabric/bottom bar. Original shade objects are retained with renderers/colliders disabled except the existing pull interaction. Backups remain in the working project under Evidence/FinalPass/Rooms/BeforeShade and Evidence/CurtainLighting/Before.

Indoor-only lightmaps/probes were rebuilt for Physics, Chemistry, Biology and Engineering. Window fill and sun are real-time, controlled by the shade opening fraction using fixed baseline intensities. Indoor lamps remain independent. A settled curtain requests a time-sliced reflection update. Quality switching reapplies the curtain state. The legacy shader/pipeline and XR head pose are unchanged.

41 Editor interaction checks passed after the final code changes, including simulated tracked-head gaze on the actual curtain collider, single activation per dwell, and deliberate reopening. Four authored model endpoint/reversal/10-toggle checks passed. The final four-room HUD/lighting pass completed139 checks with0 failures. Same-camera runtime images cover P01/C04/B01/M01 with the final window spotlight. Visual inspection still finds shadowed apparatus too dark in places; lighting polish and full device/performance acceptance remain open. No Android device, stereo comfort or GPU performance certification is implied.

The first synchronous post-bake open Physics capture omitted geometry; it is rejected as visual evidence. Use the runtime same-camera open/closed captures after the final QA run. The previous 139-check run proved logic/layout but showed insufficient daylight difference, prompting the final inward window spotlight adjustment. APK code22 predates these changes; no new APK or public release yet.
