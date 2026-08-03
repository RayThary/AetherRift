using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private bool isInitialized;

    public GameObject CurrentPlayer { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        FindCurrentPlayer();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        InitializeGame();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        FindCurrentPlayer();
    }

    private void FindCurrentPlayer()
    {
        PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
        CurrentPlayer = playerCore != null ? playerCore.gameObject : null;
    }

    private void InitializeGame()
    {
        if (isInitialized || SceneManager.GetActiveScene().buildIndex != 0)
            return;

        isInitialized = true;

        SceneLoader.Instance.LoadScene(1);
    }
}
