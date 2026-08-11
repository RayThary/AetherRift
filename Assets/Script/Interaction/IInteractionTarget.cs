using UnityEngine;

public interface IInteractionTarget
{
    Transform TargetTransform { get; }
    PlayerCore PlayerInRange { get; }
    string PromptMessage { get; }
    bool CanInteract { get; }

    void Interact();
}
