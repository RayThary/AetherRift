using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DungeonEnemyData
{
    [SerializeField] private EnemyHealth enemyPrefab;
    [Min(1)]
    [SerializeField] private int spawnCount = 1;
    [Min(0f)]
    [SerializeField] private float spawnInterval = 0.2f;

    public EnemyHealth EnemyPrefab => enemyPrefab;
    public int SpawnCount => spawnCount;
    public float SpawnInterval => spawnInterval;
}

[Serializable]
public class DungeonWaveData
{
    [SerializeField] private string waveName;
    [SerializeField] private List<DungeonEnemyData> enemies = new List<DungeonEnemyData>();
    [Min(0f)]
    [SerializeField] private float nextWaveDelay = 1.5f;

    public string WaveName => waveName;
    public IReadOnlyList<DungeonEnemyData> Enemies => enemies;
    public float NextWaveDelay => nextWaveDelay;
}

[CreateAssetMenu(fileName = "DungeonData", menuName = "AetherRift/Data/Dungeon Data")]
public class DungeonData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string dungeonId;
    [SerializeField] private string displayName;
    [TextArea(2, 4)]
    [SerializeField] private string description;
    [SerializeField] private Sprite previewImage;

    [Header("Unlock")]
    [SerializeField] private bool unlockedByDefault;
    [SerializeField] private string requiredCompletedDungeonId;

    [Header("Battle")]
    [SerializeField] private bool finalBossDungeon;
    [SerializeField] private List<DungeonWaveData> waves = new List<DungeonWaveData>();

    [Header("Reward")]
    [Min(0)]
    [SerializeField] private int currencyReward;

    public string DungeonId => dungeonId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite PreviewImage => previewImage;
    public bool UnlockedByDefault => unlockedByDefault;
    public string RequiredCompletedDungeonId => requiredCompletedDungeonId;
    public bool FinalBossDungeon => finalBossDungeon;
    public IReadOnlyList<DungeonWaveData> Waves => waves;
    public int CurrencyReward => currencyReward;

    private void OnValidate()
    {
        dungeonId = (dungeonId ?? string.Empty).Trim();
        requiredCompletedDungeonId = (requiredCompletedDungeonId ?? string.Empty).Trim();
        currencyReward = Mathf.Max(0, currencyReward);
    }
}
