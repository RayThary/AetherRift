using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour, IAttackStateProvider
{
    [Header("Basic Combo")]
    [SerializeField, Range(0f, 1f)] private float basicComboInputStartTime = 0.4f;
    [SerializeField, Range(0f, 1f)] private float basicComboExecuteTime = 0.6f;
    [SerializeField, Range(0f, 1f)] private float basicComboInputEndTime = 0.95f;

    [Header("Basic Attack")]
    [SerializeField] private int basicFirstAttackDamage = 10;
    [SerializeField] private HitImpact basicFirstAttackImpact = HitImpact.None;
    [SerializeField] private float basicFirstAttackKnockbackDistance = 0;

    [SerializeField] private int basicSecondAttackDamage = 12;
    [SerializeField] private HitImpact basicSecondAttackImpact = HitImpact.Light;
    [SerializeField] private float basicSecondAttackKnockbackDistance = 0.15f;

    [Header("Heavy Combo")]
    [SerializeField, Range(0f, 1f)] private float heavyComboInputStartTime = 0.4f;
    [SerializeField, Range(0f, 1f)] private float heavyComboExecuteTime = 0.6f;
    [SerializeField, Range(0f, 1f)] private float heavyComboInputEndTime = 0.95f;

    [Header("Heavy Attack")]
    [SerializeField] private int heavyFirstAttackDamage = 20;
    [SerializeField] private HitImpact heavyFirstAttackImpact = HitImpact.Middle;
    [SerializeField] private float heavyFirstAttackKnockbackDistance = 0.2f;

    [SerializeField] private int heavySecondAttackDamage = 30;
    [SerializeField] private HitImpact heavySecondAttackImpact = HitImpact.Heavy;
    [SerializeField] private float heavySecondAttackKnockbackDistance = 0.8f;

    [Header("Basic Second Attack Movement")]
    [SerializeField, Range(0f, 1f)] private float basicSecondAttackMoveStartTime = 0.25f;
    [SerializeField] private float basicSecondAttackMoveDistance = 0.5f;
    [SerializeField] private float basicSecondAttackMoveDuration = 0.15f;

    [Header("Heavy Second Attack Movement")]
    [SerializeField, Range(0f, 1f)] private float heavySecondAttackMoveStartTime = 0.25f;
    [SerializeField] private float heavySecondAttackMoveDistance = 0.8f;
    [SerializeField] private float heavySecondAttackMoveDuration = 0.2f;

    [Header("Attack State")]
    [SerializeField] private float attackStateCheckDelay = 0.1f;

    [Header("Attack Rotation")]
    [SerializeField] private float attackRotationDuration = 0.12f;

    private PlayerCore playerCore;
    private PlayerMovement playerMovement;
    private PlayerAttackHitbox playerAttackHitbox;
    private Animator animator;

    private Vector3 attackMoveDirection;

    private Quaternion attackStartRotation;
    private Quaternion attackTargetRotation;

    private float remainingAttackMoveTime;
    private float currentAttackMoveDuration;
    private float currentAttackMoveDistance;
    private float currentAttackStateCheckDelay;
    private float attackRotationElapsedTime;

    private float pendingAttackMoveStartTime;
    private float pendingAttackMoveDistance;
    private float pendingAttackMoveDuration;

    private int pendingSourceAttackStateHash;
    private int basicAttackStep;

    private bool isAttacking;
    private bool isBasicAttacking;
    private bool isHeavyAttacking;

    private bool isAttackMoving;
    private bool isAttackMovementPending;

    private bool isNextBasicAttackBuffered;
    private bool isNextHeavyAttackBuffered;
    private bool hasUsedHeavySecondAttack;
    private int attackPowerBonus;

    public int CurrentAttackDamage { get; private set; }
    public HitImpact CurrentAttackImpact { get; private set; } = HitImpact.None;
    public float CurrentAttackKnockbackDistance { get; private set; }

    public void Initialize(PlayerCore core)
    {
        playerCore = core;
        playerMovement = GetComponent<PlayerMovement>();
        playerAttackHitbox = GetComponent<PlayerAttackHitbox>();
        animator = core.Animator;
    }

    public void SetAttackPowerBonus(int value)
    {
        attackPowerBonus = value;
    }

    private void Update()
    {
        if (playerCore == null)
            return;

        if (playerCore.IsPlayerPaused)
            return;

        UpdateAttackState();
        HandleAttackInput();
        UpdateBufferedBasicCombo();
        UpdateBufferedHeavyCombo();
        UpdateAttackRotation();
        UpdatePendingAttackMovement();
        UpdateAttackMovement();
    }

    private void HandleAttackInput()
    {
        if (Mouse.current == null)
            return;

        bool basicAttackPressed = Mouse.current.leftButton.wasPressedThisFrame;
        bool heavyAttackPressed = Mouse.current.rightButton.wasPressedThisFrame;

        if (!isAttacking)
        {
            if (basicAttackPressed)
            {
                StartBasicAttack();
                return;
            }

            if (heavyAttackPressed)
                StartHeavyAttack();

            return;
        }

        if (isBasicAttacking && basicAttackPressed)
        {
            BufferNextBasicAttack();
            return;
        }

        if (isHeavyAttacking && heavyAttackPressed)
            BufferNextHeavyAttack();
    }

    private void StartBasicAttack()
    {
        if (!playerCore.TryEnterAttack())
            return;

        StartAttackRotation();

        isAttacking = true;
        isBasicAttacking = true;
        isHeavyAttacking = false;

        isNextBasicAttackBuffered = false;
        isNextHeavyAttackBuffered = false;
        hasUsedHeavySecondAttack = false;

        basicAttackStep = 1;
        currentAttackStateCheckDelay = attackStateCheckDelay;

        SetCurrentAttack(basicFirstAttackDamage, basicFirstAttackImpact, basicFirstAttackKnockbackDistance);

        StopAttackMovement();
        CancelPendingAttackMovement();
        ResetAttackTriggers();

        animator.SetTrigger("BasicAttack");
    }

    private void StartHeavyAttack()
    {
        if (!playerCore.TryEnterAttack())
            return;

        StartAttackRotation();

        isAttacking = true;
        isBasicAttacking = false;
        isHeavyAttacking = true;

        isNextBasicAttackBuffered = false;
        isNextHeavyAttackBuffered = false;
        hasUsedHeavySecondAttack = false;

        basicAttackStep = 0;
        currentAttackStateCheckDelay = attackStateCheckDelay;

        SetCurrentAttack(heavyFirstAttackDamage, heavyFirstAttackImpact, heavyFirstAttackKnockbackDistance);

        StopAttackMovement();
        CancelPendingAttackMovement();
        ResetAttackTriggers();

        animator.SetTrigger("HeavyAttack");
    }

    private void BufferNextBasicAttack()
    {
        if (animator.IsInTransition(0))
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsTag("Attack"))
            return;

        float normalizedTime = stateInfo.normalizedTime;

        if (normalizedTime < basicComboInputStartTime || normalizedTime >= basicComboInputEndTime)
            return;

        isNextBasicAttackBuffered = true;
    }

    private void BufferNextHeavyAttack()
    {
        if (hasUsedHeavySecondAttack || animator.IsInTransition(0))
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsTag("Attack"))
            return;

        float normalizedTime = stateInfo.normalizedTime;

        if (normalizedTime < heavyComboInputStartTime || normalizedTime >= heavyComboInputEndTime)
            return;

        isNextHeavyAttackBuffered = true;
    }

    private void UpdateBufferedBasicCombo()
    {
        if (!isAttacking || !isBasicAttacking || !isNextBasicAttackBuffered)
            return;

        if (animator.IsInTransition(0))
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsTag("Attack"))
            return;

        float normalizedTime = stateInfo.normalizedTime;

        if (normalizedTime < basicComboExecuteTime)
            return;

        if (normalizedTime >= basicComboInputEndTime)
        {
            isNextBasicAttackBuffered = false;
            return;
        }

        isNextBasicAttackBuffered = false;

        StartAttackRotation();

        if (basicAttackStep == 1)
        {
            basicAttackStep = 2;

            SetCurrentAttack(basicSecondAttackDamage, basicSecondAttackImpact, basicSecondAttackKnockbackDistance);
            PrepareAttackMovement(stateInfo.fullPathHash, basicSecondAttackMoveStartTime, basicSecondAttackMoveDistance, basicSecondAttackMoveDuration);
        }
        else
        {
            basicAttackStep = 1;

            SetCurrentAttack(basicFirstAttackDamage, basicFirstAttackImpact, basicFirstAttackKnockbackDistance);
            CancelPendingAttackMovement();
        }

        animator.SetTrigger("NextAttack");
    }

    private void UpdateBufferedHeavyCombo()
    {
        if (!isAttacking || !isHeavyAttacking || !isNextHeavyAttackBuffered)
            return;

        if (hasUsedHeavySecondAttack || animator.IsInTransition(0))
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!stateInfo.IsTag("Attack"))
            return;

        float normalizedTime = stateInfo.normalizedTime;

        if (normalizedTime < heavyComboExecuteTime)
            return;

        if (normalizedTime >= heavyComboInputEndTime)
        {
            isNextHeavyAttackBuffered = false;
            return;
        }

        isNextHeavyAttackBuffered = false;
        hasUsedHeavySecondAttack = true;

        StartAttackRotation();

        SetCurrentAttack(heavySecondAttackDamage, heavySecondAttackImpact, heavySecondAttackKnockbackDistance);
        PrepareAttackMovement(stateInfo.fullPathHash, heavySecondAttackMoveStartTime, heavySecondAttackMoveDistance, heavySecondAttackMoveDuration);

        animator.SetTrigger("NextHeavyAttack");
    }

    private void StartAttackRotation()
    {
        if (playerMovement == null)
            return;

        Vector3 inputDirection = playerMovement.GetCameraRelativeInputDirection();

        if (inputDirection.sqrMagnitude < 0.01f)
        {
            StopAttackRotation();
            return;
        }

        inputDirection.y = 0f;
        inputDirection.Normalize();

        attackStartRotation = transform.rotation;
        attackTargetRotation = Quaternion.LookRotation(inputDirection);
        attackRotationElapsedTime = 0f;
    }

    private void UpdateAttackRotation()
    {
        if (!isAttacking || attackRotationElapsedTime >= attackRotationDuration)
            return;

        if (attackRotationDuration <= 0f)
        {
            transform.rotation = attackTargetRotation;
            attackRotationElapsedTime = attackRotationDuration;
            return;
        }

        attackRotationElapsedTime += Time.deltaTime;
        float rotationProgress = Mathf.Clamp01(attackRotationElapsedTime / attackRotationDuration);
        transform.rotation = Quaternion.Slerp(attackStartRotation, attackTargetRotation, rotationProgress);
    }

    private void StopAttackRotation()
    {
        attackRotationElapsedTime = attackRotationDuration;
    }

    private void SetCurrentAttack(int damage, HitImpact impact, float knockbackDistance)
    {
        CurrentAttackDamage = Mathf.Max(damage + attackPowerBonus, 0);
        CurrentAttackImpact = impact;
        CurrentAttackKnockbackDistance = Mathf.Max(knockbackDistance, 0f);

        if (playerAttackHitbox != null)
            playerAttackHitbox.SetAttackData(CurrentAttackDamage, CurrentAttackImpact, CurrentAttackKnockbackDistance);
    }

    private void PrepareAttackMovement(int sourceStateHash, float startTime, float distance, float duration)
    {
        pendingSourceAttackStateHash = sourceStateHash;
        pendingAttackMoveStartTime = Mathf.Clamp01(startTime);
        pendingAttackMoveDistance = Mathf.Max(distance, 0f);
        pendingAttackMoveDuration = Mathf.Max(duration, 0.01f);

        isAttackMovementPending = pendingAttackMoveDistance > 0f;
    }

    private void UpdatePendingAttackMovement()
    {
        if (!isAttackMovementPending || !isAttacking)
            return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);

            if (nextState.IsTag("Attack") && nextState.fullPathHash != pendingSourceAttackStateHash)
                stateInfo = nextState;
        }

        if (!stateInfo.IsTag("Attack"))
            return;

        if (stateInfo.fullPathHash == pendingSourceAttackStateHash)
            return;

        if (stateInfo.normalizedTime < pendingAttackMoveStartTime)
            return;

        isAttackMovementPending = false;
        StartAttackMovement(pendingAttackMoveDistance, pendingAttackMoveDuration);
    }

    private void StartAttackMovement(float distance, float duration)
    {
        attackMoveDirection = transform.forward;
        attackMoveDirection.y = 0f;

        if (attackMoveDirection.sqrMagnitude <= 0.01f)
            return;

        attackMoveDirection.Normalize();

        currentAttackMoveDistance = Mathf.Max(distance, 0f);
        currentAttackMoveDuration = Mathf.Max(duration, 0.01f);
        remainingAttackMoveTime = currentAttackMoveDuration;

        isAttackMoving = currentAttackMoveDistance > 0f;
    }

    private void UpdateAttackMovement()
    {
        if (!isAttackMoving || remainingAttackMoveTime <= 0f)
            return;

        if (!isAttacking)
        {
            StopAttackMovement();
            return;
        }

        float moveTime = Mathf.Min(Time.deltaTime, remainingAttackMoveTime);
        float moveSpeed = currentAttackMoveDistance / currentAttackMoveDuration;

        transform.position += attackMoveDirection * moveSpeed * moveTime;
        remainingAttackMoveTime -= moveTime;

        if (remainingAttackMoveTime <= 0f)
            StopAttackMovement();
    }

    private void StopAttackMovement()
    {
        isAttackMoving = false;
        remainingAttackMoveTime = 0f;
        currentAttackMoveDuration = 0f;
        currentAttackMoveDistance = 0f;
    }

    private void CancelPendingAttackMovement()
    {
        isAttackMovementPending = false;
        pendingSourceAttackStateHash = 0;
        pendingAttackMoveStartTime = 0f;
        pendingAttackMoveDistance = 0f;
        pendingAttackMoveDuration = 0f;
    }

    public void CancelAttackForDodge()
    {
        if (!isAttacking)
            return;

        ClearAttackState();
        ResetAttackTriggers();

        animator.SetTrigger("AttackCancel");
    }

    public void CancelAttackForHit()
    {
        if (!isAttacking)
            return;

        ClearAttackState();
        ResetAttackTriggers();

        playerCore.ExitAttack();
    }

    public void BeginInvincibility()
    {
        CurrentAttackImpact = HitImpact.Invincible;
    }

    public void EndInvincibility()
    {
        if (CurrentAttackImpact != HitImpact.Invincible)
            return;

        CurrentAttackImpact = HitImpact.None;
    }

    private void UpdateAttackState()
    {
        if (!isAttacking)
            return;

        if (currentAttackStateCheckDelay > 0f)
        {
            currentAttackStateCheckDelay -= Time.deltaTime;
            return;
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        bool animatorIsAttacking = currentState.IsTag("Attack");

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
            animatorIsAttacking = animatorIsAttacking || nextState.IsTag("Attack");
        }

        if (animatorIsAttacking)
            return;

        EndAttack();
    }

    private void EndAttack()
    {
        ClearAttackState();
        ResetAttackTriggers();

        playerCore.ExitAttack();
    }

    private void ClearAttackState()
    {
        isAttacking = false;
        isBasicAttacking = false;
        isHeavyAttacking = false;

        isNextBasicAttackBuffered = false;
        isNextHeavyAttackBuffered = false;
        hasUsedHeavySecondAttack = false;

        basicAttackStep = 0;

        SetCurrentAttack(0, HitImpact.None, 0f);

        StopAttackMovement();
        StopAttackRotation();
        CancelPendingAttackMovement();
    }

    private void ResetAttackTriggers()
    {
        animator.ResetTrigger("BasicAttack");
        animator.ResetTrigger("NextAttack");
        animator.ResetTrigger("HeavyAttack");
        animator.ResetTrigger("NextHeavyAttack");
        animator.ResetTrigger("AttackCancel");
    }
}
