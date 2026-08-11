using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public class NPCInteractionRange : MonoBehaviour
{
    [SerializeField] private NPCInteraction npcInteraction;
    [SerializeField] private float interactionRange = 2.5f;

    private readonly Dictionary<PlayerCore, int> playerOverlapCounts = new Dictionary<PlayerCore, int>();

    private SphereCollider interactionCollider;

    private void Awake()
    {
        interactionCollider = GetComponent<SphereCollider>();

        if (npcInteraction == null)
            npcInteraction = GetComponentInParent<NPCInteraction>();

        ApplyColliderSettings();

        if (npcInteraction != null)
            return;

        Debug.LogError("[NPCInteractionRange] NPCInteraction을 찾을 수 없습니다.", this);
        enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerCore playerCore = other.GetComponentInParent<PlayerCore>();

        if (playerCore == null)
            return;

        if (playerOverlapCounts.TryGetValue(playerCore, out int overlapCount))
        {
            playerOverlapCounts[playerCore] = overlapCount + 1;
            return;
        }

        playerOverlapCounts.Add(playerCore, 1);
        npcInteraction.RegisterPlayer(playerCore);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCore playerCore = other.GetComponentInParent<PlayerCore>();

        if (playerCore == null || !playerOverlapCounts.TryGetValue(playerCore, out int overlapCount))
            return;

        if (overlapCount > 1)
        {
            playerOverlapCounts[playerCore] = overlapCount - 1;
            return;
        }

        playerOverlapCounts.Remove(playerCore);
        npcInteraction.UnregisterPlayer(playerCore);
    }

    private void OnDisable()
    {
        if (npcInteraction != null)
        {
            foreach (PlayerCore playerCore in playerOverlapCounts.Keys)
            {
                if (playerCore != null)
                    npcInteraction.UnregisterPlayer(playerCore);
            }
        }

        playerOverlapCounts.Clear();
    }

    private void Reset()
    {
        interactionCollider = GetComponent<SphereCollider>();
        npcInteraction = GetComponentInParent<NPCInteraction>();
        ApplyColliderSettings();
    }

    private void OnValidate()
    {
        interactionRange = Mathf.Max(0.1f, interactionRange);

        if (interactionCollider == null)
            interactionCollider = GetComponent<SphereCollider>();

        ApplyColliderSettings();
    }

    private void ApplyColliderSettings()
    {
        if (interactionCollider == null)
            return;

        interactionCollider.isTrigger = true;
        interactionCollider.radius = interactionRange;
    }
}
