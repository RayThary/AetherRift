using UnityEngine;

[RequireComponent(typeof(ArcherCombat))]
public class ArcherEnemyAI : MonoBehaviour
{
    [Header("Range")]
    [SerializeField] private float detectionRange = 14f;
    [SerializeField] private float shootingRange = 10f;
    [SerializeField] private float tooCloseRange = 3.5f;
    [SerializeField] private float retreatStopRange = 5.5f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.6f;

    [Header("Cooldown Strafe")]
    [SerializeField] private float strafeDistance = 7f;
    [SerializeField] private float strafeSideOffset = 1.5f;
    [SerializeField] private float strafeReachDistance = 0.3f;
    [SerializeField] private float strafeRefreshInterval = 0.8f;
    [Range(0.1f, 1f)]
    [SerializeField] private float strafeSpeedMultiplier = 0.65f;

    private EnemyCore enemyCore;
    private EnemyTarget enemyTarget;
    private EnemyMovement enemyMovement;
    private ArcherCombat archerCombat;

    private float nextAttackTime;
    private float nextStrafeRefreshTime;
    private float strafeSide;

    private bool wasAttacking;
    private bool retreatAfterMeleeAttack;
    private bool hasStrafeDestination;

    private void Awake()
    {
        enemyCore = GetComponent<EnemyCore>();
        enemyTarget = GetComponent<EnemyTarget>();
        enemyMovement = GetComponent<EnemyMovement>();
        archerCombat = GetComponent<ArcherCombat>();
        strafeSide = Random.value < 0.5f ? -1f : 1f;
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
            strafeSide = Random.value < 0.5f ? -1f : 1f;
            ClearStrafeDestination();
        }

        float distance = enemyMovement.GetFlatDistanceTo(target.position);

        if (distance > detectionRange)
        {
            ClearStrafeDestination();
            enemyCore.EnterIdle();
            enemyMovement.Stop();
            return;
        }

        enemyMovement.RotateTo(target.position);

        if (retreatAfterMeleeAttack)
        {
            ClearStrafeDestination();

            if (distance < retreatStopRange)
            {
                RetreatFromTarget(target);
                return;
            }

            retreatAfterMeleeAttack = false;
        }

        if (distance <= tooCloseRange)
        {
            ClearStrafeDestination();

            if (Time.time >= nextAttackTime)
            {
                enemyMovement.Stop();

                if (archerCombat.TryStartMeleeAttack())
                    retreatAfterMeleeAttack = true;

                return;
            }

            RetreatFromTarget(target);
            return;
        }

        if (distance > shootingRange)
        {
            ClearStrafeDestination();
            ChaseToShootingRange(target);
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            ClearStrafeDestination();
            enemyMovement.Stop();
            archerCombat.TryStartRangedAttack();
            return;
        }

        StrafeAroundTarget(target);
    }

    private void RetreatFromTarget(Transform target)
    {
        enemyCore.EnterReposition();

        Vector3 awayDirection = GetAwayDirection(target);
        Vector3 desiredPosition = target.position + awayDirection * retreatStopRange;

        if (!enemyMovement.MoveTo(desiredPosition, 0.05f))
            enemyMovement.Stop();
    }

    private void ChaseToShootingRange(Transform target)
    {
        enemyCore.EnterChase();

        if (!enemyMovement.MoveTo(target.position, shootingRange * 0.85f))
            enemyMovement.Stop();
    }

    private void StrafeAroundTarget(Transform target)
    {
        enemyCore.EnterReposition();

        if (ShouldChooseNewStrafeDestination())
            TryChooseStrafeDestination(target);

        if (!hasStrafeDestination)
            enemyMovement.Stop();
    }

    private bool ShouldChooseNewStrafeDestination()
    {
        return !hasStrafeDestination || enemyMovement.HasReachedCurrentDestination(strafeReachDistance) || Time.time >= nextStrafeRefreshTime;
    }

    private void TryChooseStrafeDestination(Transform target)
    {
        Vector3 awayDirection = GetAwayDirection(target);
        Vector3 sideDirection = Vector3.Cross(Vector3.up, awayDirection) * strafeSide;
        Vector3 desiredPosition = target.position + awayDirection * strafeDistance + sideDirection * strafeSideOffset;

        hasStrafeDestination = enemyMovement.MoveTo(desiredPosition, 0.05f, strafeSpeedMultiplier);
        nextStrafeRefreshTime = Time.time + strafeRefreshInterval;
    }

    private Vector3 GetAwayDirection(Transform target)
    {
        Vector3 awayDirection = transform.position - target.position;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.01f)
            return -transform.forward;

        return awayDirection.normalized;
    }

    private void ClearStrafeDestination()
    {
        hasStrafeDestination = false;
        nextStrafeRefreshTime = 0f;
    }
}
