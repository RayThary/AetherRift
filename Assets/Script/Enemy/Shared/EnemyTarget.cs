using UnityEngine;

public class EnemyTarget : MonoBehaviour
{
    private GameManager gameManager;
    private Transform target;
    private bool isSubscribed;

    public Transform Target => target;

    private void OnEnable()
    {
        TrySubscribeToGameManager();
    }

    private void Start()
    {
        TrySubscribeToGameManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
        target = null;
    }

    private void TrySubscribeToGameManager()
    {
        if (isSubscribed || GameManager.Instance == null)
            return;

        gameManager = GameManager.Instance;
        gameManager.CurrentPlayerChanged += HandleCurrentPlayerChanged;
        isSubscribed = true;

        HandleCurrentPlayerChanged(gameManager.CurrentPlayer);
    }

    private void UnsubscribeFromGameManager()
    {
        if (!isSubscribed)
            return;

        if (gameManager != null)
            gameManager.CurrentPlayerChanged -= HandleCurrentPlayerChanged;

        gameManager = null;
        isSubscribed = false;
    }

    private void HandleCurrentPlayerChanged(GameObject currentPlayer)
    {
        target = currentPlayer != null ? currentPlayer.transform : null;
    }
}
