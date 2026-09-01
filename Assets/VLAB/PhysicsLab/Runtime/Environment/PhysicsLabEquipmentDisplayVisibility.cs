using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VLAB.PhysicsLab.Environment
{
    public sealed class PhysicsLabEquipmentDisplayVisibility : MonoBehaviour
    {
        [Serializable]
        private struct DisplayBinding
        {
            public GameObject displayObject;
            public string hiddenInScene;
            public string alsoHiddenInScene;
        }

        [SerializeField] private DisplayBinding[] bindings = Array.Empty<DisplayBinding>();

        public void Configure(GameObject[] displayObjects, string[] hiddenScenes, string[] secondaryHiddenScenes)
        {
            if (displayObjects == null || hiddenScenes == null || secondaryHiddenScenes == null ||
                displayObjects.Length != hiddenScenes.Length || displayObjects.Length != secondaryHiddenScenes.Length)
            {
                throw new ArgumentException("Equipment display visibility arrays must be non-null and have matching lengths.");
            }

            bindings = new DisplayBinding[displayObjects.Length];
            for (var index = 0; index < displayObjects.Length; index++)
            {
                bindings[index] = new DisplayBinding
                {
                    displayObject = displayObjects[index],
                    hiddenInScene = hiddenScenes[index],
                    alsoHiddenInScene = secondaryHiddenScenes[index],
                };
            }
            RefreshVisibility();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneChanged;
            SceneManager.sceneUnloaded += HandleSceneChanged;
            RefreshVisibility();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneChanged;
            SceneManager.sceneUnloaded -= HandleSceneChanged;
        }

        private void HandleSceneChanged(Scene scene, LoadSceneMode mode) => RefreshVisibility();
        private void HandleSceneChanged(Scene scene) => RefreshVisibility();

        private void RefreshVisibility()
        {
            foreach (var binding in bindings)
            {
                if (binding.displayObject == null)
                {
                    continue;
                }

                var hidden = IsLoaded(binding.hiddenInScene) || IsLoaded(binding.alsoHiddenInScene);
                binding.displayObject.SetActive(!hidden);
            }
        }

        private static bool IsLoaded(string sceneName) =>
            !string.IsNullOrWhiteSpace(sceneName) && SceneManager.GetSceneByName(sceneName).isLoaded;
    }
}
