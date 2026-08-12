using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RelicManager : MonoBehaviour
{
    public static RelicManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private RelicDatabase relicDatabase;

    private readonly List<RelicInstanceData> currentRewardOptions = new List<RelicInstanceData>();

    public event Action RewardOptionsChanged;

    public IReadOnlyList<RelicInstanceData> CurrentRewardOptions => currentRewardOptions;
    public IReadOnlyList<RelicData> AllRelics => relicDatabase != null ? relicDatabase.Relics : Array.Empty<RelicData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (relicDatabase == null)
            Debug.LogError("[RelicManager] RelicDatabase가 연결되지 않았습니다.", this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public RelicData FindRelic(string relicId)
    {
        return relicDatabase != null ? relicDatabase.FindRelic(relicId) : null;
    }

    public RelicData GetRelicData(RelicInstanceData relicInstance)
    {
        return relicInstance != null ? FindRelic(relicInstance.RelicId) : null;
    }

    public RelicInstanceData CreateRelicInstance(RelicData relicData)
    {
        if (relicData == null || string.IsNullOrWhiteSpace(relicData.RelicId))
            return null;

        RelicData registeredRelic = FindRelic(relicData.RelicId);
        return registeredRelic != null ? registeredRelic.CreateInstance() : null;
    }

    public RelicInstanceData CreateRelicInstance(string relicId)
    {
        RelicData relicData = FindRelic(relicId);
        return relicData != null ? relicData.CreateInstance() : null;
    }

    public IReadOnlyList<RelicInstanceData> CreateRewardOptions(int count)
    {
        currentRewardOptions.Clear();

        if (relicDatabase == null || count <= 0)
        {
            NotifyRewardOptionsChanged();
            return currentRewardOptions;
        }

        IReadOnlyList<RelicData> selectedRelics = relicDatabase.GetRandomRelics(count);

        for (int i = 0; i < selectedRelics.Count; i++)
        {
            RelicInstanceData relicInstance = CreateRelicInstance(selectedRelics[i]);

            if (relicInstance != null)
                currentRewardOptions.Add(relicInstance);
        }

        NotifyRewardOptionsChanged();
        return currentRewardOptions;
    }

    public bool ClaimReward(RelicInstanceData rewardOption)
    {
        return rewardOption != null && ClaimReward(rewardOption.InstanceId);
    }

    public bool ClaimReward(string rewardInstanceId)
    {
        RelicInstanceData rewardOption = FindCurrentRewardOption(rewardInstanceId);

        if (rewardOption == null || !TryGetProgressManager(out GameProgressManager progressManager))
            return false;

        if (!progressManager.AddRelicInstance(rewardOption))
            return false;

        currentRewardOptions.Clear();
        NotifyRewardOptionsChanged();
        return true;
    }

    public void ClearRewardOptions()
    {
        if (currentRewardOptions.Count == 0)
            return;

        currentRewardOptions.Clear();
        NotifyRewardOptionsChanged();
    }

    public IReadOnlyList<RelicInventoryData> GetOwnedRelicGroups()
    {
        return TryGetProgressManager(out GameProgressManager progressManager)
            ? progressManager.GetOwnedRelicGroups()
            : Array.Empty<RelicInventoryData>();
    }

    public IReadOnlyList<RelicInstanceData> GetOwnedRelics(string relicId)
    {
        return TryGetProgressManager(out GameProgressManager progressManager)
            ? progressManager.GetOwnedRelics(relicId)
            : Array.Empty<RelicInstanceData>();
    }

    public bool TryEquipRelic(RelicInstanceData relicInstance, int slotIndex)
    {
        return relicInstance != null && TryEquipRelic(relicInstance.InstanceId, slotIndex);
    }

    public bool TryEquipRelic(string relicInstanceId, int slotIndex)
    {
        return TryGetProgressManager(out GameProgressManager progressManager)
            && progressManager.TryEquipRelic(relicInstanceId, slotIndex);
    }

    public bool UnequipRelic(int slotIndex)
    {
        return TryGetProgressManager(out GameProgressManager progressManager)
            && progressManager.UnequipRelic(slotIndex);
    }

    public RelicInstanceData GetEquippedRelic(int slotIndex)
    {
        return TryGetProgressManager(out GameProgressManager progressManager)
            ? progressManager.GetEquippedRelic(slotIndex)
            : null;
    }

    public bool TryRemoveRelic(string relicInstanceId)
    {
        return TryGetProgressManager(out GameProgressManager progressManager)
            && progressManager.TryRemoveRelic(relicInstanceId);
    }

    private RelicInstanceData FindCurrentRewardOption(string rewardInstanceId)
    {
        if (string.IsNullOrWhiteSpace(rewardInstanceId))
            return null;

        for (int i = 0; i < currentRewardOptions.Count; i++)
        {
            RelicInstanceData rewardOption = currentRewardOptions[i];

            if (rewardOption != null && string.Equals(rewardOption.InstanceId, rewardInstanceId, StringComparison.Ordinal))
                return rewardOption;
        }

        return null;
    }

    private bool TryGetProgressManager(out GameProgressManager progressManager)
    {
        progressManager = GameProgressManager.Instance;

        if (progressManager != null)
            return true;

        Debug.LogError("[RelicManager] GameProgressManager를 찾지 못했습니다.", this);
        return false;
    }

    private void NotifyRewardOptionsChanged()
    {
        RewardOptionsChanged?.Invoke();
    }
}
