using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    private List<RelicData> relicDatabase = new List<RelicData>();
    private List<QuestData> questDatabase = new List<QuestData>();
    private List<DungeonData> dungeonDatabase = new List<DungeonData>();

    private GameProgressData currentData;

    public event Action ProgressChanged;

    public GameProgressData CurrentData => currentData;
    public int Currency => currentData?.currency ?? 0;
    public bool FinalBossCleared => currentData != null && currentData.finalBossCleared;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        relicDatabase = new List<RelicData>(Resources.LoadAll<RelicData>("Data/Relics"));
        questDatabase = new List<QuestData>(Resources.LoadAll<QuestData>("Data/Quests"));
        dungeonDatabase = new List<DungeonData>(Resources.LoadAll<DungeonData>("Data/Dungeons"));

        CreateNewProgress();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void CreateNewProgress()
    {
        currentData = new GameProgressData();
        UnlockAvailableDungeons();
        NotifyProgressChanged();
    }

    public void ApplyLoadedProgress(GameProgressData loadedData)
    {
        currentData = loadedData ?? new GameProgressData();
        currentData.EnsureValid();
        currentData.currency = Mathf.Max(0, currentData.currency);
        UnlockAvailableDungeons();
        NotifyProgressChanged();
    }

    public void AddCurrency(int amount)
    {
        if (amount <= 0)
            return;

        EnsureCurrentData();
        currentData.currency += amount;
        NotifyProgressChanged();
    }

    public bool TrySpendCurrency(int amount)
    {
        if (amount < 0)
            return false;

        EnsureCurrentData();

        if (currentData.currency < amount)
            return false;

        if (amount == 0)
            return true;

        currentData.currency -= amount;
        NotifyProgressChanged();
        return true;
    }

    public bool HasRelic(RelicData relicData)
    {
        return relicData != null && HasRelic(relicData.RelicId);
    }

    public bool HasRelic(string relicId)
    {
        EnsureCurrentData();
        return ContainsId(currentData.ownedRelicIds, relicId);
    }

    public bool AddRelic(RelicData relicData)
    {
        if (!TryGetValidId(relicData, out string relicId) || HasRelic(relicId))
            return false;

        currentData.ownedRelicIds.Add(relicId);
        NotifyProgressChanged();
        return true;
    }

    public bool TryPurchaseRelic(RelicData relicData)
    {
        if (!TryGetValidId(relicData, out string relicId) || HasRelic(relicId))
            return false;

        if (currentData.currency < relicData.Price)
            return false;

        currentData.currency -= relicData.Price;
        currentData.ownedRelicIds.Add(relicId);
        NotifyProgressChanged();
        return true;
    }

    public bool TryEquipRelic(RelicData relicData, int slotIndex)
    {
        if (!IsValidRelicSlot(slotIndex) || !TryGetValidId(relicData, out string relicId) || !HasRelic(relicId))
            return false;

        if (IdsMatch(currentData.equippedRelicIds[slotIndex], relicId))
            return true;

        for (int i = 0; i < currentData.equippedRelicIds.Count; i++)
        {
            if (IdsMatch(currentData.equippedRelicIds[i], relicId))
                currentData.equippedRelicIds[i] = string.Empty;
        }

        currentData.equippedRelicIds[slotIndex] = relicId;
        NotifyProgressChanged();
        return true;
    }

    public bool UnequipRelic(int slotIndex)
    {
        if (!IsValidRelicSlot(slotIndex) || string.IsNullOrEmpty(currentData.equippedRelicIds[slotIndex]))
            return false;

        currentData.equippedRelicIds[slotIndex] = string.Empty;
        NotifyProgressChanged();
        return true;
    }

    public string GetEquippedRelicId(int slotIndex)
    {
        if (!IsValidRelicSlot(slotIndex))
            return string.Empty;

        return currentData.equippedRelicIds[slotIndex];
    }

    public RelicData GetEquippedRelic(int slotIndex)
    {
        return FindRelic(GetEquippedRelicId(slotIndex));
    }

    public QuestProgressState GetQuestState(QuestData questData)
    {
        QuestProgressData questProgress = FindQuestProgress(questData);
        return questProgress?.state ?? QuestProgressState.NotAccepted;
    }

    public int GetQuestCurrentAmount(QuestData questData)
    {
        QuestProgressData questProgress = FindQuestProgress(questData);
        return questProgress?.currentAmount ?? 0;
    }

    public bool TryAcceptQuest(QuestData questData)
    {
        if (!TryGetValidId(questData, out string questId))
            return false;

        QuestProgressData questProgress = FindQuestProgress(questId);

        if (questProgress == null)
        {
            questProgress = new QuestProgressData(questId);
            currentData.questProgressList.Add(questProgress);
        }

        if (questProgress.state != QuestProgressState.NotAccepted)
            return false;

        questProgress.state = QuestProgressState.InProgress;
        questProgress.currentAmount = 0;
        NotifyProgressChanged();
        return true;
    }

    public int ReportQuestProgress(QuestObjectiveType objectiveType, string targetId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(targetId) || amount <= 0)
            return 0;

        EnsureCurrentData();
        int updatedQuestCount = ApplyQuestProgress(objectiveType, targetId.Trim(), amount);

        if (updatedQuestCount > 0)
            NotifyProgressChanged();

        return updatedQuestCount;
    }

    public bool TryCompleteQuest(QuestData questData)
    {
        if (!TryGetValidId(questData, out string questId))
            return false;

        QuestProgressData questProgress = FindQuestProgress(questId);

        if (questProgress == null || questProgress.state != QuestProgressState.ReadyToComplete)
            return false;

        questProgress.state = QuestProgressState.Completed;
        currentData.currency += questData.CurrencyReward;

        if (TryGetValidId(questData.RelicReward, out string relicRewardId) && !HasRelic(relicRewardId))
            currentData.ownedRelicIds.Add(relicRewardId);

        AddNextQuestProgress(questData.NextQuest);
        NotifyProgressChanged();
        return true;
    }

    public bool IsDungeonUnlocked(DungeonData dungeonData)
    {
        return dungeonData != null && IsDungeonUnlocked(dungeonData.DungeonId);
    }

    public bool IsDungeonUnlocked(string dungeonId)
    {
        EnsureCurrentData();
        return ContainsId(currentData.unlockedDungeonIds, dungeonId);
    }

    public bool IsDungeonCompleted(DungeonData dungeonData)
    {
        return dungeonData != null && IsDungeonCompleted(dungeonData.DungeonId);
    }

    public bool IsDungeonCompleted(string dungeonId)
    {
        EnsureCurrentData();
        return ContainsId(currentData.completedDungeonIds, dungeonId);
    }

    public bool UnlockDungeon(DungeonData dungeonData)
    {
        if (!TryGetValidId(dungeonData, out string dungeonId) || IsDungeonUnlocked(dungeonId))
            return false;

        currentData.unlockedDungeonIds.Add(dungeonId);
        NotifyProgressChanged();
        return true;
    }

    public bool CompleteDungeon(DungeonData dungeonData)
    {
        if (!TryGetValidId(dungeonData, out string dungeonId))
            return false;

        if (!IsDungeonUnlocked(dungeonId))
            currentData.unlockedDungeonIds.Add(dungeonId);

        if (!IsDungeonCompleted(dungeonId))
            currentData.completedDungeonIds.Add(dungeonId);

        currentData.currency += dungeonData.CurrencyReward;

        if (dungeonData.FinalBossDungeon)
            currentData.finalBossCleared = true;

        ApplyQuestProgress(QuestObjectiveType.ClearDungeon, dungeonId, 1);
        UnlockAvailableDungeons();
        NotifyProgressChanged();
        return true;
    }

    public RelicData FindRelic(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return null;

        for (int i = 0; i < relicDatabase.Count; i++)
        {
            RelicData relicData = relicDatabase[i];

            if (relicData != null && IdsMatch(relicData.RelicId, relicId))
                return relicData;
        }

        return null;
    }

    public QuestData FindQuest(string questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
            return null;

        for (int i = 0; i < questDatabase.Count; i++)
        {
            QuestData questData = questDatabase[i];

            if (questData != null && IdsMatch(questData.QuestId, questId))
                return questData;
        }

        return null;
    }

    public DungeonData FindDungeon(string dungeonId)
    {
        if (string.IsNullOrWhiteSpace(dungeonId))
            return null;

        for (int i = 0; i < dungeonDatabase.Count; i++)
        {
            DungeonData dungeonData = dungeonDatabase[i];

            if (dungeonData != null && IdsMatch(dungeonData.DungeonId, dungeonId))
                return dungeonData;
        }

        return null;
    }

    private int ApplyQuestProgress(QuestObjectiveType objectiveType, string targetId, int amount)
    {
        int updatedQuestCount = 0;

        for (int i = 0; i < currentData.questProgressList.Count; i++)
        {
            QuestProgressData questProgress = currentData.questProgressList[i];

            if (questProgress == null || questProgress.state != QuestProgressState.InProgress)
                continue;

            QuestData questData = FindQuest(questProgress.questId);

            if (questData == null || questData.ObjectiveType != objectiveType || !IdsMatch(questData.TargetId, targetId))
                continue;

            questProgress.currentAmount = Mathf.Min(questProgress.currentAmount + amount, questData.RequiredAmount);

            if (questProgress.currentAmount >= questData.RequiredAmount)
                questProgress.state = QuestProgressState.ReadyToComplete;

            updatedQuestCount++;
        }

        return updatedQuestCount;
    }

    private void AddNextQuestProgress(QuestData nextQuest)
    {
        if (!TryGetValidId(nextQuest, out string nextQuestId) || FindQuestProgress(nextQuestId) != null)
            return;

        currentData.questProgressList.Add(new QuestProgressData(nextQuestId));
    }

    private QuestProgressData FindQuestProgress(QuestData questData)
    {
        if (!TryGetValidId(questData, out string questId))
            return null;

        return FindQuestProgress(questId);
    }

    private QuestProgressData FindQuestProgress(string questId)
    {
        EnsureCurrentData();

        for (int i = 0; i < currentData.questProgressList.Count; i++)
        {
            QuestProgressData questProgress = currentData.questProgressList[i];

            if (questProgress != null && IdsMatch(questProgress.questId, questId))
                return questProgress;
        }

        return null;
    }

    private void UnlockAvailableDungeons()
    {
        EnsureCurrentData();

        for (int i = 0; i < dungeonDatabase.Count; i++)
        {
            DungeonData dungeonData = dungeonDatabase[i];

            if (dungeonData == null || string.IsNullOrWhiteSpace(dungeonData.DungeonId) || IsDungeonUnlocked(dungeonData.DungeonId))
                continue;

            bool canUnlock = dungeonData.UnlockedByDefault;

            if (!canUnlock && !string.IsNullOrWhiteSpace(dungeonData.RequiredCompletedDungeonId))
                canUnlock = IsDungeonCompleted(dungeonData.RequiredCompletedDungeonId);

            if (canUnlock)
                currentData.unlockedDungeonIds.Add(dungeonData.DungeonId);
        }
    }

    private bool IsValidRelicSlot(int slotIndex)
    {
        EnsureCurrentData();
        return slotIndex >= 0 && slotIndex < GameProgressData.RelicSlotCount;
    }

    private void EnsureCurrentData()
    {
        if (currentData == null)
            currentData = new GameProgressData();

        currentData.EnsureValid();
    }

    private void NotifyProgressChanged()
    {
        ProgressChanged?.Invoke();
    }

    private static bool ContainsId(List<string> idList, string id)
    {
        if (idList == null || string.IsNullOrWhiteSpace(id))
            return false;

        for (int i = 0; i < idList.Count; i++)
        {
            if (IdsMatch(idList[i], id))
                return true;
        }

        return false;
    }

    private static bool IdsMatch(string firstId, string secondId)
    {
        return string.Equals(firstId, secondId, StringComparison.Ordinal);
    }

    private static bool TryGetValidId(RelicData relicData, out string relicId)
    {
        relicId = relicData != null ? relicData.RelicId : string.Empty;
        return !string.IsNullOrWhiteSpace(relicId);
    }

    private static bool TryGetValidId(QuestData questData, out string questId)
    {
        questId = questData != null ? questData.QuestId : string.Empty;
        return !string.IsNullOrWhiteSpace(questId);
    }

    private static bool TryGetValidId(DungeonData dungeonData, out string dungeonId)
    {
        dungeonId = dungeonData != null ? dungeonData.DungeonId : string.Empty;
        return !string.IsNullOrWhiteSpace(dungeonId);
    }
}