using UnityEngine;

namespace VLAB.PhysicsLab.Interaction
{
    public interface IInteractable
    {
        bool CanInteract { get; }
        Transform InteractionTransform { get; }
        void SetHighlighted(bool highlighted);
        void BeginInteraction();
        void EndInteraction();
    }

    public interface IActivatable
    {
        void Activate();
    }

    public interface IGrabbable : IInteractable
    {
        bool CanGrab { get; }
        Rigidbody Body { get; }
        void OnGrabbed(Transform grabAnchor);
        void OnReleased();
    }

    public interface IResettable
    {
        void ResetLabObject();
    }
}
