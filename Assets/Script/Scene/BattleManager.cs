using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    private enum BattleState
    {
        Waiting,
        Playing,
        Result
    }

    [Header("Dungeon")]
    [SerializeField] private DungeonData dungeonData;

    [Header("Reference")]
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn")]
    [Min(0f)]
    [SerializeField] private float battleStartDelay = 1f;

    [Header("Player Battle Setup")]
    [SerializeField] private bool restorePlayerHealthOnStart = true;
    [SerializeField] private bool refillHealingPotionsOnStart = true;

    [Header("Result")]
    [SerializeField] private GameObject clearPanel;
    [Min(0f)]
    [SerializeField] private float clearPanelDuration = 2f;
    [SerializeField] private GameObject failPanel;
    [SerializeField] private ScenePortal exitPortal;
    [SerializeField] private int lobbySceneIndex = 2;

    private readonly List<EnemyHealth> aliveEnemies = new();

    private PlayerHealth playerHealth;
    private BattleState battleState = BattleState.Waiting;
    private int currentWaveIndex;
    private int nextSpawnPointIndex;
    private bool isSpawningWave;
    private bool isChangingWave;
    private bool rewardGranted;

    private void Awake()
    {
        if (clearPanel != null)
            clearPanel.SetActive(false);

        if (failPanel != null)
            failPanel.SetActive(false);

        if (exitPortal != null)
            exitPortal.gameObject.SetActive(false);
    }

    private void Start()
    {
        ResolveSelectedDungeonData();
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogError("[BattleManager] PlayerHealth를 찾지 못했습니다.", this);
            return;
        }

        if (dungeonData == null)
        {
            Debug.LogError("[BattleManager] DungeonData가 설정되지 않았습니다.", this);
            return;
        }

        if (dungeonData.Waves == null || dungeonData.Waves.Count == 0)
        {
            Debug.LogError($"[BattleManager] {dungeonData.DisplayName}에 Wave가 설정되지 않았습니다.", dungeonData);
            return;
        }

        if (!HasValidSpawnPoint())
        {
            Debug.LogError("[BattleManager] 사용할 수 있는 Spawn Point가 없습니다.", this);
            return;
        }

        playerHealth.Died += HandlePlayerDied;
        SetupPlayerForBattle();

        battleState = BattleState.Playing;
        StartCoroutine(StartBattleAfterDelay());
    }

    private void ResolveSelectedDungeonData()
    {
        if (GameManager.Instance == null || GameManager.Instance.SelectedDungeonData == null)
            return;

        dungeonData = GameManager.Instance.SelectedDungeonData;
    }

    private void SetupPlayerForBattle()
    {
        if (playerHealth == null)
            return;

        if (restorePlayerHealthOnStart)
            playerHealth.RestoreFullHealth();

        PlayerHealingPotionController healingPotionController = playerHealth.GetComponent<PlayerHealingPotionController>();

        if (refillHealingPotionsOnStart && healingPotionController != null)
            healingPotionController.RefillPotions();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.Died -= HandlePlayerDied;

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            EnemyHealth enemy = aliveEnemies[i];

            if (enemy != null)
                enemy.Died -= HandleEnemyDied;
            }
    }

    private IEnumerator StartBattleAfterDelay()
    {
        if (battleStartDelay > 0f)
            yield return new WaitForSeconds(battleStartDelay);

        if (battleState != BattleState.Playing)
            yield break;

        yield return SpawnCurrentWave();
    }

    private IEnumerator SpawnCurrentWave()
    {
        isSpawningWave = true;
        nextSpawnPointIndex = 0;

        DungeonWaveData currentWave = dungeonData.Waves[currentWaveIndex];
        IReadOnlyList<DungeonEnemyData> enemyDataList = currentWave.Enemies;
        int spawnedEnemyCount = 0;

        if (enemyDataList == null || enemyDataList.Count == 0)
        {
            Debug.LogError($"[BattleManager] {currentWave.WaveName}에 적 데이터가 없습니다.", dungeonData);
            isSpawningWave = false;
            TryCompleteCurrentWave();
            yield break;
        }

        for (int i = 0; i < enemyDataList.Count; i++)
        {
            DungeonEnemyData enemyData = enemyDataList[i];

            if (enemyData == null || enemyData.EnemyPrefab == null)
            {
                Debug.LogWarning($"[BattleManager] {currentWave.WaveName}의 {i + 1}번째 적 프리팹이 비어 있습니다.", dungeonData);
                continue;
            }

            for (int spawnIndex = 0; spawnIndex < enemyData.SpawnCount; spawnIndex++)
            {
                if (battleState != BattleState.Playing)
                {
                    isSpawningWave = false;
                    yield break;
                }

                Transform spawnPoint = GetNextSpawnPoint();

                if (spawnPoint == null)
                {
                    Debug.LogError("[BattleManager] 사용할 수 있는 Spawn Point가 없습니다.", this);
                    isSpawningWave = false;
                    yield break;
                }

                EnemyHealth spawnedEnemy = Instantiate(enemyData.EnemyPrefab, spawnPoint.position, spawnPoint.rotation, enemyContainer);               

                spawnedEnemy.Died += HandleEnemyDied;
                aliveEnemies.Add(spawnedEnemy);
                spawnedEnemyCount++;

                if (spawnIndex < enemyData.SpawnCount - 1 && enemyData.SpawnInterval > 0f)
                    yield return new WaitForSeconds(enemyData.SpawnInterval);
            }
        }

        isSpawningWave = false;

        if (spawnedEnemyCount == 0)
            Debug.LogError($"[BattleManager] {currentWave.WaveName}에 생성된 적이 없습니다.", dungeonData);

        TryCompleteCurrentWave();
    }

    private bool HasValidSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return false;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
                return true;
        }

        return false;
    }

    private Transform GetNextSpawnPoint()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform spawnPoint = spawnPoints[nextSpawnPointIndex];
            nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;

            if (spawnPoint != null)
                return spawnPoint;
        }

        return null;
    }

    private void HandleEnemyDied(EnemyHealth defeatedEnemy)
    {
        if (defeatedEnemy == null)
            return;

        defeatedEnemy.Died -= HandleEnemyDied;
        aliveEnemies.Remove(defeatedEnemy);
        TryCompleteCurrentWave();
    }

    private void HandlePlayerDied()
    {
        if (battleState != BattleState.Playing)
            return;

        ShowFail();
    }

    private void TryCompleteCurrentWave()
    {
        if (battleState != BattleState.Playing || isSpawningWave || isChangingWave || aliveEnemies.Count > 0)
            return;

        MoveToNextWave();
    }

    private void MoveToNextWave()
    {
        DungeonWaveData completedWave = dungeonData.Waves[currentWaveIndex];
        currentWaveIndex++;

        if (currentWaveIndex >= dungeonData.Waves.Count)
        {
            ShowClear();
            return;
        }

        StartCoroutine(SpawnNextWaveAfterDelay(completedWave.NextWaveDelay));
    }

    private IEnumerator SpawnNextWaveAfterDelay(float delay)
    {
        isChangingWave = true;
        yield return new WaitForSeconds(delay);

        if (battleState != BattleState.Playing)
        {
            isChangingWave = false;
            yield break;
        }

        isChangingWave = false;
        yield return SpawnCurrentWave();
    }

    private void ShowClear()
    {
        battleState = BattleState.Result;
        StopEnemyControl();
        GrantClearReward();

        if (clearPanel != null)
        {
            clearPanel.SetActive(true);
            StartCoroutine(HideClearPanelAfterDelay());
        }

        if (exitPortal != null)
            exitPortal.gameObject.SetActive(true);
        else
            Debug.LogWarning("[BattleManager] Exit Portal이 설정되지 않았습니다.", this);
    }

    private void GrantClearReward()
    {
        if (rewardGranted || dungeonData == null || GameProgressManager.Instance == null)
            return;

        if (!GameProgressManager.Instance.CompleteDungeon(dungeonData))
            return;

        rewardGranted = true;

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    private IEnumerator HideClearPanelAfterDelay()
    {
        yield return new WaitForSeconds(clearPanelDuration);

        if (clearPanel != null)
            clearPanel.SetActive(false);
    }

    private void ShowFail()
    {
        battleState = BattleState.Result;
        StopBattleControl();

        if (failPanel != null)
            failPanel.SetActive(true);
    }

    private void StopBattleControl()
    {
        StopPlayerControl();
        StopEnemyControl();
    }

    private void StopPlayerControl()
    {
        if (playerHealth == null)
            return;

        PlayerMovement playerMovement = playerHealth.GetComponent<PlayerMovement>();
        PlayerCombat playerCombat = playerHealth.GetComponent<PlayerCombat>();
        PlayerDodge playerDodge = playerHealth.GetComponent<PlayerDodge>();

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerCombat != null)
            playerCombat.enabled = false;

        if (playerDodge != null)
            playerDodge.enabled = false;
    }

    private void StopEnemyControl()
    {
        EnemyMovement[] enemyMovements = FindObjectsByType<EnemyMovement>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < enemyMovements.Length; i++)
        {
            EnemyMovement enemyMovement = enemyMovements[i];
            enemyMovement.Stop();
        }

        MeleeEnemyAI[] meleeAIs = FindObjectsByType<MeleeEnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < meleeAIs.Length; i++)
            meleeAIs[i].enabled = false;

        ArcherEnemyAI[] archerAIs = FindObjectsByType<ArcherEnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < archerAIs.Length; i++)
            archerAIs[i].enabled = false;

        BossEnemyAI[] bossAIs = FindObjectsByType<BossEnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < bossAIs.Length; i++)
            bossAIs[i].enabled = false;
    }

    public void RetryBattle()
    {
        if (SceneLoader.Instance == null)
            return;

        SceneLoader.Instance.ReloadCurrentScene();
    }

    public void ReturnToLobby()
    {
        if (SceneLoader.Instance == null)
            return;

        SceneLoader.Instance.LoadScene(lobbySceneIndex);
    }
}
