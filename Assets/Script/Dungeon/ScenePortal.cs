using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ScenePortal : MonoBehaviour, IInteractable
{
    [Header("Scene")]
    [SerializeField] private int targetSceneIndex;

    [Header("Prompt")]
    [SerializeField] private string promptMessage = "F  포탈 타기";

    private PlayerCore playerInRange;
    private PlayerHealth playerHealth;
    private PlayerInteraction playerInteraction;
    private int playerOverlapCount;

    public Transform InteractionTransform => transform;
    public string PromptMessage => promptMessage;
    public bool CanInteract => playerInRange != null && playerHealth != null && !playerHealth.IsDead && playerInRange.CurrentState == PlayerState.Locomotion;

    public void Interact()
    {
        if (!CanInteract)
            return;

        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[ScenePortal] SceneLoader를 찾지 못했습니다.", this);
            return;
        }

        SceneLoader.Instance.LoadScene(targetSceneIndex);
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
            Debug.LogError("[ScenePortal] PlayerInteraction을 찾지 못했습니다.", this);
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

        if (playerInteraction != null)
            playerInteraction.UnregisterInteractable(this);

        playerInRange = null;
        playerHealth = null;
        playerInteraction = null;
        playerOverlapCount = 0;
    }

    private void OnDisable()
    {
        if (playerInteraction != null)
            playerInteraction.UnregisterInteractable(this);

        playerInRange = null;
        playerHealth = null;
        playerInteraction = null;
        playerOverlapCount = 0;
    }
}
