using System;
using UnityEngine;
using VLAB.PhysicsLab.Interaction;

namespace VLAB.PhysicsLab.Common
{
    public class LabInteractable : MonoBehaviour, IInteractable, IActivatable
    {
        [SerializeField] private bool interactionEnabled = true;
        [SerializeField] private Color highlightColor = new Color(0f, 0.71f, 0.85f, 1f);

        private Renderer[] renderers;
        private MaterialPropertyBlock highlightBlock;

        public event Action InteractionBegan;
        public event Action InteractionEnded;
        public event Action Activated;

        public bool CanInteract => interactionEnabled && isActiveAndEnabled;
        public Transform InteractionTransform => transform;
        public bool IsInteracting { get; private set; }

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            highlightBlock = new MaterialPropertyBlock();
            highlightBlock.SetColor("_Color", highlightColor);
            highlightBlock.SetColor("_EmissionColor", highlightColor * 0.35f);
        }

        public void SetInteractionEnabled(bool value)
        {
            interactionEnabled = value;
            if (!value)
            {
                SetHighlighted(false);
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            if (!Education.PhysicsLabPreferences.InteractionOutlines)
            {
                highlighted = false;
            }
            if (renderers == null)
            {
                return;
            }

            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer != null)
                {
                    targetRenderer.SetPropertyBlock(highlighted ? highlightBlock : null);
                }
            }
        }

        public void BeginInteraction()
        {
            if (!CanInteract || IsInteracting)
            {
                return;
            }

            IsInteracting = true;
            InteractionBegan?.Invoke();
        }

        public void EndInteraction()
        {
            if (!IsInteracting)
            {
                return;
            }

            IsInteracting = false;
            InteractionEnded?.Invoke();
        }

        public void Activate()
        {
            if (CanInteract)
            {
                Activated?.Invoke();
            }
        }
    }
}
