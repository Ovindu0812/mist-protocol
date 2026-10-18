namespace MistProtocol.Interaction
{
    /// <summary>Anything the player can use with the E key.</summary>
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        void Interact();
    }

    /// <summary>Interactables that must be held (E held down) for a duration.</summary>
    public interface IHoldInteractable : IInteractable
    {
        float HoldProgress { get; }
        void Hold(float deltaTime);
    }
}
