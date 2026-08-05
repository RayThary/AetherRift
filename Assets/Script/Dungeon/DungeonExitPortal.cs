using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DungeonExitPortal : MonoBehaviour
{
    [SerializeField] private int lobbySceneIndex = 2;

    private bool hasEnteredPortal;

    private void OnTriggerEnter(Collider other)
    {
        if (hasEnteredPortal)
            return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null || playerHealth.IsDead)
            return;

        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[DungeonExitPortal] SceneLoader를 찾지 못했습니다.", this);
            return;
        }

        hasEnteredPortal = true;
        SceneLoader.Instance.LoadScene(lobbySceneIndex);
    }

    private void Reset()
    {
        Collider portalCollider = GetComponent<Collider>();

        if (portalCollider != null)
            portalCollider.isTrigger = true;
    }
}
