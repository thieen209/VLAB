using System;
using UnityEngine;

namespace VLAB.DemoLabs
{
    public interface IVLabResettable { void ResetState(); }

    public class VLabInteractable : MonoBehaviour
    {
        [SerializeField] private string contextLabel;
        private Renderer[] surfaces;
        private MaterialPropertyBlock highlight;
        private MaterialPropertyBlock[] originalBlocks;
        private bool isFocused;
        public string ContextLabel { get => contextLabel; set => contextLabel = value; }
        public event Action Activated;
        public virtual void Activate() => Activated?.Invoke();
        public virtual void Rotate(float amount) { }
        public void SetFocus(bool focused)
        {
            if (isFocused == focused) return;
            isFocused = focused;
            if (surfaces == null)
            {
                surfaces = GetComponentsInChildren<Renderer>();
                originalBlocks = new MaterialPropertyBlock[surfaces.Length];
                for (var i = 0; i < surfaces.Length; i++) originalBlocks[i] = new MaterialPropertyBlock();
            }
            if (highlight == null) highlight = new MaterialPropertyBlock();
            for (var i = 0; i < surfaces.Length; i++)
            {
                var surface = surfaces[i]; if (surface == null) continue;
                if (focused)
                {
                    surface.GetPropertyBlock(originalBlocks[i]); surface.GetPropertyBlock(highlight);
                    highlight.SetColor("_EmissionColor", originalBlocks[i].GetColor("_EmissionColor") + new Color(.06f, .17f, .14f));
                    surface.SetPropertyBlock(highlight);
                }
                else surface.SetPropertyBlock(originalBlocks[i]);
            }
        }
    }
}
