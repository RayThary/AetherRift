using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RelicEffectValue
{
    [SerializeField] private RelicEffectType effectType;
    [SerializeField] private float value;

    public RelicEffectType EffectType => effectType;
    public float Value => value;

    public RelicEffectValue(RelicEffectType effectType, float value)
    {
        this.effectType = effectType;
        this.value = value;
    }
}

[Serializable]
public class RelicInstanceData
{
    public string instanceId;
    public string relicId;
    public int inventorySlotIndex = -1;
    public int price;
    public List<RelicEffectValue> effects = new List<RelicEffectValue>();

    public string InstanceId => instanceId;
    public string RelicId => relicId;
    public int InventorySlotIndex => inventorySlotIndex;
    public int Price => price;
    public IReadOnlyList<RelicEffectValue> Effects => effects;

    public RelicInstanceData()
    {
    }

    public RelicInstanceData(string instanceId, string relicId, List<RelicEffectValue> effects, int price)
    {
        this.instanceId = instanceId;
        this.relicId = relicId;
        this.effects = effects ?? new List<RelicEffectValue>();
        this.price = price;
    }

    public void EnsureValid()
    {
        instanceId = (instanceId ?? string.Empty).Trim();
        relicId = (relicId ?? string.Empty).Trim();
        inventorySlotIndex = inventorySlotIndex >= 0 && inventorySlotIndex < GameProgressData.MaxOwnedRelicCount ? inventorySlotIndex : -1;
        price = Mathf.Max(price, 0);
        effects ??= new List<RelicEffectValue>();
    }
}

[Serializable]
public class RelicInventoryData
{
    public string relicId;
    public List<RelicInstanceData> instances = new List<RelicInstanceData>();

    public string RelicId => relicId;
    public IReadOnlyList<RelicInstanceData> Instances => instances;
    public int Count => instances?.Count ?? 0;

    public RelicInventoryData()
    {
    }

    public RelicInventoryData(string relicId)
    {
        this.relicId = relicId;
    }

    public void EnsureValid()
    {
        relicId = (relicId ?? string.Empty).Trim();
        instances ??= new List<RelicInstanceData>();

        for (int i = instances.Count - 1; i >= 0; i--)
        {
            RelicInstanceData relicInstance = instances[i];

            if (relicInstance == null)
            {
                instances.RemoveAt(i);
                continue;
            }

            relicInstance.EnsureValid();
        }
    }
}
