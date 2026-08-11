using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public enum RelicEffectType
{
    AttackPower,
    MaxHealth,
    MoveSpeed,
    DamageReduction
}

[Serializable]
public class RelicEffectData
{
    [SerializeField] private RelicEffectType effectType;

    [Tooltip("획득 시 적용될 최소 수치입니다.")]
    [FormerlySerializedAs("value")]
    [SerializeField] private float minValue;

    [Tooltip("획득 시 적용될 최대 수치입니다.")]
    [SerializeField] private float maxValue;

    [SerializeField, HideInInspector] private bool hasValueRange;

    public RelicEffectType EffectType => effectType;
    public float MinValue => minValue;
    public float MaxValue => maxValue;

    public float GetRandomValue()
    {
        return UnityEngine.Random.Range(minValue, maxValue);
    }

    public float GetValueProgress(float value)
    {
        if (Mathf.Approximately(minValue, maxValue))
            return 0f;

        return Mathf.InverseLerp(minValue, maxValue, value);
    }

    public void Validate()
    {
        if (!hasValueRange)
        {
            maxValue = minValue;
            hasValueRange = true;
        }

        if (minValue > maxValue)
            (minValue, maxValue) = (maxValue, minValue);
    }
}

[CreateAssetMenu(fileName = "RelicData", menuName = "AetherRift/Data/Relic Data")]
public class RelicData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string relicId;
    [SerializeField] private string displayName;

    [TextArea(2, 4)]
    [SerializeField] private string description;

    [SerializeField] private Sprite icon;

    [Header("Shop")]
    [Min(0)]
    [FormerlySerializedAs("price")]
    [SerializeField] private int minPrice;

    [Min(0)]
    [SerializeField] private int maxPrice;

    [SerializeField, HideInInspector] private bool hasPriceRange;

    [Header("Effects")]
    [SerializeField] private List<RelicEffectData> effects = new List<RelicEffectData>();

    public string RelicId => relicId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int MinPrice => minPrice;
    public int MaxPrice => maxPrice;
    public IReadOnlyList<RelicEffectData> Effects => effects;

    public RelicInstanceData CreateInstance()
    {
        effects ??= new List<RelicEffectData>();
        List<RelicEffectValue> rolledEffects = new List<RelicEffectValue>();

        for (int i = 0; i < effects.Count; i++)
        {
            RelicEffectData effect = effects[i];

            if (effect == null)
                continue;

            rolledEffects.Add(new RelicEffectValue(effect.EffectType, effect.GetRandomValue()));
        }

        int rolledPrice = CalculatePrice(rolledEffects);
        return new RelicInstanceData(Guid.NewGuid().ToString("N"), relicId, rolledEffects, rolledPrice);
    }

    private int CalculatePrice(IReadOnlyList<RelicEffectValue> rolledEffects)
    {
        effects ??= new List<RelicEffectData>();

        if (effects.Count == 0 || minPrice == maxPrice)
            return minPrice;

        float totalProgress = 0f;
        int validEffectCount = 0;

        for (int i = 0; i < effects.Count; i++)
        {
            RelicEffectData effect = effects[i];

            if (effect == null || !TryFindRolledValue(rolledEffects, effect.EffectType, out float rolledValue))
                continue;

            totalProgress += effect.GetValueProgress(rolledValue);
            validEffectCount++;
        }

        float averageProgress = validEffectCount > 0 ? totalProgress / validEffectCount : 0f;
        return Mathf.RoundToInt(Mathf.Lerp(minPrice, maxPrice, averageProgress));
    }

    private static bool TryFindRolledValue(IReadOnlyList<RelicEffectValue> rolledEffects, RelicEffectType effectType, out float value)
    {
        if (rolledEffects != null)
        {
            for (int i = 0; i < rolledEffects.Count; i++)
            {
                RelicEffectValue rolledEffect = rolledEffects[i];

                if (rolledEffect != null && rolledEffect.EffectType == effectType)
                {
                    value = rolledEffect.Value;
                    return true;
                }
            }
        }

        value = 0f;
        return false;
    }

    private void OnValidate()
    {
        effects ??= new List<RelicEffectData>();
        relicId = (relicId ?? string.Empty).Trim();

        if (!hasPriceRange)
        {
            maxPrice = minPrice;
            hasPriceRange = true;
        }

        minPrice = Mathf.Max(0, minPrice);
        maxPrice = Mathf.Max(0, maxPrice);

        if (minPrice > maxPrice)
            (minPrice, maxPrice) = (maxPrice, minPrice);

        foreach (RelicEffectData effect in effects)
            effect?.Validate();
    }
}
