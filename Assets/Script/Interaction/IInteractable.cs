using UnityEngine;

public interface IInteractable
{
    Transform InteractionTransform { get; }
    string PromptMessage { get; }
    bool CanInteract { get; }

    void Interact();
}
