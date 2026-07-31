using UnityEngine;
using UnityEngine.AI;

public class MeleeEnemyCombat : MonoBehaviour, IAttackStateProvider
{
    [Header("Reference")]
    [SerializeField] private Animator animator;

    [Header("Light Attack")]
    [SerializeField] private int lightFirstAttackDamage = 10;
    [SerializeField] private HitImpact lightFirstAttackImpact = HitImpact.None;
    [SerializeField] private float lightFirstAttackKnockbackDistance;

    [SerializeField] private int lightSecondAttackDamage = 12;
    [SerializeField] private HitImpact lightSecondAttackImpact = HitImpact.Light;
    [SerializeField] private float lightSecondAttackKnockbackDistance = 0.15f;

    [Header("Heavy Attack")]
    [SerializeField] private int heavyFirstAttackDamage = 20;
    [SerializeField] private HitImpact heavyFirstAttackImpact = HitImpact.Middle;
    [SerializeField] private float heavyFirstAttackKnockbackDistance = 0.2f;

    [SerializeField] private int heavySecondAttackDamage = 30;
    [SerializeField] private HitImpact heavySecondAttackImpact = HitImpact.Heavy;
    [SerializeField] private float heavySecondAttackKnockbackDistance = 0.8f;

    [Header("Attack Movement")]
    [SerializeField, Range(0f, 1f)] private float attackMoveStartTime = 0.15f;
    [SerializeField] private float lightAttackMoveDistance = 0.35f;
    [SerializeField] private float lightAttackMoveDuration = 0.12f;
    [SerializeField] private float heavyAttackMoveDistance = 0.5f;
    [SerializeField] private float heavyAttackMoveDuration = 0.16f;

    private EnemyCore enemyCore;
    private NavMeshAgent agent;

    private Vector3 attackMoveDirection;

    private float currentAttackMoveDistance;
    private float currentAttackMoveDuration;
    private float remainingAttackMoveTime;

    private bool isAttacking;
    private bool hasEnteredAttackState;
    private bool isAttackMoving;

    public bool IsAttacking => isAttacking;
    public int CurrentAttackDamage { get; private set; }
    public HitImpact CurrentAttackImpact { get; private set; } = HitImpact.None;
    public float CurrentAttackKnockbackDistance { get; private set; }

    private void Awake()
    {
        enemyCore = GetComponent<EnemyCore>();
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        UpdateAttackState();
        UpdateAttackMovement();
    }

    public bool TryStartLightAttack()
    {
        if (isAttacking || !enemyCore.TryEnterAttack())
            return false;

        isAttacking = true;
        hasEnteredAttackState = false;

        SetCurrentAttack(lightFirstAttackDamage, lightFirstAttackImpact, lightFirstAttackKnockbackDistance);
        PrepareAttackMovement(lightAttackMoveDistance, lightAttackMoveDuration);

        ResetAttackTriggers();
        animator.SetTrigger("LightAttack");

        return true;
    }

    public bool TryStartHeavyAttack()
    {
        if (isAttacking || !enemyCore.TryEnterAttack())
            return false;

        isAttacking = true;
        hasEnteredAttackState = false;

        SetCurrentAttack(heavyFirstAttackDamage, heavyFirstAttackImpact, heavyFirstAttackKnockbackDistance);
        PrepareAttackMovement(heavyAttackMoveDistance, heavyAttackMoveDuration);

        ResetAttackTriggers();
        animator.SetTrigger("HeavyAttack");

        return true;
    }

    public void SetLightSecondAttack()
    {
        if (!isAttacking)
            return;

        SetCurrentAttack(lightSecondAttackDamage, lightSecondAttackImpact, lightSecondAttackKnockbackDistance);
    }

    public void SetHeavySecondAttack()
    {
        if (!isAttacking)
            return;

        SetCurrentAttack(heavySecondAttackDamage, heavySecondAttackImpact, heavySecondAttackKnockbackDistance);
    }

    private void SetCurrentAttack(int damage, HitImpact impact, float knockbackDistance)
    {
        CurrentAttackDamage = Mathf.Max(damage, 0);
        CurrentAttackImpact = impact;
        CurrentAttackKnockbackDistance = Mathf.Max(knockbackDistance, 0f);
    }

    private void PrepareAttackMovement(float distance, float duration)
    {
        attackMoveDirection = transform.forward;
        attackMoveDirection.y = 0f;

        if (attackMoveDirection.sqrMagnitude > 0.01f)
            attackMoveDirection.Normalize();

        currentAttackMoveDistance = Mathf.Max(distance, 0f);
        currentAttackMoveDuration = Mathf.Max(duration, 0.01f);
        remainingAttackMoveTime = currentAttackMoveDuration;
        isAttackMoving = currentAttackMoveDistance > 0f && attackMoveDirection.sqrMagnitude > 0.01f;
    }

    private void UpdateAttackMovement()
    {
        if (!isAttackMoving || !hasEnteredAttackState)
            return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (!currentState.IsTag("Attack") || currentState.normalizedTime < attackMoveStartTime)
            return;

        if (!isAttacking)
        {
            StopAttackMovement();
            return;
        }

        float moveTime = Mathf.Min(Time.deltaTime, remainingAttackMoveTime);
        float moveSpeed = currentAttackMoveDistance / currentAttackMoveDuration;
        Vector3 movement = attackMoveDirection * moveSpeed * moveTime;

        if (agent != null && agent.isOnNavMesh)
            agent.Move(movement);
        else
            transform.position += movement;

        remainingAttackMoveTime -= moveTime;

        if (remainingAttackMoveTime <= 0f)
            StopAttackMovement();
    }

    private void StopAttackMovement()
    {
        isAttackMoving = false;
        currentAttackMoveDistance = 0f;
        currentAttackMoveDuration = 0f;
        remainingAttackMoveTime = 0f;
    }

    private void UpdateAttackState()
    {
        if (!isAttacking)
            return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (!hasEnteredAttackState)
        {
            if (currentState.IsTag("Attack"))
                hasEnteredAttackState = true;

            return;
        }

        if (IsAnimatorAttacking())
            return;

        EndAttack();
    }

    private bool IsAnimatorAttacking()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsTag("Attack"))
            return true;

        if (!animator.IsInTransition(0))
            return false;

        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        return nextState.IsTag("Attack");
    }

    public void CancelAttackForHit()
    {
        if (!isAttacking)
            return;

        ClearAttackState();
        ResetAttackTriggers();
        enemyCore.ExitAttack();
    }

    private void EndAttack()
    {
        ClearAttackState();
        ResetAttackTriggers();
        enemyCore.ExitAttack();
    }

    private void ClearAttackState()
    {
        isAttacking = false;
        hasEnteredAttackState = false;
        SetCurrentAttack(0, HitImpact.None, 0f);
        StopAttackMovement();
    }

    private void ResetAttackTriggers()
    {
        animator.ResetTrigger("LightAttack");
        animator.ResetTrigger("HeavyAttack");
    }
}
