using UnityEngine;

[RequireComponent(typeof(MeleeEnemyCombat))]
public class MeleeEnemyAI : MonoBehaviour
{
    [Header("Range")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2.1f;
    [SerializeField] private float tooCloseRange = 1f;
    [SerializeField] private float retreatStopRange = 1.6f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.2f;
    [Range(0.1f, 1f)]
    [SerializeField] private float closeRangeCooldownMultiplier = 0.65f;
    [Range(0f, 1f)]
    [SerializeField] private float heavyAttackChance = 0.3f;

    [Header("Post Attack Reposition")]
    [SerializeField] private float repositionMinRadius = 1.7f;
    [SerializeField] private float repositionMaxRadius = 2.2f;
    [SerializeField] private float repositionReachDistance = 0.25f;
    [SerializeField] private float repositionRefreshInterval = 0.7f;
    [Range(0.1f, 1f)]
    [SerializeField] private float repositionSpeedMultiplier = 0.7f;

    private EnemyCore enemyCore;
    private EnemyTarget enemyTarget;
    private EnemyMovement enemyMovement;
    private MeleeEnemyCombat enemyCombat;

    private float nextAttackTime;
    private float nextRepositionRefreshTime;

    private bool wasAttacking;
    private bool hasRepositionTarget;

    public void SetTarget(Transform newTarget)
    {
        enemyTarget.SetTarget(newTarget);
    }

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
            nextAttackTime = Time.time + GetNextAttackCooldown(target);
            ClearRepositionTarget();
        }

        float distance = enemyMovement.GetFlatDistanceTo(target.position);

        if (distance > detectionRange)
        {
            ClearRepositionTarget();
            enemyCore.EnterIdle();
            enemyMovement.Stop();
            return;
        }

        enemyMovement.RotateTo(target.position);

        if (Time.time >= nextAttackTime && distance <= attackRange)
        {
            ClearRepositionTarget();
            enemyMovement.Stop();
            TryAttack();
            return;
        }

        if (distance > attackRange)
        {
            ClearRepositionTarget();
            ChaseTarget(target);
            return;
        }

        if (distance <= tooCloseRange)
        {
            ClearRepositionTarget();
            RetreatFromTarget(target);
            return;
        }

        RepositionAroundTarget(target);
    }

    private float GetNextAttackCooldown(Transform target)
    {
        float distance = enemyMovement.GetFlatDistanceTo(target.position);

        if (distance <= tooCloseRange)
            return attackCooldown * closeRangeCooldownMultiplier;

        return attackCooldown;
    }

    private void TryAttack()
    {
        if (Random.value < heavyAttackChance)
            enemyCombat.TryStartHeavyAttack();
        else
            enemyCombat.TryStartLightAttack();
    }

    private void ChaseTarget(Transform target)
    {
        enemyCore.EnterChase();

        if (!enemyMovement.MoveTo(target.position, attackRange * 0.9f))
            enemyMovement.Stop();
    }

    private void RetreatFromTarget(Transform target)
    {
        enemyCore.EnterReposition();

        Vector3 awayDirection = GetAwayDirection(target);
        Vector3 desiredPosition = target.position + awayDirection * retreatStopRange;

        if (!enemyMovement.MoveTo(desiredPosition, 0.05f))
            enemyMovement.Stop();
    }

    private void RepositionAroundTarget(Transform target)
    {
        enemyCore.EnterReposition();

        if (ShouldChooseNewRepositionTarget())
            TryChooseRepositionTarget(target);

        if (!hasRepositionTarget)
        {
            enemyMovement.Stop();
            return;
        }

        if (enemyMovement.HasReachedCurrentDestination(repositionReachDistance))
            ClearRepositionTarget();
    }

    private bool ShouldChooseNewRepositionTarget()
    {
        return !hasRepositionTarget || enemyMovement.HasReachedCurrentDestination(repositionReachDistance) || Time.time >= nextRepositionRefreshTime;
    }

    private void TryChooseRepositionTarget(Transform target)
    {
        Vector3 awayDirection = GetAwayDirection(target);
        float side = Random.value < 0.5f ? -1f : 1f;
        float angle = Random.Range(45f, 110f) * side;
        float radius = Random.Range(repositionMinRadius, Mathf.Max(repositionMinRadius, repositionMaxRadius));
        Vector3 repositionDirection = Quaternion.AngleAxis(angle, Vector3.up) * awayDirection;
        Vector3 desiredPosition = target.position + repositionDirection * radius;

        hasRepositionTarget = enemyMovement.MoveTo(desiredPosition, 0.05f, repositionSpeedMultiplier);
        nextRepositionRefreshTime = Time.time + repositionRefreshInterval;
    }

    private Vector3 GetAwayDirection(Transform target)
    {
        Vector3 awayDirection = transform.position - target.position;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.01f)
            return -transform.forward;

        return awayDirection.normalized;
    }

    private void ClearRepositionTarget()
    {
        hasRepositionTarget = false;
        nextRepositionRefreshTime = 0f;
    }
}
