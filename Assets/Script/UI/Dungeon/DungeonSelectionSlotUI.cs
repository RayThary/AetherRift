using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DungeonSelectionSlotUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text difficultyText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private TMP_Text unlockConditionText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TMP_Text actionButtonText;

    private DungeonData currentDungeonData;
    private Action<DungeonData> clickCallback;

    private void Awake()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(HandleButtonClicked);
    }

    private void OnDestroy()
    {
        if (actionButton != null)
            actionButton.onClick.RemoveListener(HandleButtonClicked);
    }

    public void Set(DungeonData dungeonData, bool isUnlocked, bool isCompleted, string unlockCondition, Action<DungeonData> onClick)
    {
        currentDungeonData = dungeonData;
        clickCallback = onClick;

        if (nameText != null)
            nameText.text = GetDisplayName(dungeonData);

        if (difficultyText != null)
            difficultyText.text = $"난이도: {dungeonData.DifficultyText}";

        if (rewardText != null)
            rewardText.text = $"보상: {dungeonData.CurrencyReward} 골드";

        if (stateText != null)
            stateText.text = isUnlocked ? (isCompleted ? "상태: 클리어" : "상태: 입장 가능") : "상태: 잠김";

        if (unlockConditionText != null)
            unlockConditionText.text = unlockCondition;

        if (actionButtonText != null)
            actionButtonText.text = isUnlocked ? "입장" : "잠김";

        if (actionButton != null)
            actionButton.interactable = isUnlocked;

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        currentDungeonData = null;
        clickCallback = null;
        gameObject.SetActive(false);
    }

    private void HandleButtonClicked()
    {
        if (currentDungeonData == null)
            return;

        clickCallback?.Invoke(currentDungeonData);
    }

    private string GetDisplayName(DungeonData dungeonData)
    {
        if (dungeonData == null)
            return string.Empty;

        return string.IsNullOrWhiteSpace(dungeonData.DisplayName) ? dungeonData.DungeonId : dungeonData.DisplayName;
    }
}
