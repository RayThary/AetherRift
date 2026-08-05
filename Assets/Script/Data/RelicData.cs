using System;
using System.Collections.Generic;
using UnityEngine;

public enum RelicEffectType
{
    AttackPower,
    MaxHealth,
    DodgeCooldown,
    SkillCooldown
}

[Serializable]
public class RelicEffectData
{
    [SerializeField] private RelicEffectType effectType;
    [Tooltip("증가는 양수, 감소는 음수로 입력합니다.")]
    [SerializeField] private float value;

    public RelicEffectType EffectType => effectType;
    public float Value => value;
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
    [SerializeField] private int price;

    [Header("Effects")]
    [SerializeField] private List<RelicEffectData> effects = new List<RelicEffectData>();

    public string RelicId => relicId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public int Price => price;
    public IReadOnlyList<RelicEffectData> Effects => effects;

    private void OnValidate()
    {
        relicId = (relicId ?? string.Empty).Trim();
        price = Mathf.Max(0, price);
    }
}
