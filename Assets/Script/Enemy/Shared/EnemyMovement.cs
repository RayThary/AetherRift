using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Animator animator;

    [Header("Navigation")]
    [SerializeField] private float navMeshSampleDistance = 1.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Animation")]
    [SerializeField] private float locomotionDampTime = 0.08f;

    private NavMeshAgent agent;
    private NavMeshPath navigationPath;
    private float defaultMoveSpeed;

    public bool IsOnNavMesh => agent != null && agent.isOnNavMesh;
    public Vector3 CurrentDestination { get; private set; }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        navigationPath = new NavMeshPath();
        defaultMoveSpeed = agent.speed;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        agent.updateRotation = false;
    }

    private void Update()
    {
        UpdateLocomotionAnimation();
    }

    public bool MoveTo(Vector3 desiredPosition, float stoppingDistance, float speedMultiplier = 1f)
    {
        if (!IsOnNavMesh)
            return false;

        if (!NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, navMeshSampleDistance, NavMesh.AllAreas))
            return false;

        if (!agent.CalculatePath(hit.position, navigationPath) || navigationPath.status != NavMeshPathStatus.PathComplete)
            return false;

        agent.speed = defaultMoveSpeed * Mathf.Max(speedMultiplier, 0.01f);
        agent.stoppingDistance = Mathf.Max(stoppingDistance, 0f);
        agent.isStopped = false;
        agent.SetDestination(hit.position);

        CurrentDestination = hit.position;
        return true;
    }

    public void Stop()
    {
        SetLocomotion(false, 0f, 0f);

        if (!IsOnNavMesh)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    public void RotateTo(Vector3 lookPosition)
    {
        Vector3 direction = lookPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public float GetFlatDistanceTo(Vector3 position)
    {
        Vector3 offset = position - transform.position;
        offset.y = 0f;

        return offset.magnitude;
    }

    public bool HasReachedCurrentDestination(float reachDistance)
    {
        Vector3 offset = CurrentDestination - transform.position;
        offset.y = 0f;

        return offset.sqrMagnitude <= reachDistance * reachDistance;
    }

    private void UpdateLocomotionAnimation()
    {
        if (animator == null || !IsOnNavMesh || agent.isStopped)
        {
            SetLocomotion(false, 0f, 0f);
            return;
        }

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
}
