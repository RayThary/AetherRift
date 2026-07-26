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
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float chaseResumeRange = 2.3f;
    [SerializeField] private float tooCloseRange = 0.9f;
    [SerializeField] private float retreatStopRange = 1.4f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.2f;
    [Range(0f, 1f)]
    [SerializeField] private float heavyAttackChance = 0.3f;

    [Header("Reposition")]
    [SerializeField] private float repositionRadius = 1.7f;
    [SerializeField] private float repositionAngle = 75f;
    [SerializeField] private float repositionSampleDistance = 1.5f;
    [SerializeField] private float repositionReachDistance = 0.2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation")]
    [SerializeField] private float locomotionDampTime = 0.08f;

    private NavMeshAgent agent;
    private EnemyCore enemyCore;
    private EnemyCombat enemyCombat;

    private float nextAttackTime;

    private bool wasAttacking;
    private bool isRetreating;
    private bool hasRepositionTarget;

    private Vector3 repositionTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyCore = GetComponent<EnemyCore>();
        enemyCombat = GetComponent<EnemyCombat>();

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
            hasRepositionTarget = false;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > detectionRange)
        {
            isRetreating = false;
            hasRepositionTarget = false;

            enemyCore.EnterIdle();
            StopMoving();
            return;
        }

        RotateToTarget();

        if (isRetreating)
        {
            if (distance >= retreatStopRange)
            {
                isRetreating = false;
                hasRepositionTarget = false;
            }
            else
            {
                RetreatFromTarget();
                return;
            }
        }

        if (distance <= tooCloseRange)
        {
            isRetreating = true;
            hasRepositionTarget = false;

            RetreatFromTarget();
            return;
        }

        if (Time.time < nextAttackTime)
        {
            HandleCooldownMovement(distance);
            return;
        }

        hasRepositionTarget = false;

        if (distance <= attackRange)
        {
            enemyCore.EnterIdle();
            StopMoving();
            TryAttack();
            return;
        }

        if (distance >= chaseResumeRange)
        {
            ChaseTarget();
            return;
        }

        enemyCore.EnterIdle();
        StopMoving();
    }

    private void HandleCooldownMovement(float distance)
    {
        if (distance >= chaseResumeRange)
        {
            hasRepositionTarget = false;
            ChaseTarget();
            return;
        }

        RepositionAroundTarget();
    }

    private void TryAttack()
    {
        bool useHeavyAttack = Random.value < heavyAttackChance;

        if (useHeavyAttack)
            enemyCombat.TryStartHeavyAttack();
        else
            enemyCombat.TryStartLightAttack();
    }

    private void ChaseTarget()
    {
        if (!agent.isOnNavMesh)
            return;

        enemyCore.EnterChase();

        agent.isStopped = false;
        agent.SetDestination(target.position);

        UpdateLocomotionAnimation();
    }

    private void RetreatFromTarget()
    {
        if (!agent.isOnNavMesh)
            return;

        enemyCore.EnterReposition();

        Vector3 awayDirection = transform.position - target.position;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.01f)
            awayDirection = -transform.forward;

        awayDirection.Normalize();

        Vector3 desiredPosition = target.position + awayDirection * retreatStopRange;

        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, repositionSampleDistance, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }

        UpdateLocomotionAnimation();
    }

    private void RepositionAroundTarget()
    {
        if (!agent.isOnNavMesh)
            return;

        enemyCore.EnterReposition();

        if (!hasRepositionTarget || HasReachedRepositionTarget())
            ChooseRepositionTarget();

        if (!hasRepositionTarget)
        {
            StopMoving();
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(repositionTarget);

        UpdateLocomotionAnimation();
    }

    private void ChooseRepositionTarget()
    {
        Vector3 awayDirection = transform.position - target.position;
        awayDirection.y = 0f;

        if (awayDirection.sqrMagnitude < 0.01f)
            awayDirection = -target.forward;

        awayDirection.Normalize();

        float side = Random.value < 0.5f ? -1f : 1f;
        Vector3 repositionDirection = Quaternion.AngleAxis(repositionAngle * side, Vector3.up) * awayDirection;
        Vector3 desiredPosition = target.position + repositionDirection * repositionRadius;

        if (!NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, repositionSampleDistance, NavMesh.AllAreas))
        {
            hasRepositionTarget = false;
            return;
        }

        repositionTarget = hit.position;
        hasRepositionTarget = true;
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
