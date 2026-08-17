using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum RelicInventoryCategory
{
    All,
    AttackPower,
    MaxHealth,
    MoveSpeed,
    DamageReduction
}

public class RelicInventoryUI : MonoBehaviour
{
    [Header("Inventory Slots")]
    [SerializeField] private RelicInventorySlotUI[] inventorySlots = new RelicInventorySlotUI[GameProgressData.MaxOwnedRelicCount];

    [Header("Equipped Relics")]
    [SerializeField] private RelicEquippedSlotUI[] equippedSlots = new RelicEquippedSlotUI[GameProgressData.RelicSlotCount];
    [SerializeField] private Sprite emptyEquippedSlotIcon;

    [Header("Tooltip")]
    [SerializeField] private RelicTooltipUI tooltipUI;

    [Header("Fallback")]
    [SerializeField] private Sprite temporaryIcon;

    private readonly RelicSlotEntry[] visibleEntries = new RelicSlotEntry[GameProgressData.MaxOwnedRelicCount];
    private RelicInventoryCategory currentCategory = RelicInventoryCategory.All;

    private struct RelicSlotEntry
    {
        public bool HasEntry { get; }
        public RelicInstanceData RelicInstance { get; }
        public RelicData RelicData { get; }

        public RelicSlotEntry(RelicInstanceData relicInstance, RelicData relicData)
        {
            HasEntry = true;
            RelicInstance = relicInstance;
            RelicData = relicData;
        }
    }

    private void OnEnable()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.ProgressChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.ProgressChanged -= Refresh;

        if (tooltipUI != null)
            tooltipUI.Hide();
    }

    public void ShowAll() => SetCategory(RelicInventoryCategory.All);
    public void ShowAttackPower() => SetCategory(RelicInventoryCategory.AttackPower);
    public void ShowMaxHealth() => SetCategory(RelicInventoryCategory.MaxHealth);
    public void ShowMoveSpeed() => SetCategory(RelicInventoryCategory.MoveSpeed);
    public void ShowDamageReduction() => SetCategory(RelicInventoryCategory.DamageReduction);
    public void UnequipFirstSlot() => UnequipRelic(0);
    public void UnequipSecondSlot() => UnequipRelic(1);

    public void SetCategory(RelicInventoryCategory category)
    {
        currentCategory = category;
        Refresh();
    }

    public void Refresh()
    {
        BuildVisibleEntries();
        RefreshInventorySlots();
        RefreshEquippedRelics();
    }

    public void UnequipRelic(int slotIndex)
    {
        if (RelicManager.Instance == null)
            return;

        RelicManager.Instance.UnequipRelic(slotIndex);
    }

    private void BuildVisibleEntries()
    {
        Array.Clear(visibleEntries, 0, visibleEntries.Length);

        if (RelicManager.Instance == null)
            return;

        IReadOnlyList<RelicInventoryData> ownedRelicGroups = RelicManager.Instance.GetOwnedRelicGroups();

        for (int groupIndex = 0; groupIndex < ownedRelicGroups.Count; groupIndex++)
        {
            RelicInventoryData inventoryData = ownedRelicGroups[groupIndex];

            if (inventoryData == null || inventoryData.Instances == null)
                continue;

            RelicData relicData = RelicManager.Instance.FindRelic(inventoryData.RelicId);

            if (!IsVisibleByCategory(relicData))
                continue;

            IReadOnlyList<RelicInstanceData> relicInstances = inventoryData.Instances;

            for (int instanceIndex = 0; instanceIndex < relicInstances.Count; instanceIndex++)
            {
                RelicInstanceData relicInstance = relicInstances[instanceIndex];

                if (relicInstance == null || IsEquipped(relicInstance))
                    continue;

                int inventorySlotIndex = relicInstance.InventorySlotIndex;

                if (inventorySlotIndex < 0 || inventorySlotIndex >= visibleEntries.Length)
                    continue;

                visibleEntries[inventorySlotIndex] = new RelicSlotEntry(relicInstance, relicData);
            }
        }
    }

    private void RefreshInventorySlots()
    {
        if (inventorySlots == null)
            return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            RelicInventorySlotUI slot = inventorySlots[i];

            if (slot == null)
                continue;

            if (i >= visibleEntries.Length || i >= GameProgressData.MaxOwnedRelicCount || !visibleEntries[i].HasEntry)
            {
                slot.Clear();
                continue;
            }

            RelicSlotEntry entry = visibleEntries[i];
            string tooltipText = BuildTooltipText(entry.RelicInstance);
            bool isEquipped = IsEquipped(entry.RelicInstance);
            slot.Set(entry.RelicInstance, entry.RelicData, temporaryIcon, isEquipped, tooltipUI, tooltipText, HandleInventorySlotRightClicked);
        }
    }

    private void RefreshEquippedRelics()
    {
        if (equippedSlots == null)
            return;

        GameProgressManager progressManager = GameProgressManager.Instance;
        RelicManager relicManager = RelicManager.Instance;

        for (int i = 0; i < equippedSlots.Length && i < GameProgressData.RelicSlotCount; i++)
        {
            RelicEquippedSlotUI equippedSlot = equippedSlots[i];

            if (equippedSlot == null)
                continue;

            RelicInstanceData equippedRelic = progressManager != null ? progressManager.GetEquippedRelic(i) : null;
            RelicData relicData = relicManager != null ? relicManager.GetRelicData(equippedRelic) : null;
            string tooltipText = BuildTooltipText(equippedRelic);
            equippedSlot.Set(i, equippedRelic, relicData, emptyEquippedSlotIcon, tooltipUI, tooltipText, HandleEquippedSlotRightClicked);
        }
    }

    private void HandleInventorySlotRightClicked(RelicInstanceData relicInstance)
    {
        if (relicInstance == null || RelicManager.Instance == null)
            return;

        int equippedSlotIndex = FindEquippedSlotIndex(relicInstance.InstanceId);

        if (equippedSlotIndex >= 0)
        {
            RelicManager.Instance.UnequipRelic(equippedSlotIndex);
            return;
        }

        int targetSlotIndex = FindFirstEmptyEquippedSlotIndex();

        if (targetSlotIndex < 0)
            targetSlotIndex = 0;

        RelicManager.Instance.TryEquipRelic(relicInstance.InstanceId, targetSlotIndex);
    }

    private void HandleEquippedSlotRightClicked(int slotIndex)
    {
        if (RelicManager.Instance == null)
            return;

        RelicManager.Instance.UnequipRelic(slotIndex);
    }

    private bool IsVisibleByCategory(RelicData relicData)
    {
        if (currentCategory == RelicInventoryCategory.All)
            return true;

        if (relicData == null || !TryGetCategoryEffectType(currentCategory, out RelicEffectType targetEffectType))
            return false;

        IReadOnlyList<RelicEffectData> effects = relicData.Effects;

        if (effects == null)
            return false;

        for (int i = 0; i < effects.Count; i++)
        {
            if (effects[i] != null && effects[i].EffectType == targetEffectType)
                return true;
        }

        return false;
    }

    private bool TryGetCategoryEffectType(RelicInventoryCategory category, out RelicEffectType effectType)
    {
        switch (category)
        {
            case RelicInventoryCategory.AttackPower:
                effectType = RelicEffectType.AttackPower;
                return true;
            case RelicInventoryCategory.MaxHealth:
                effectType = RelicEffectType.MaxHealth;
                return true;
            case RelicInventoryCategory.MoveSpeed:
                effectType = RelicEffectType.MoveSpeed;
                return true;
            case RelicInventoryCategory.DamageReduction:
                effectType = RelicEffectType.DamageReduction;
                return true;
            default:
                effectType = default;
                return false;
        }
    }

    private string BuildTooltipText(RelicInstanceData relicInstance)
    {
        if (relicInstance == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        if (relicInstance.Effects != null && relicInstance.Effects.Count > 0)
        {
            for (int i = 0; i < relicInstance.Effects.Count; i++)
            {
                RelicEffectValue effect = relicInstance.Effects[i];

                if (effect == null)
                    continue;

                builder.AppendLine($"{GetEffectDisplayName(effect.EffectType)} {FormatEffectValue(effect.Value)}");
            }
        }
        else
        {
            builder.AppendLine("효과 없음");
        }

        builder.AppendLine($"가격 {relicInstance.Price}");
        return builder.ToString().TrimEnd();
    }

    private string GetEffectDisplayName(RelicEffectType effectType)
    {
        switch (effectType)
        {
            case RelicEffectType.AttackPower:
                return "공격력";
            case RelicEffectType.MaxHealth:
                return "체력";
            case RelicEffectType.MoveSpeed:
                return "속도";
            case RelicEffectType.DamageReduction:
                return "피해 감소";
            default:
                return effectType.ToString();
        }
    }

    private string FormatEffectValue(float value)
    {
        string prefix = value > 0f ? "+" : string.Empty;
        return $"{prefix}{value:0.##}";
    }

    private bool IsEquipped(RelicInstanceData relicInstance)
    {
        return relicInstance != null && FindEquippedSlotIndex(relicInstance.InstanceId) >= 0;
    }

    private int FindEquippedSlotIndex(string relicInstanceId)
    {
        if (string.IsNullOrEmpty(relicInstanceId) || GameProgressManager.Instance == null)
            return -1;

        for (int i = 0; i < GameProgressData.RelicSlotCount; i++)
        {
            RelicInstanceData equippedRelic = GameProgressManager.Instance.GetEquippedRelic(i);

            if (equippedRelic != null && IdsMatch(equippedRelic.InstanceId, relicInstanceId))
                return i;
        }

        return -1;
    }

    private int FindFirstEmptyEquippedSlotIndex()
    {
        if (GameProgressManager.Instance == null)
            return -1;

        for (int i = 0; i < GameProgressData.RelicSlotCount; i++)
        {
            RelicInstanceData equippedRelic = GameProgressManager.Instance.GetEquippedRelic(i);

            if (equippedRelic == null)
                return i;
        }

        return -1;
    }

    private bool IdsMatch(string left, string right)
    {
        return string.Equals(left, right, StringComparison.Ordinal);
    }
}
