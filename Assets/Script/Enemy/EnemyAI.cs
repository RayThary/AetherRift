using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Transform target;
    [SerializeField] private Animator animator;

    [Header("Range")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float chaseResumeRange = 2.3f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    private NavMeshAgent agent;
    private bool isChasing;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (target == null)
        {
            StopMoving();
            return;
        }

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > detectionRange)
        {
            isChasing = false;
            StopMoving();
            return;
        }

        if (isChasing)
        {
            if (distance <= attackRange)
            {
                isChasing = false;
                StopMoving();
                RotateToTarget();
                return;
            }

            ChaseTarget();
            return;
        }

        if (distance >= chaseResumeRange)
        {
            isChasing = true;
            ChaseTarget();
            return;
        }

        StopMoving();
        RotateToTarget();
    }

    private void ChaseTarget()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;
        agent.SetDestination(target.position);

        animator.SetFloat("Locomotion", 1f);
    }

    private void StopMoving()
    {
        animator.SetFloat("Locomotion", 0f);

        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
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