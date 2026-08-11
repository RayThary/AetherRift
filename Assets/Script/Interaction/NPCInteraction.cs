using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class NPCInteraction : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [SerializeField] private string promptMessage = "F  대화하기";

    [Header("Interaction Event")]
    [SerializeField] private UnityEvent onInteract;

    private PlayerCore playerInRange;
    private PlayerInteraction playerInteraction;

    public Transform InteractionTransform => transform;
    public string PromptMessage => promptMessage;
    public bool CanInteract => playerInRange != null && playerInRange.CurrentState == PlayerState.Locomotion;

    public void RegisterPlayer(PlayerCore playerCore)
    {
        if (playerCore == null || playerInRange != null)
            return;

        playerInteraction = playerCore.GetComponent<PlayerInteraction>();

        if (playerInteraction == null)
        {
            Debug.LogError("[NPCInteraction] PlayerInteraction을 찾지 못했습니다.", this);
            return;
        }

        playerInRange = playerCore;
        playerInteraction.RegisterInteractable(this);
    }

    public void UnregisterPlayer(PlayerCore playerCore)
    {
        if (playerInRange != playerCore)
            return;

        if (playerInteraction != null)
            playerInteraction.UnregisterInteractable(this);

        playerInRange = null;
        playerInteraction = null;
    }

    public void Interact()
    {
        if (!CanInteract)
            return;

        onInteract?.Invoke();
    }

    private void OnDisable()
    {
        if (playerInteraction != null)
            playerInteraction.UnregisterInteractable(this);

        playerInRange = null;
        playerInteraction = null;
    }
}
