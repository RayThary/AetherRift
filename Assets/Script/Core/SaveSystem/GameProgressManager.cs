using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameProgressManager : MonoBehaviour
{
    public static GameProgressManager Instance { get; private set; }

    private const int CurrentSaveVersion = 4;

    private List<QuestData> questDatabase = new List<QuestData>();
    private List<DungeonData> dungeonDatabase = new List<DungeonData>();

    private GameProgressData currentData;

    public event Action ProgressChanged;

    public GameProgressData CurrentData => currentData;
    public int Currency => currentData?.currency ?? 0;
    public int CurrentHealth => currentData?.currentHealth ?? -1;
    public bool FinalBossCleared => currentData != null && currentData.finalBossCleared;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

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

    public bool ApplyLoadedProgress(GameProgressData loadedData)
    {
        currentData = loadedData ?? new GameProgressData();
        currentData.EnsureValid();

        bool relicDataMigrated = MigrateRelicData();

        currentData.currency = Mathf.Max(0, currentData.currency);
        UnlockAvailableDungeons();
        NotifyProgressChanged();
        return relicDataMigrated;
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

    public void AddMaxHealthBonus(int amount)
    {
        if (amount <= 0)
            return;

        EnsureCurrentData();
        currentData.maxHealthBonus += amount;
        NotifyProgressChanged();
    }

    public void AddAttackPowerBonus(int amount)
    {
        if (amount <= 0)
            return;

        EnsureCurrentData();
        currentData.attackPowerBonus += amount;
        NotifyProgressChanged();
    }

    public void AddMoveSpeedBonus(float amount)
    {
        if (amount <= 0f)
            return;

        EnsureCurrentData();
        currentData.moveSpeedBonus += amount;
        NotifyProgressChanged();
    }

    public void SetCurrentHealth(int health)
    {
        EnsureCurrentData();
        currentData.currentHealth = Mathf.Max(health, 0);
    }

    public IReadOnlyList<RelicInventoryData> GetOwnedRelicGroups()
    {
        EnsureCurrentData();
        return currentData.ownedRelicGroups;
    }

    public IReadOnlyList<RelicInstanceData> GetOwnedRelics(string relicId)
    {
        RelicInventoryData relicGroup = FindOwnedRelicGroup(relicId);
        return relicGroup != null ? relicGroup.Instances : Array.Empty<RelicInstanceData>();
    }

    public bool HasRelic(RelicData relicData)
    {
        return relicData != null && HasRelic(relicData.RelicId);
    }

    public bool HasRelic(string relicId)
    {
        return FindOwnedRelicInstanceByRelicId(relicId) != null;
    }

    public bool HasRelicInstance(string relicInstanceId)
    {
        return FindRelicInstance(relicInstanceId) != null;
    }

    public RelicInstanceData CreateRelicInstance(RelicData relicData)
    {
        if (RelicManager.Instance == null)
        {
            Debug.LogError("[GameProgressManager] RelicManager를 찾지 못했습니다.", this);
            return null;
        }

        return RelicManager.Instance.CreateRelicInstance(relicData);
    }

    public bool AddRelic(RelicData relicData)
    {
        RelicInstanceData relicInstance = CreateRelicInstance(relicData);
        return AddRelicInstance(relicInstance);
    }

    public bool AddRelicInstance(RelicInstanceData relicInstance)
    {
        if (!TryAddRelicInstance(relicInstance))
            return false;

        NotifyProgressChanged();
        return true;
    }

    public bool TryPurchaseRelic(RelicInstanceData relicInstance)
    {
        if (!IsRelicInstanceValid(relicInstance))
            return false;

        EnsureCurrentData();

        if (currentData.currency < relicInstance.Price || HasRelicInstance(relicInstance.InstanceId))
            return false;

        currentData.currency -= relicInstance.Price;
        TryAddRelicInstance(relicInstance);
        NotifyProgressChanged();
        return true;
    }

    public bool TryEquipRelic(RelicInstanceData relicInstance, int slotIndex)
    {
        return relicInstance != null && TryEquipRelic(relicInstance.InstanceId, slotIndex);
    }

    public bool TryEquipRelic(string relicInstanceId, int slotIndex)
    {
        if (!IsValidRelicSlot(slotIndex))
            return false;

        RelicInstanceData relicInstance = FindRelicInstance(relicInstanceId);

        if (relicInstance == null)
            return false;

        if (IdsMatch(currentData.equippedRelicInstanceIds[slotIndex], relicInstance.InstanceId))
            return true;

        if (IsRelicTypeEquipped(relicInstance.RelicId, slotIndex))
            return false;

        currentData.equippedRelicInstanceIds[slotIndex] = relicInstance.InstanceId;
        NotifyProgressChanged();
        return true;
    }

    public bool UnequipRelic(int slotIndex)
    {
        if (!IsValidRelicSlot(slotIndex) || string.IsNullOrEmpty(currentData.equippedRelicInstanceIds[slotIndex]))
            return false;

        currentData.equippedRelicInstanceIds[slotIndex] = string.Empty;
        NotifyProgressChanged();
        return true;
    }

    public string GetEquippedRelicInstanceId(int slotIndex)
    {
        if (!IsValidRelicSlot(slotIndex))
            return string.Empty;

        return currentData.equippedRelicInstanceIds[slotIndex];
    }

    public RelicInstanceData GetEquippedRelic(int slotIndex)
    {
        return FindRelicInstance(GetEquippedRelicInstanceId(slotIndex));
    }

    public bool TryRemoveRelic(string relicInstanceId)
    {
        if (string.IsNullOrWhiteSpace(relicInstanceId))
            return false;

        EnsureCurrentData();

        for (int groupIndex = 0; groupIndex < currentData.ownedRelicGroups.Count; groupIndex++)
        {
            RelicInventoryData relicGroup = currentData.ownedRelicGroups[groupIndex];

            if (relicGroup == null || relicGroup.instances == null)
                continue;

            for (int instanceIndex = 0; instanceIndex < relicGroup.instances.Count; instanceIndex++)
            {
                RelicInstanceData relicInstance = relicGroup.instances[instanceIndex];

                if (relicInstance == null || !IdsMatch(relicInstance.InstanceId, relicInstanceId))
                    continue;

                UnequipRelicInstance(relicInstance.InstanceId);
                relicGroup.instances.RemoveAt(instanceIndex);

                if (relicGroup.instances.Count == 0)
                    currentData.ownedRelicGroups.RemoveAt(groupIndex);

                NotifyProgressChanged();
                return true;
            }
        }

        return false;
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

        RelicInstanceData relicReward = CreateRelicInstance(questData.RelicReward);

        if (relicReward != null)
            TryAddRelicInstance(relicReward);

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
        return RelicManager.Instance != null ? RelicManager.Instance.FindRelic(relicId) : null;
    }

    public RelicInstanceData FindRelicInstance(string relicInstanceId)
    {
        if (string.IsNullOrWhiteSpace(relicInstanceId))
            return null;

        EnsureCurrentData();

        for (int groupIndex = 0; groupIndex < currentData.ownedRelicGroups.Count; groupIndex++)
        {
            RelicInventoryData relicGroup = currentData.ownedRelicGroups[groupIndex];

            if (relicGroup == null || relicGroup.instances == null)
                continue;

            for (int instanceIndex = 0; instanceIndex < relicGroup.instances.Count; instanceIndex++)
            {
                RelicInstanceData relicInstance = relicGroup.instances[instanceIndex];

                if (relicInstance != null && IdsMatch(relicInstance.InstanceId, relicInstanceId))
                    return relicInstance;
            }
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

    private bool MigrateRelicData()
    {
        bool relicDataMigrated = false;

        if (currentData.saveVersion < 3)
        {
            for (int i = 0; i < currentData.ownedRelicIds.Count; i++)
            {
                RelicData relicData = FindRelic(currentData.ownedRelicIds[i]);
                RelicInstanceData relicInstance = CreateRelicInstance(relicData);

                if (relicInstance != null)
                    TryAddRelicInstance(relicInstance);
            }

            HashSet<string> equippedInstanceIds = new HashSet<string>();

            for (int i = 0; i < GameProgressData.RelicSlotCount; i++)
            {
                string legacyRelicId = currentData.equippedRelicIds[i];
                RelicInstanceData relicInstance = FindOwnedRelicInstanceByRelicId(legacyRelicId, equippedInstanceIds);

                if (relicInstance == null)
                {
                    RelicData relicData = FindRelic(legacyRelicId);
                    relicInstance = CreateRelicInstance(relicData);

                    if (relicInstance != null)
                        TryAddRelicInstance(relicInstance);
                }

                currentData.equippedRelicInstanceIds[i] = relicInstance != null ? relicInstance.InstanceId : string.Empty;

                if (relicInstance != null)
                    equippedInstanceIds.Add(relicInstance.InstanceId);
            }

            currentData.ownedRelicIds.Clear();
            currentData.equippedRelicIds.Clear();
            currentData.saveVersion = 3;
            relicDataMigrated = true;
        }

        if (currentData.saveVersion < CurrentSaveVersion)
        {
            for (int i = 0; i < currentData.ownedRelics.Count; i++)
                TryAddRelicInstance(currentData.ownedRelics[i]);

            currentData.ownedRelics.Clear();
            currentData.saveVersion = CurrentSaveVersion;
            relicDataMigrated = true;
        }

        currentData.EnsureValid();
        return relicDataMigrated;
    }

    private bool TryAddRelicInstance(RelicInstanceData relicInstance)
    {
        if (!IsRelicInstanceValid(relicInstance) || HasRelicInstance(relicInstance.InstanceId))
            return false;

        RelicInventoryData relicGroup = FindOwnedRelicGroup(relicInstance.RelicId);

        if (relicGroup == null)
        {
            relicGroup = new RelicInventoryData(relicInstance.RelicId);
            currentData.ownedRelicGroups.Add(relicGroup);
        }

        relicGroup.instances.Add(relicInstance);
        return true;
    }

    private bool IsRelicInstanceValid(RelicInstanceData relicInstance)
    {
        if (relicInstance == null)
            return false;

        relicInstance.EnsureValid();

        return !string.IsNullOrWhiteSpace(relicInstance.InstanceId)
            && FindRelic(relicInstance.RelicId) != null
            && relicInstance.Effects.Count > 0;
    }

    private RelicInstanceData FindOwnedRelicInstanceByRelicId(string relicId, HashSet<string> excludedInstanceIds = null)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return null;

        EnsureCurrentData();

        RelicInventoryData relicGroup = FindOwnedRelicGroup(relicId);

        if (relicGroup == null || relicGroup.instances == null)
            return null;

        for (int i = 0; i < relicGroup.instances.Count; i++)
        {
            RelicInstanceData relicInstance = relicGroup.instances[i];

            if (relicInstance == null || !IdsMatch(relicInstance.RelicId, relicId))
                continue;

            if (excludedInstanceIds == null || !excludedInstanceIds.Contains(relicInstance.InstanceId))
                return relicInstance;
        }

        return null;
    }

    private RelicInventoryData FindOwnedRelicGroup(string relicId)
    {
        if (string.IsNullOrWhiteSpace(relicId))
            return null;

        EnsureCurrentData();

        for (int i = 0; i < currentData.ownedRelicGroups.Count; i++)
        {
            RelicInventoryData relicGroup = currentData.ownedRelicGroups[i];

            if (relicGroup != null && IdsMatch(relicGroup.RelicId, relicId))
                return relicGroup;
        }

        return null;
    }

    private void UnequipRelicInstance(string relicInstanceId)
    {
        for (int i = 0; i < currentData.equippedRelicInstanceIds.Count; i++)
        {
            if (IdsMatch(currentData.equippedRelicInstanceIds[i], relicInstanceId))
                currentData.equippedRelicInstanceIds[i] = string.Empty;
        }
    }

    private bool IsRelicTypeEquipped(string relicId, int ignoredSlotIndex)
    {
        for (int i = 0; i < currentData.equippedRelicInstanceIds.Count; i++)
        {
            if (i == ignoredSlotIndex)
                continue;

            RelicInstanceData equippedRelic = FindRelicInstance(currentData.equippedRelicInstanceIds[i]);

            if (equippedRelic != null && IdsMatch(equippedRelic.RelicId, relicId))
                return true;
        }

        return false;
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
