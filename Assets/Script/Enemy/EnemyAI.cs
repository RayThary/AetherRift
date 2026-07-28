using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(EnemyCore), typeof(EnemyCombat))]
public class EnemyAI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform target;
    [SerializeField] private Animator animator;

    [Header("Range")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2.1f;
    [SerializeField] private float tooCloseRange = 1.15f;
    [SerializeField] private float retreatStopRange = 1.8f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.2f;
    [Range(0f, 1f)]
    [SerializeField] private float heavyAttackChance = 0.3f;

    [Header("Reposition")]
    [SerializeField] private float repositionMinRadius = 2.4f;
    [SerializeField] private float repositionMaxRadius = 3.4f;
    [SerializeField] private float repositionSampleDistance = 2.5f;
    [SerializeField] private float repositionReachDistance = 0.25f;
    [SerializeField] private float repositionTargetRefreshInterval = 0.8f;
    [SerializeField] private float repositionMinimumMoveDistance = 0.9f;
    [SerializeField] private int repositionTargetAttempts = 12;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation")]
    [SerializeField] private float locomotionDampTime = 0.08f;

    private NavMeshAgent agent;
    private EnemyCore enemyCore;
    private EnemyCombat enemyCombat;
    private NavMeshPath navigationPath;

    private float nextAttackTime;
    private float nextRepositionTargetRefreshTime;

    private bool wasAttacking;
    private bool isRetreating;
    private bool hasRepositionTarget;

    private Vector3 repositionTarget;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyCore = GetComponent<EnemyCore>();
        enemyCombat = GetComponent<EnemyCombat>();
        navigationPath = new NavMeshPath();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        agent.updateRotation = false;
    }

    private void Update()
    {
        if (enemyCore.IsDead)
        {
            StopMoving();
            return;
        }

        if (target == null)
        {
            enemyCore.EnterIdle();
            StopMoving();
            return;
        }

        if (enemyCore.CurrentState == EnemyState.Hit)
        {
            StopMoving();
            return;
        }

        if (enemyCore.CurrentState == EnemyState.Attack)
        {
            wasAttacking = true;
            StopMoving();
            RotateToTarget();
            return;
        }

        if (wasAttacking)
        {
            wasAttacking = false;
            nextAttackTime = Time.time + attackCooldown;
            ClearRepositionTarget();
        }

        float distance = GetDistanceToTarget();

        if (distance > detectionRange)
        {
            isRetreating = false;
            ClearRepositionTarget();

            enemyCore.EnterIdle();
            StopMoving();
            return;
        }

        RotateToTarget();

        if (isRetreating)
        {
            if (distance < retreatStopRange)
            {
                RetreatFromTarget();
                return;
            }

            isRetreating = false;
            ClearRepositionTarget();
        }

        if (distance <= tooCloseRange)
        {
            isRetreating = true;
            ClearRepositionTarget();

            RetreatFromTarget();
            return;
        }

        if (Time.time < nextAttackTime)
        {
            RepositionAroundTarget();
            return;
        }

        ClearRepositionTarget();

        if (distance <= attackRange)
        {
            StopMoving();
            TryAttack();
            return;
        }

        ChaseTarget();
    }

    private float GetDistanceToTarget()
    {
        Vector3 offset = target.position - transform.position;
        offset.y = 0f;

        return offset.magnitude;
    }

    private void TryAttack()
    {
        if (Random.value < heavyAttackChance)
            enemyCombat.TryStartHeavyAttack();
        else
            enemyCombat.TryStartLightAttack();
    }

    private void ChaseTarget()
    {
        enemyCore.EnterChase();
        TrySetDestination(target.position, attackRange * 0.9f);
    }

    private void RetreatFromTarget()
    {
        enemyCore.EnterReposition();

        Vector3 awayDirection = GetDirectionAwayFromTarget();
        Vector3 desiredPosition = target.position + awayDirection * retreatStopRange;

        if (TrySetDestination(desiredPosition, 0.05f))
            return;

        MoveDirectly(awayDirection);
    }

    private void RepositionAroundTarget()
    {
        enemyCore.EnterReposition();

        if (ShouldChooseNewRepositionTarget())
            TryChooseRepositionTarget();

        if (hasRepositionTarget && TrySetDestination(repositionTarget, 0.05f))
            return;

        TryMoveToFallbackRepositionPosition();
    }

    private bool ShouldChooseNewRepositionTarget()
    {
        return !hasRepositionTarget || HasReachedRepositionTarget() || Time.time >= nextRepositionTargetRefreshTime;
    }

    private void TryChooseRepositionTarget()
    {
        hasRepositionTarget = false;

        float minimumRadius = Mathf.Max(repositionMinRadius, retreatStopRange);
        float maximumRadius = Mathf.Max(minimumRadius, repositionMaxRadius);

        for (int attempt = 0; attempt < repositionTargetAttempts; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float radius = Random.Range(minimumRadius, maximumRadius);
            Vector3 desiredPosition = target.position + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;

            if (!NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, repositionSampleDistance, NavMesh.AllAreas))
                continue;

            Vector3 movementToCandidate = hit.position - transform.position;
            movementToCandidate.y = 0f;

            if (movementToCandidate.sqrMagnitude < repositionMinimumMoveDistance * repositionMinimumMoveDistance)
                continue;

            if (!CanReachPosition(hit.position))
                continue;

            repositionTarget = hit.position;
            hasRepositionTarget = true;
            nextRepositionTargetRefreshTime = Time.time + repositionTargetRefreshInterval;
            return;
        }

        nextRepositionTargetRefreshTime = Time.time + repositionTargetRefreshInterval;
    }

    private void TryMoveToFallbackRepositionPosition()
    {
        Vector3 awayDirection = GetDirectionAwayFromTarget();
        float fallbackRadius = Mathf.Max(repositionMinRadius, retreatStopRange);
        Vector3 desiredPosition = target.position + awayDirection * fallbackRadius;

        if (TrySetDestination(desiredPosition, 0.05f))
            return;

        MoveDirectly(awayDirection);
    }

    private Vector3 GetDirectionAwayFromTarget()
    {
        Vector3 awayDirection = transform.position - target.position;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.01f)
            return -transform.forward;

        return awayDirection.normalized;
    }

    private bool TrySetDestination(Vector3 destination, float stoppingDistance)
    {
        if (!agent.isOnNavMesh || !CanReachPosition(destination))
            return false;

        agent.isStopped = false;
        agent.stoppingDistance = stoppingDistance;
        agent.SetDestination(destination);

        UpdateLocomotionAnimation();
        return true;
    }

    private bool CanReachPosition(Vector3 destination)
    {
        return agent.isOnNavMesh && agent.CalculatePath(destination, navigationPath) && navigationPath.status == NavMeshPathStatus.PathComplete;
    }

    private void MoveDirectly(Vector3 direction)
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.ResetPath();
        agent.Move(direction * agent.speed * Time.deltaTime);

        Vector3 localDirection = transform.InverseTransformDirection(direction);
        SetLocomotion(true, localDirection.x, localDirection.z);
    }

    private void ClearRepositionTarget()
    {
        hasRepositionTarget = false;
        nextRepositionTargetRefreshTime = 0f;
    }

    private bool HasReachedRepositionTarget()
    {
        Vector3 direction = repositionTarget - transform.position;
        direction.y = 0f;

        return direction.sqrMagnitude <= repositionReachDistance * repositionReachDistance;
    }

    private void StopMoving()
    {
        SetLocomotion(false, 0f, 0f);

        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private void UpdateLocomotionAnimation()
    {
        if (animator == null)
            return;

        Vector3 movementDirection = agent.desiredVelocity;
        movementDirection.y = 0f;

        if (movementDirection.sqrMagnitude < 0.01f)
        {
            SetLocomotion(false, 0f, 0f);
            return;
        }

        movementDirection.Normalize();

        Vector3 localDirection = transform.InverseTransformDirection(movementDirection);
        SetLocomotion(true, localDirection.x, localDirection.z);
    }

    private void SetLocomotion(bool isMoving, float x, float y)
    {
        if (animator == null)
            return;

        animator.SetFloat("Locomotion", isMoving ? 1f : 0f);
        animator.SetFloat("LocomotionX", x, locomotionDampTime, Time.deltaTime);
        animator.SetFloat("LocomotionY", y, locomotionDampTime, Time.deltaTime);
    }

    private void RotateToTarget()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
