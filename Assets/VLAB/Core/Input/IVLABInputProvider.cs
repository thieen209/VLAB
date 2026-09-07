namespace VLAB.Core.Input
{
    public interface IVLABInputProvider
    {
        string ProviderName { get; }
        bool InteractionPressed { get; }
        bool ResetPressed { get; }
        VLABInputState ReadState();
    }
}
