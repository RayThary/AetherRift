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
    public const int RelicSlotCount = 2; // 최대 유물개수

    public int saveVersion = 1; // 저장 데이터 구조 버전
    public int currency;  // 보유재화 
    public List<string> ownedRelicIds = new List<string>(); // 보유 유물 ID
    public List<string> equippedRelicIds = new List<string> { string.Empty, string.Empty }; //장착 유물 ID
    public List<QuestProgressData> questProgressList = new List<QuestProgressData>(); // 퀘스트 진행 상황
    public List<string> unlockedDungeonIds = new List<string>(); // 입장 가능한 던전 ID
    public List<string> completedDungeonIds = new List<string>(); // 클리어한 던전 ID
    public bool finalBossCleared;

    public void EnsureValid()
    {
        ownedRelicIds ??= new List<string>();
        equippedRelicIds ??= new List<string>();
        questProgressList ??= new List<QuestProgressData>();
        unlockedDungeonIds ??= new List<string>();
        completedDungeonIds ??= new List<string>();

        while (equippedRelicIds.Count < RelicSlotCount)
            equippedRelicIds.Add(string.Empty);

        if (equippedRelicIds.Count > RelicSlotCount)
            equippedRelicIds.RemoveRange(RelicSlotCount, equippedRelicIds.Count - RelicSlotCount);
    }
}
