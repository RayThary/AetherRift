using UnityEngine;
using UnityEngine.UI;

public class DungeonSelectionUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool closeOnStart = true;

    [Header("Dungeon")]
    [SerializeField] private DungeonData[] dungeons;
    [SerializeField] private DungeonSelectionSlotUI dungeonSlot;
    [SerializeField] private int battleSceneIndex = 3;

    [Header("Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;

    private int currentDungeonIndex;

    private void Awake()
    {
        EnsurePanelRoot();

        if (previousButton != null)
            previousButton.onClick.AddListener(SelectPreviousDungeon);

        if (nextButton != null)
            nextButton.onClick.AddListener(SelectNextDungeon);

        if (closeOnStart)
            Close();
        else
            Refresh();
    }

    private void OnDestroy()
    {
        if (previousButton != null)
            previousButton.onClick.RemoveListener(SelectPreviousDungeon);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(SelectNextDungeon);
    }

    public void Open()
    {
        EnsurePanelRoot();

        if (panelRoot != null)
            panelRoot.SetActive(true);

        currentDungeonIndex = Mathf.Clamp(currentDungeonIndex, 0, GetLastDungeonIndex());
        Refresh();

        if (GameManager.Instance != null)
            GameManager.Instance.SetGamePaused(GamePauseReason.DungeonSelection, true);
    }

    public void Close()
    {
        EnsurePanelRoot();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.SetGamePaused(GamePauseReason.DungeonSelection, false);
    }

    public void Refresh()
    {
        if (dungeonSlot == null)
            return;

        currentDungeonIndex = Mathf.Clamp(currentDungeonIndex, 0, GetLastDungeonIndex());
        RefreshNavigationButtons();

        if (dungeons == null || dungeons.Length == 0 || dungeons[currentDungeonIndex] == null)
        {
            dungeonSlot.Clear();
            return;
        }

        DungeonData dungeonData = dungeons[currentDungeonIndex];
        bool isUnlocked = GameProgressManager.Instance == null || GameProgressManager.Instance.IsDungeonUnlocked(dungeonData);
        bool isCompleted = GameProgressManager.Instance != null && GameProgressManager.Instance.IsDungeonCompleted(dungeonData);
        string unlockCondition = GetUnlockConditionText(dungeonData);
        dungeonSlot.Set(dungeonData, isUnlocked, isCompleted, unlockCondition, HandleEnterDungeonClicked);
    }

    public void SelectPreviousDungeon()
    {
        if (currentDungeonIndex <= 0)
            return;

        currentDungeonIndex--;
        Refresh();
    }

    public void SelectNextDungeon()
    {
        int lastDungeonIndex = GetLastDungeonIndex();

        if (currentDungeonIndex >= lastDungeonIndex)
            return;

        currentDungeonIndex++;
        Refresh();
    }

    private void HandleEnterDungeonClicked(DungeonData dungeonData)
    {
        if (dungeonData == null || SceneLoader.Instance == null)
            return;

        if (GameProgressManager.Instance != null && !GameProgressManager.Instance.IsDungeonUnlocked(dungeonData))
            return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetSelectedDungeon(dungeonData);
            GameManager.Instance.SetGamePaused(GamePauseReason.DungeonSelection, false);
        }

        SceneLoader.Instance.LoadScene(battleSceneIndex);
    }

    private int GetLastDungeonIndex()
    {
        if (dungeons == null || dungeons.Length == 0)
            return 0;

        return dungeons.Length - 1;
    }

    private void RefreshNavigationButtons()
    {
        int lastDungeonIndex = GetLastDungeonIndex();

        if (previousButton != null)
            previousButton.interactable = dungeons != null && dungeons.Length > 0 && currentDungeonIndex > 0;

        if (nextButton != null)
            nextButton.interactable = dungeons != null && dungeons.Length > 0 && currentDungeonIndex < lastDungeonIndex;
    }

    private void EnsurePanelRoot()
    {
        if (panelRoot == null)
            panelRoot = gameObject;
    }

    private string GetUnlockConditionText(DungeonData dungeonData)
    {
        if (dungeonData == null || dungeonData.UnlockedByDefault || string.IsNullOrWhiteSpace(dungeonData.RequiredCompletedDungeonId))
            return "해금 조건: 없음";

        string requiredDungeonName = GetDungeonDisplayNameById(dungeonData.RequiredCompletedDungeonId);
        return $"해금 조건: {requiredDungeonName} 클리어";
    }

    private string GetDungeonDisplayNameById(string dungeonId)
    {
        if (dungeons != null)
        {
            for (int i = 0; i < dungeons.Length; i++)
            {
                DungeonData dungeonData = dungeons[i];

                if (dungeonData == null || dungeonData.DungeonId != dungeonId)
                    continue;

                return string.IsNullOrWhiteSpace(dungeonData.DisplayName) ? dungeonData.DungeonId : dungeonData.DisplayName;
            }
        }

        return dungeonId;
    }
}
