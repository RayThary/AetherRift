using UnityEngine;

public enum QuestObjectiveType
{
    ClearDungeon,
    DefeatEnemy,
    DefeatBoss
}

[CreateAssetMenu(fileName = "QuestData", menuName = "AetherRift/Data/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string questId;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;

    [Header("Objective")]
    [SerializeField] private QuestObjectiveType objectiveType;
    [Tooltip("던전 ID 또는 적 ID를 입력합니다.")]
    [SerializeField] private string targetId;
    [Min(1)]
    [SerializeField] private int requiredAmount = 1;

    [Header("Reward")]
    [Min(0)]
    [SerializeField] private int currencyReward;
    [SerializeField] private RelicData relicReward;

    [Header("Quest Chain")]
    [SerializeField] private QuestData nextQuest;

    public string QuestId => questId;
    public string DisplayName => displayName;
    public string Description => description;
    public QuestObjectiveType ObjectiveType => objectiveType;
    public string TargetId => targetId;
    public int RequiredAmount => requiredAmount;
    public int CurrencyReward => currencyReward;
    public RelicData RelicReward => relicReward;
    public QuestData NextQuest => nextQuest;

    private void OnValidate()
    {
        questId = (questId ?? string.Empty).Trim();
        targetId = (targetId ?? string.Empty).Trim();
        requiredAmount = Mathf.Max(1, requiredAmount);
        currencyReward = Mathf.Max(0, currencyReward);
    }
}
