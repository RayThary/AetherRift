using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private bool isInitialized;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializeGame();
    }

    private void InitializeGame()
    {
        if (isInitialized)
            return;

        isInitialized = true;

        SceneLoader.Instance.LoadScene(1);
    }
}
