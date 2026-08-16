using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class DungeonEntranceInteraction : MonoBehaviour, IInteractable
{
    [SerializeField] private DungeonSelectionUI dungeonSelectionUI;
    [SerializeField] private string promptMessage = "F 던전 입장";

    private PlayerCore playerInRange;
    private PlayerHealth playerHealth;
    private PlayerInteraction playerInteraction;
    private int playerOverlapCount;

    public Transform InteractionTransform => transform;
    public string PromptMessage => promptMessage;
    public bool CanInteract => playerInRange != null && playerHealth != null && !playerHealth.IsDead && !playerInRange.IsPlayerPaused && playerInRange.CurrentState == PlayerState.Locomotion;

    private void Awake()
    {
        if (dungeonSelectionUI == null)
            dungeonSelectionUI = FindFirstObjectByType<DungeonSelectionUI>(FindObjectsInactive.Include);
    }

    public void Interact()
    {
        if (!CanInteract || dungeonSelectionUI == null)
            return;

        dungeonSelectionUI.Open();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCore playerCore = other.GetComponentInParent<PlayerCore>();
        PlayerHealth foundPlayerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerCore == null || foundPlayerHealth == null || foundPlayerHealth.IsDead)
            return;

        if (playerInRange == playerCore)
        {
            playerOverlapCount++;
            return;
        }

        if (playerInRange != null)
            return;

        PlayerInteraction foundPlayerInteraction = playerCore.GetComponent<PlayerInteraction>();

        if (foundPlayerInteraction == null)
        {
            Debug.LogError("[DungeonEntranceInteraction] PlayerInteraction을 찾지 못했습니다.", this);
            return;
        }

        playerInRange = playerCore;
        playerHealth = foundPlayerHealth;
        playerInteraction = foundPlayerInteraction;
        playerOverlapCount = 1;
        playerInteraction.RegisterInteractable(this);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCore playerCore = other.GetComponentInParent<PlayerCore>();

        if (playerCore == null || playerCore != playerInRange)
            return;

        playerOverlapCount--;

        if (playerOverlapCount > 0)
            return;

        ClearPlayer();
    }

    private void ClearPlayer()
    {
        if (playerInteraction != null)
            playerInteraction.UnregisterInteractable(this);

        playerInRange = null;
        playerHealth = null;
        playerInteraction = null;
        playerOverlapCount = 0;
    }

    private void OnDisable()
    {
        ClearPlayer();
    }
}