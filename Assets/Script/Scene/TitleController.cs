using UnityEngine;
using UnityEngine.UI;

public class TitleController : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject newGameConfirmPanel;

    private const int LobbySceneIndex = 2;

    private void Start()
    {
        if (newGameConfirmPanel != null)
            newGameConfirmPanel.SetActive(false);

        RefreshContinueButton();
    }

    public void StartGame()
    {
        StartNewGame();
    }

    public void StartNewGame()
    {
        if (!TryGetSaveManager(out SaveManager saveManager))
            return;

        if (saveManager.HasSaveData && newGameConfirmPanel != null)
        {
            newGameConfirmPanel.SetActive(true);
            return;
        }

        ConfirmStartNewGame();
    }

    public void ConfirmStartNewGame()
    {
        if (!TryGetSaveManager(out SaveManager saveManager) || !saveManager.CreateNewGame())
            return;

        if (newGameConfirmPanel != null)
            newGameConfirmPanel.SetActive(false);

        LoadLobbyScene();
    }

    public void CancelStartNewGame()
    {
        if (newGameConfirmPanel != null)
            newGameConfirmPanel.SetActive(false);
    }

    public void ContinueGame()
    {
        if (!TryGetSaveManager(out SaveManager saveManager) || !saveManager.LoadGame())
            return;

        LoadLobbyScene();
    }

    public void RefreshContinueButton()
    {
        if (continueButton != null)
            continueButton.interactable = SaveManager.Instance != null && SaveManager.Instance.HasSaveData;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    private void LoadLobbyScene()
    {
        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[TitleController] SceneLoader를 찾지 못했습니다.", this);
            return;
        }

        SceneLoader.Instance.LoadScene(LobbySceneIndex);
    }

    private bool TryGetSaveManager(out SaveManager saveManager)
    {
        saveManager = SaveManager.Instance;

        if (saveManager != null)
            return true;

        Debug.LogError("[TitleController] SaveManager를 찾지 못했습니다.", this);
        return false;
    }
}
