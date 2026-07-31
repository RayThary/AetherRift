using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Serializable]
    private class EnemySpawnData
    {
        [SerializeField] private EnemyHealth enemyPrefab;
        [SerializeField] private Transform spawnPoint;

        public EnemyHealth EnemyPrefab => enemyPrefab;
        public Transform SpawnPoint => spawnPoint;
    }

    [Serializable]
    private class BattleWave
    {
        [SerializeField] private string waveName = "Wave";
        [SerializeField] private EnemySpawnData[] enemies;

        public string WaveName => waveName;
        public EnemySpawnData[] Enemies => enemies;
    }

    private enum BattleState
    {
        Waiting,
        Playing,
        Result
    }

    [Header("Reference")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform enemyContainer;

    [Header("Wave")]
    [SerializeField] private BattleWave[] waves;
    [SerializeField] private float nextWaveDelay = 1.5f;

    [Header("Result UI")]
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private GameObject failPanel;
    [SerializeField] private int lobbySceneIndex = 2;

    private readonly List<EnemyHealth> aliveEnemies = new();

    private BattleState battleState = BattleState.Waiting;
    private int currentWaveIndex;
    private bool isChangingWave;

    private void Awake()
    {
        if (clearPanel != null)
            clearPanel.SetActive(false);

        if (failPanel != null)
            failPanel.SetActive(false);
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogError("[BattleManager] PlayerHealth를 찾지 못했습니다.");
            return;
        }

        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("[BattleManager] Wave가 설정되지 않았습니다.");
            return;
        }

        battleState = BattleState.Playing;
        SpawnCurrentWave();
    }

    private void Update()
    {
        if (battleState != BattleState.Playing)
            return;

        if (!playerHealth.gameObject.activeInHierarchy)
        {
            ShowFail();
            return;
        }

        RemoveDefeatedEnemies();

        if (aliveEnemies.Count > 0 || isChangingWave)
            return;

        MoveToNextWave();
    }

    private void SpawnCurrentWave()
    {
        BattleWave currentWave = waves[currentWaveIndex];
        EnemySpawnData[] spawnDataList = currentWave.Enemies;

        if (spawnDataList == null || spawnDataList.Length == 0)
        {
            Debug.LogError($"[BattleManager] {currentWave.WaveName}에 배치된 적이 없습니다.");
            return;
        }

        for (int i = 0; i < spawnDataList.Length; i++)
        {
            EnemySpawnData spawnData = spawnDataList[i];

            if (spawnData.EnemyPrefab == null || spawnData.SpawnPoint == null)
            {
                Debug.LogWarning($"[BattleManager] {currentWave.WaveName}의 {i + 1}번째 적 설정이 비어 있습니다.");
                continue;
            }

            EnemyHealth spawnedEnemy = Instantiate(spawnData.EnemyPrefab, spawnData.SpawnPoint.position, spawnData.SpawnPoint.rotation, enemyContainer);
            EnemyTarget enemyTarget = spawnedEnemy.GetComponent<EnemyTarget>();

            if (enemyTarget == null)
            {
                Debug.LogWarning("[BattleManager] 생성된 적에 EnemyTarget이 없습니다.", spawnedEnemy);
            }
            else
            {
                enemyTarget.SetTarget(playerHealth.transform);
            }

            aliveEnemies.Add(spawnedEnemy);
        }

        if (aliveEnemies.Count == 0)
            Debug.LogError($"[BattleManager] {currentWave.WaveName}에 생성된 적이 없습니다.");
    }

    private void RemoveDefeatedEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyHealth enemy = aliveEnemies[i];

            if (enemy != null && enemy.gameObject.activeInHierarchy)
                continue;

            aliveEnemies.RemoveAt(i);
        }
    }

    private void MoveToNextWave()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= waves.Length)
        {
            ShowClear();
            return;
        }

        StartCoroutine(SpawnNextWaveAfterDelay());
    }

    private IEnumerator SpawnNextWaveAfterDelay()
    {
        isChangingWave = true;
        yield return new WaitForSeconds(nextWaveDelay);

        if (battleState == BattleState.Playing)
            SpawnCurrentWave();

        isChangingWave = false;
    }

    private void ShowClear()
    {
        battleState = BattleState.Result;
        StopBattleControl();

        if (clearPanel != null)
            clearPanel.SetActive(true);
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
