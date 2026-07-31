using UnityEngine;

[RequireComponent(typeof(EnemyCore), typeof(EnemyTarget), typeof(EnemyMovement))]
[RequireComponent(typeof(MeleeEnemyCombat))]
public class BossEnemyAI : MonoBehaviour
{
    [Header("Range")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float attackRange = 3f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.4f;
    [Range(0f, 1f)]
    [SerializeField] private float heavyAttackChance = 0.6f;

    private EnemyCore enemyCore;
    private EnemyTarget enemyTarget;
    private EnemyMovement enemyMovement;
    private MeleeEnemyCombat enemyCombat;

    private float nextAttackTime;
    private bool wasAttacking;

    private void Awake()
    {
        enemyCore = GetComponent<EnemyCore>();
        enemyTarget = GetComponent<EnemyTarget>();
        enemyMovement = GetComponent<EnemyMovement>();
        enemyCombat = GetComponent<MeleeEnemyCombat>();
    }

    private void Update()
    {
        if (enemyCore.IsDead)
        {
            enemyMovement.Stop();
            return;
        }

        Transform target = enemyTarget.Target;

        if (target == null)
        {
            enemyCore.EnterIdle();
            enemyMovement.Stop();
            return;
        }

        if (enemyCore.CurrentState == EnemyState.Hit)
        {
            enemyMovement.Stop();
            return;
        }

        if (enemyCore.CurrentState == EnemyState.Attack)
        {
            wasAttacking = true;
            enemyMovement.Stop();
            enemyMovement.RotateTo(target.position);
            return;
        }

        if (wasAttacking)
        {
            wasAttacking = false;
            nextAttackTime = Time.time + attackCooldown;
        }

        float distance = enemyMovement.GetFlatDistanceTo(target.position);

        if (distance > detectionRange)
        {
            enemyCore.EnterIdle();
            enemyMovement.Stop();
            return;
        }

        enemyMovement.RotateTo(target.position);

        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            enemyMovement.Stop();
            TryAttack();
            return;
        }

        enemyCore.EnterChase();

        if (!enemyMovement.MoveTo(target.position, attackRange * 0.85f))
            enemyMovement.Stop();
    }

    private void TryAttack()
    {
        if (Random.value < heavyAttackChance)
            enemyCombat.TryStartHeavyAttack();
        else
            enemyCombat.TryStartLightAttack();
    }
}
