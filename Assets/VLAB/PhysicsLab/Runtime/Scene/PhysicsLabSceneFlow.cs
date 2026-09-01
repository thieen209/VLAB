using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VLAB.PhysicsLab.Interaction;

namespace VLAB.PhysicsLab.SceneFlow
{
    public sealed class PhysicsLabSceneFlow : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeCanvas;
        [SerializeField, Min(0.05f)] private float fadeDuration = 0.22f;
        [SerializeField] private string startupContentScene = PhysicsLabSceneNames.Hub;

        private Coroutine transition;
        private string currentContentScene;

        public static PhysicsLabSceneFlow Instance { get; private set; }
        public string CurrentContentScene => currentContentScene;
        public bool IsTransitioning => transition != null;

        public void Configure(CanvasGroup fade, string startupScene)
        {
            fadeCanvas = fade;
            startupContentScene = string.IsNullOrWhiteSpace(startupScene) ? PhysicsLabSceneNames.Hub : startupScene;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private IEnumerator Start()
        {
            yield return null;
            currentContentScene = FindLoadedContentScene();
            if (string.IsNullOrEmpty(currentContentScene))
            {
                LoadContent(startupContentScene);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void LoadContent(string sceneName)
        {
            if (transition == null && IsKnownContentScene(sceneName) && sceneName != currentContentScene)
            {
                transition = StartCoroutine(SwitchContent(sceneName));
            }
        }

        public void LoadHub() => LoadContent(PhysicsLabSceneNames.Hub);
        public void ReturnToMainMenu()
        {
            if (transition == null)
            {
                transition = StartCoroutine(LoadMainMenu());
            }
        }

        private IEnumerator SwitchContent(string nextScene)
        {
            yield return FadeTo(1f);

            var loadedContent = FindLoadedContentScene();
            if (!string.IsNullOrEmpty(loadedContent))
            {
                var unload = SceneManager.UnloadSceneAsync(loadedContent);
                if (unload != null)
                {
                    yield return unload;
                }
            }

            var load = SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
            if (load == null)
            {
                Debug.LogError($"Unable to load Physics Lab content scene '{nextScene}'.", this);
                transition = null;
                yield return FadeTo(0f);
                yield break;
            }
            yield return load;

            currentContentScene = nextScene;
            var loadedScene = SceneManager.GetSceneByName(nextScene);
            if (loadedScene.IsValid())
            {
                SceneManager.SetActiveScene(loadedScene);
            }
            FindAnyObjectByType<DesktopPlayerRig>()?.ReleaseCursor();
            yield return FadeTo(0f);
            transition = null;
        }

        private IEnumerator LoadMainMenu()
        {
            yield return FadeTo(1f);
            SceneManager.LoadScene(PhysicsLabSceneNames.Home, LoadSceneMode.Single);
        }

        private IEnumerator FadeTo(float target)
        {
            if (fadeCanvas == null)
            {
                yield break;
            }

            fadeCanvas.blocksRaycasts = true;
            var start = fadeCanvas.alpha;
            var elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                // Headless and unthrottled test runs can report near-zero frame deltas.
                // A 240 Hz floor keeps transitions finite without changing normal gameplay timing.
                elapsed += Mathf.Max(Time.unscaledDeltaTime, 1f / 240f);
                fadeCanvas.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            fadeCanvas.alpha = target;
            fadeCanvas.blocksRaycasts = target > 0.01f;
        }

        private static bool IsKnownContentScene(string sceneName)
        {
            foreach (var candidate in PhysicsLabSceneNames.ContentScenes)
            {
                if (candidate == sceneName)
                {
                    return true;
                }
            }
            return false;
        }

        private static string FindLoadedContentScene()
        {
            foreach (var candidate in PhysicsLabSceneNames.ContentScenes)
            {
                if (SceneManager.GetSceneByName(candidate).isLoaded)
                {
                    return candidate;
                }
            }
            return null;
        }
    }
}
