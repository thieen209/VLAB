using System;
using UnityEngine;
using VLAB.PhysicsLab.Common;

namespace VLAB.PhysicsLab.Mechanics
{
    [RequireComponent(typeof(Collider), typeof(LabInteractable))]
    public sealed class FrictionSurfaceSelector : MonoBehaviour
    {
        [SerializeField] private string surfaceName = "Gỗ";
        [SerializeField, Range(0f, 2f)] private float staticCoefficient = 0.45f;
        [SerializeField, Range(0f, 2f)] private float kineticCoefficient = 0.30f;
        [SerializeField] private FrictionBlock targetBlock;
        [SerializeField] private Renderer surfaceRenderer;

        private LabInteractable interactable;
        private MaterialPropertyBlock visualBlock;

        public event Action<FrictionSurfaceSelector> Selected;
        public string SurfaceName => surfaceName;
        public float StaticCoefficient => staticCoefficient;
        public float KineticCoefficient => kineticCoefficient;

        public void Configure(string label, float staticMu, float kineticMu, FrictionBlock block, Renderer targetRenderer)
        {
            surfaceName = string.IsNullOrWhiteSpace(label) ? "Bề mặt" : label;
            staticCoefficient = Mathf.Max(0f, staticMu);
            kineticCoefficient = Mathf.Max(0f, kineticMu);
            targetBlock = block;
            surfaceRenderer = targetRenderer;
        }

        private void Awake()
        {
            interactable = GetComponent<LabInteractable>();
            visualBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (interactable == null) interactable = GetComponent<LabInteractable>();
            interactable.Activated += SelectSurface;
        }

        private void OnDisable()
        {
            if (interactable != null) interactable.Activated -= SelectSurface;
        }

        public void SelectSurface()
        {
            if (targetBlock == null) return;
            targetBlock.SetParameters(targetBlock.MassKilograms, staticCoefficient, kineticCoefficient);
            ShowSelected();
            Selected?.Invoke(this);
        }

        private void ShowSelected()
        {
            if (surfaceRenderer == null) return;
            visualBlock.Clear();
            visualBlock.SetColor("_EmissionColor", new Color(0.02f, 0.35f, 0.22f));
            surfaceRenderer.SetPropertyBlock(visualBlock);
        }
    }
}
