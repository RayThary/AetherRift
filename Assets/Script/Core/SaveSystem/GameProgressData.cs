using System;
using System.Collections.Generic;

public enum QuestProgressState
{
    NotAccepted,
    InProgress,
    ReadyToComplete,
    Completed
}

[Serializable]
public class QuestProgressData
{
    public string questId;
    public QuestProgressState state;
    public int currentAmount;

    public QuestProgressData(string questId)
    {
        this.questId = questId;
        state = QuestProgressState.NotAccepted;
        currentAmount = 0;
    }
}

[Serializable]
public class GameProgressData
{
    public const int RelicSlotCount = 2;
    public const int MaxOwnedRelicCount = 30;

    public int saveVersion = 4;
    public int currency;
    public int currentHealth = -1;
    public int maxHealthBonus;
    public int attackPowerBonus;
    public float moveSpeedBonus;

    // v4: 유물 종류 ID별로 묶고, 각 묶음 안에 개별 유물 데이터를 저장합니다.
    public List<RelicInventoryData> ownedRelicGroups = new List<RelicInventoryData>();
    public List<RelicInstanceData> ownedRelics = new List<RelicInstanceData>();
    public List<string> equippedRelicInstanceIds = new List<string> { string.Empty, string.Empty };

    // v3 저장 파일 변환용입니다. v4에서는 ownedRelicGroups만 사용합니다.
    // v2 이하 저장 파일 변환용입니다.
    public List<string> ownedRelicIds = new List<string>();
    public List<string> equippedRelicIds = new List<string> { string.Empty, string.Empty };

    public List<QuestProgressData> questProgressList = new List<QuestProgressData>();
    public List<string> unlockedDungeonIds = new List<string>();
    public List<string> completedDungeonIds = new List<string>();
    public bool finalBossCleared;

    public void EnsureValid()
    {
        if (saveVersion < 2)
        {
            currentHealth = -1;
            saveVersion = 2;
        }

        ownedRelicGroups ??= new List<RelicInventoryData>();
        ownedRelics ??= new List<RelicInstanceData>();
        equippedRelicInstanceIds ??= new List<string>();
        ownedRelicIds ??= new List<string>();
        equippedRelicIds ??= new List<string>();
        questProgressList ??= new List<QuestProgressData>();
        unlockedDungeonIds ??= new List<string>();
        completedDungeonIds ??= new List<string>();

        EnsureRelicSlots(equippedRelicInstanceIds);
        EnsureRelicSlots(equippedRelicIds);

        for (int i = 0; i < ownedRelicGroups.Count; i++)
            ownedRelicGroups[i]?.EnsureValid();

        for (int i = 0; i < ownedRelics.Count; i++)
            ownedRelics[i]?.EnsureValid();

        maxHealthBonus = Math.Max(maxHealthBonus, 0);
        attackPowerBonus = Math.Max(attackPowerBonus, 0);
        moveSpeedBonus = Math.Max(moveSpeedBonus, 0f);
        currentHealth = Math.Max(currentHealth, -1);
    }

    private static void EnsureRelicSlots(List<string> relicIds)
    {
        while (relicIds.Count < RelicSlotCount)
            relicIds.Add(string.Empty);

        if (relicIds.Count > RelicSlotCount)
            relicIds.RemoveRange(RelicSlotCount, relicIds.Count - RelicSlotCount);
    }
}
