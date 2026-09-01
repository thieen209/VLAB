using UnityEngine;
using UnityEngine.SceneManagement;

namespace VLAB.PhysicsLab.SceneFlow
{
    /// <summary>
    /// Makes every Physics Lab content scene safe to run directly from the Editor
    /// while keeping the environment in one shared additive Base scene.
    /// </summary>
    public static class PhysicsLabSceneBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneBootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (SceneManager.GetSceneByName(PhysicsLabSceneNames.Base).isLoaded ||
                !IsPhysicsLabContentScene(scene.name))
            {
                return;
            }

            // Synchronous loading here is intentional: the camera, EventSystem and
            // shared floor must exist before the first playable frame is rendered.
            SceneManager.LoadScene(PhysicsLabSceneNames.Base, LoadSceneMode.Additive);
        }

        private static bool IsPhysicsLabContentScene(string sceneName)
        {
            foreach (var contentScene in PhysicsLabSceneNames.ContentScenes)
            {
                if (contentScene == sceneName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
