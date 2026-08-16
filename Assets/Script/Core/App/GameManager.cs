using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GamePauseReason
{
    Pause,
    Inventory,
    DungeonSelection,
    Dialogue,
    Cutscene
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Pause")]
    [SerializeField] private bool lockCursorWhenUnpaused = true;

    private readonly HashSet<GamePauseReason> pauseReasons = new HashSet<GamePauseReason>();
    private bool isInitialized;

    public GameObject CurrentPlayer { get; private set; }
    public DungeonData SelectedDungeonData { get; private set; }
    public string SelectedDungeonId => SelectedDungeonData != null ? SelectedDungeonData.DungeonId : string.Empty;
    public bool IsGamePaused => pauseReasons.Count > 0;

    public event Action<GameObject> CurrentPlayerChanged;
    public event Action<bool> GamePauseChanged;

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

        RefreshCurrentPlayer();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }

    private void Start()
    {
        InitializeGame();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        RefreshCurrentPlayer();
    }

    private void RefreshCurrentPlayer()
    {
        PlayerCore playerCore = FindFirstObjectByType<PlayerCore>();
        CurrentPlayer = playerCore != null ? playerCore.gameObject : null;
        ApplyPlayerPauseState();
        CurrentPlayerChanged?.Invoke(CurrentPlayer);
    }

    public void SetGamePaused(bool isPaused)
    {
        SetGamePaused(GamePauseReason.Pause, isPaused);
    }

    public void SetGamePaused(GamePauseReason reason, bool isPaused)
    {
        bool wasPaused = IsGamePaused;

        if (isPaused)
            pauseReasons.Add(reason);
        else
            pauseReasons.Remove(reason);

        bool isNowPaused = IsGamePaused;

        if (wasPaused == isNowPaused)
            return;

        ApplyPauseState();
        GamePauseChanged?.Invoke(isNowPaused);
    }

    public void ClearAllPauseReasons()
    {
        if (pauseReasons.Count == 0)
            return;

        pauseReasons.Clear();
        ApplyPauseState();
        GamePauseChanged?.Invoke(false);
    }

    public void SetSelectedDungeon(DungeonData dungeonData)
    {
        SelectedDungeonData = dungeonData;
    }

    public void ClearSelectedDungeon()
    {
        SelectedDungeonData = null;
    }

    private void ApplyPauseState()
    {
        Time.timeScale = IsGamePaused ? 0f : 1f;
        ApplyPlayerPauseState();
        ApplyCursorState();
    }

    private void ApplyPlayerPauseState()
    {
        if (CurrentPlayer == null)
            return;

        PlayerCore playerCore = CurrentPlayer.GetComponent<PlayerCore>();

        if (playerCore != null)
            playerCore.SetPlayerPaused(IsGamePaused);
    }

    private void ApplyCursorState()
    {
        if (IsGamePaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (!lockCursorWhenUnpaused)
            return;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void InitializeGame()
    {
        if (isInitialized || SceneManager.GetActiveScene().buildIndex != 0)
            return;

        isInitialized = true;

        SceneLoader.Instance.LoadScene(1);
    }
}
