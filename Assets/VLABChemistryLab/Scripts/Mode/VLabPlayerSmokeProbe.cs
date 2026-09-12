using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace VLAB.ChemistryLab.Mode
{
    /// <summary>Opt-in Windows player probe used only by automated upgrade smoke runs.</summary>
    internal static class VLabPlayerSmokeProbe
    {
        private const string SmokeFlag = "-vlabSmokeTest";
        private const string ArtifactArgument = "-vlabSmokeArtifact";
        private const string ExpectationArgument = "-vlabSmokeExpected";

        [Serializable]
        private sealed class SmokeReport
        {
            public string generatedUtc;
            public string unityVersion;
            public string expectation;
            public string result;
            public string message;
            public string mode;
            public string failureReason;
            public int activeCameraCount;
            public int activeAudioListenerCount;
            public int mainCameraTagCount;
            public bool simulatorPresent;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartWhenRequested()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, SmokeFlag) < 0)
                return;

            GameObject host = new GameObject("__VLAB_PlayerSmokeProbe__");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<ProbeHost>().Begin(args);
        }

        private sealed class ProbeHost : MonoBehaviour
        {
            private string[] args;

            public void Begin(string[] commandLineArgs)
            {
                // Automated hidden players must keep advancing even without desktop focus.
                Application.runInBackground = true;
                args = commandLineArgs;
                StartCoroutine(Run());
            }

            private IEnumerator Run()
            {
                string artifactPath = RequireArgument(args, ArtifactArgument);
                string expectation = OptionalArgument(args, ExpectationArgument, "desktop");
                float deadline = Time.realtimeSinceStartup + 15f;
                ChemistryLabModeController controller = null;

                while (Time.realtimeSinceStartup < deadline)
                {
                    controller = UnityEngine.Object.FindAnyObjectByType<ChemistryLabModeController>();
                    if (controller != null && IsSettled(controller, expectation))
                        break;
                    yield return null;
                }

                SmokeReport report = Evaluate(controller, expectation);
                if (Array.IndexOf(args, "-vlabSmokeSettings") >= 0)
                {
                    var dashboard = UnityEngine.Object.FindAnyObjectByType<DesktopTitrationInterface>();
                    if (dashboard != null) dashboard.OpenSettings();
                    yield return null;
                }
                // Capture the actual player UI, not only an offscreen scene camera.
                if (Array.IndexOf(args, "-vlabSmokeScreenshot") >= 0)
                {
                    yield return new WaitForSecondsRealtime(.5f);
                    ScreenCapture.CaptureScreenshot(Path.ChangeExtension(artifactPath, ".png"));
                    yield return new WaitForSecondsRealtime(1f);
                }
                try
                {
                    string directory = Path.GetDirectoryName(artifactPath);
                    if (!string.IsNullOrEmpty(directory))
                        Directory.CreateDirectory(directory);
                    File.WriteAllText(artifactPath, JsonUtility.ToJson(report, true));
                    Debug.Log($"[VLAB][PLAYER-SMOKE] {report.result}: {report.message}");
                }
                catch (Exception exception)
                {
                    Debug.LogError("[VLAB][PLAYER-SMOKE] Could not write smoke evidence: " + exception);
                    Application.Quit(2);
                    yield break;
                }

                Application.Quit(string.Equals(report.result, "Pass", StringComparison.Ordinal) ? 0 : 1);
            }

            private static bool IsSettled(ChemistryLabModeController controller, string expectation)
            {
                if (string.Equals(expectation, "fallback", StringComparison.OrdinalIgnoreCase))
                    return controller.CurrentMode == VLabPresentationMode.Desktop &&
                           controller.LastFailureReason != VLabModeFailureReason.None;
                return controller.CurrentMode == VLabPresentationMode.Desktop;
            }

            private static SmokeReport Evaluate(ChemistryLabModeController controller, string expectation)
            {
                Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
                AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
                int activeCameras = Array.FindAll(cameras, item => item.isActiveAndEnabled).Length;
                int activeListeners = Array.FindAll(listeners, item => item.isActiveAndEnabled).Length;
                int mainCameraTags = Array.FindAll(cameras, item => item.CompareTag("MainCamera")).Length;
                bool simulatorPresent = FindSceneObject("VLAB XR Interaction Simulator") != null;
                bool fallbackExpected = string.Equals(expectation, "fallback", StringComparison.OrdinalIgnoreCase);
                bool correctMode = controller != null && controller.CurrentMode == VLabPresentationMode.Desktop;
                bool correctFailure = !fallbackExpected ||
                                      (controller != null && controller.LastFailureReason != VLabModeFailureReason.None);
                bool pass = correctMode && correctFailure && activeCameras == 1 && activeListeners == 1 && mainCameraTags == 1;

                return new SmokeReport
                {
                    generatedUtc = DateTime.UtcNow.ToString("O"),
                    unityVersion = Application.unityVersion,
                    expectation = expectation,
                    result = pass ? "Pass" : "Fail",
                    message = pass
                        ? "Windows player reached a safe Desktop presentation with one camera/listener."
                        : "Windows player did not reach the expected Desktop/fallback invariant before timeout.",
                    mode = controller == null ? "MissingController" : controller.CurrentMode.ToString(),
                    failureReason = controller == null ? "MissingController" : controller.LastFailureReason.ToString(),
                    activeCameraCount = activeCameras,
                    activeAudioListenerCount = activeListeners,
                    mainCameraTagCount = mainCameraTags,
                    simulatorPresent = simulatorPresent
                };
            }

            private static GameObject FindSceneObject(string objectName)
            {
                Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
                Transform match = Array.Find(transforms,
                    item => item.gameObject.scene.IsValid() && item.name == objectName);
                return match == null ? null : match.gameObject;
            }

            private static string RequireArgument(string[] commandLineArgs, string key)
            {
                string value = OptionalArgument(commandLineArgs, key, null);
                if (string.IsNullOrWhiteSpace(value))
                    throw new InvalidOperationException("Missing required player smoke argument " + key);
                return Path.GetFullPath(value);
            }

            private static string OptionalArgument(string[] commandLineArgs, string key, string fallback)
            {
                for (int index = 0; index < commandLineArgs.Length - 1; index++)
                {
                    if (string.Equals(commandLineArgs[index], key, StringComparison.OrdinalIgnoreCase))
                        return commandLineArgs[index + 1];
                }
                return fallback;
            }
        }
    }
}
