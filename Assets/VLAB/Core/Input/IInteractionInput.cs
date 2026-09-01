using System;

namespace VLAB.Core.Input
{
    public interface IInteractionInput
    {
        VLABInputState CurrentState { get; }
        event Action InteractionPressed;
        event Action InteractionReleased;
        event Action DropPressed;
        event Action ResetPressed;
        event Action PausePressed;
    }
}
