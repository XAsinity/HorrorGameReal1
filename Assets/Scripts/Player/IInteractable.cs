/// <summary>
/// Implement this on any object the player can interact with:
/// doors, terminals, items, switches, etc.
/// </summary>
public interface IInteractable
{
    string InteractionPrompt { get; }
    void Interact(UnityEngine.GameObject interactor);
}
