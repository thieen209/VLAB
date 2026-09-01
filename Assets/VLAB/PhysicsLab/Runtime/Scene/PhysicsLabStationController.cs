using UnityEngine;
using VLAB.Core.Input;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.SceneFlow
{
    public sealed class PhysicsLabStationController : MonoBehaviour
    {
        [SerializeField] private string experimentId;
        [SerializeField] private string vietnameseTitle;
        [SerializeField] private ExperimentResetManager resetManager;

        private InputManager inputManager;

        public string ExperimentId => experimentId;
        public string VietnameseTitle => vietnameseTitle;

        public void Configure(string id, string title, ExperimentResetManager manager)
        {
            experimentId = id;
            vietnameseTitle = title;
            resetManager = manager;
        }

        private void Start()
        {
            inputManager = FindAnyObjectByType<InputManager>();
            if (inputManager != null)
            {
                inputManager.ResetPressed += ResetStation;
            }
            resetManager?.CaptureAll();
        }

        private void OnDestroy()
        {
            if (inputManager != null)
            {
                inputManager.ResetPressed -= ResetStation;
            }
        }

        public void ResetStation()
        {
            FindAnyObjectByType<Interaction.GrabController>()?.Release();
            var physicalController = GetComponent<Education.ExperimentPhysicalController>();
            if (physicalController != null)
            {
                physicalController.ResetCurrentTrial();
            }
            else
            {
                resetManager?.ResetAll();
            }
        }

        public void BackToHub() => PhysicsLabSceneFlow.Instance?.LoadHub();
    }
}
