using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace VLAB.ChemistryLab.Interaction
{
    public sealed class LabButtonFeedback : MonoBehaviour
    {
        [SerializeField] private bool interactionEnabled = true;
        private readonly LabPressGate gate = new LabPressGate();
        private XRSimpleInteractable interactable;
        private Vector3 restPosition;
        private Renderer surface;
        private MaterialPropertyBlock colors;
        private Color restColor;
        private bool hovered;
        private bool pressed;
        private float releaseAt;

        private void Awake()
        {
            restPosition = transform.localPosition;
            surface = GetComponent<Renderer>();
            colors = new MaterialPropertyBlock();
            restColor = surface != null && surface.sharedMaterial != null ? surface.sharedMaterial.color : Color.white;
            interactable = GetComponent<XRSimpleInteractable>();
            if (interactable != null)
            {
                interactable.firstHoverEntered.AddListener(OnHover);
                interactable.lastHoverExited.AddListener(OnUnhover);
            }
        }

        public bool TryPress()
        {
            if (!gate.TryPress(Time.unscaledTimeAsDouble, interactionEnabled && isActiveAndEnabled)) return false;
            LabUiAudio.PlayClick();
            pressed = true;
            releaseAt = Time.unscaledTime + .12f;
            transform.localPosition = restPosition + Vector3.down * .006f;
            Paint();
            return true;
        }

        private void Update()
        {
            if (!pressed || Time.unscaledTime < releaseAt) return;
            pressed = false;
            transform.localPosition = restPosition;
            Paint();
        }

        private void OnHover(HoverEnterEventArgs args) { hovered = true; Paint(); }
        private void OnUnhover(HoverExitEventArgs args) { hovered = false; Paint(); }
        private void Paint()
        {
            if (surface == null) return;
            surface.GetPropertyBlock(colors);
            colors.SetColor("_Color", Color.Lerp(restColor, Color.white, pressed ? .4f : hovered ? .2f : 0f));
            surface.SetPropertyBlock(colors);
        }

        private void OnDisable() { transform.localPosition = restPosition; pressed = false; hovered = false; Paint(); }
        private void OnDestroy()
        {
            if (interactable == null) return;
            interactable.firstHoverEntered.RemoveListener(OnHover);
            interactable.lastHoverExited.RemoveListener(OnUnhover);
        }
    }
}
