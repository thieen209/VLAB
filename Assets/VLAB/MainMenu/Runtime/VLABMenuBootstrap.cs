using UnityEngine;
using UnityEngine.SceneManagement;
using VLAB.PhysicsLab.SceneFlow;
using VLAB.PhysicsLab.Interaction;

namespace VLAB.MainMenu
{
    public static class VLABMenuBootstrap
    {
        public const string MenuScene = "Menu";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
        }
        private static void Loaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != MenuScene && scene.name != PhysicsLabSceneNames.Base && scene.name != "ChemistryLab" && scene.name != "BiologyLab" && scene.name != "EngineeringLab") return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<VLABApplicationUI>(true) != null) return;
            bool menu = scene.name == MenuScene;
            if (menu)
            {
                // The supplied Menu scene is a copy of Base. Retain its environment and
                // camera, but prevent its content loader from opening a lab behind onboarding.
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach(var flow in root.GetComponentsInChildren<PhysicsLabSceneFlow>(true)) flow.enabled = false;
                    foreach(var rig in root.GetComponentsInChildren<DesktopPlayerRig>(true)) rig.enabled = false;
                    foreach(var locomotion in root.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionProvider>(true)) locomotion.enabled = false;
                    foreach(var grab in root.GetComponentsInChildren<GrabController>(true)) grab.enabled = false;
                    foreach(var ray in root.GetComponentsInChildren<InteractionRaycaster>(true)) ray.enabled = false;
                    foreach(var canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.gameObject.SetActive(false);
                }
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            var host = new GameObject(menu ? "VLAB Application" : "VLAB Lab Menu");
            SceneManager.MoveGameObjectToScene(host, scene);
            host.AddComponent<VLABApplicationUI>().Initialize(menu);
            host.AddComponent<VLabViewerRuntime>();
            if (menu)
            {
                foreach (var input in Object.FindObjectsByType<VLAB.Core.Input.InputManager>()) input.TranslationLocked = true;
                host.AddComponent<VLabMenuEnvironment>();
            }
        }
    }
}
