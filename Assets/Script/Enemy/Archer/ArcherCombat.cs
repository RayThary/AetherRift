using UnityEngine;

public class ArcherCombat : MonoBehaviour, IAttackStateProvider
{
    private enum AttackType
    {
        None,
        Ranged,
        Melee
    }

    [Header("Reference")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform arrowSpawnPoint;
    [SerializeField] private ArrowProjectile arrowPrefab;

    [Header("Animation")]
    [SerializeField] private string rangedAttackTrigger = "LightAttack";
    [SerializeField] private string meleeAttackTrigger = "HeavyAttack";

    [Header("Arrow Attack")]
    [SerializeField] private int arrowDamage = 8;
    [SerializeField] private HitImpact arrowImpact = HitImpact.Light;
    [SerializeField] private float arrowKnockbackDistance = 0.1f;
    [SerializeField] private float targetAimHeight = 1f;

    private EnemyCore enemyCore;
    private EnemyTarget enemyTarget;
    private ArrowProjectile preparedArrow;

    private AttackType currentAttackType;
    private bool isAttacking;
    private bool hasEnteredAttackState;
    private bool hasReleasedArrow;

    public HitImpact CurrentAttackImpact => HitImpact.None;

    private void Awake()
    {
        enemyCore = GetComponent<EnemyCore>();
        enemyTarget = GetComponent<EnemyTarget>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        UpdateAttackState();
    }

    private void OnDisable()
    {
        ClearAttackState();
    }

    public bool TryStartRangedAttack()
    {
        if (!CanStartAttack())
            return false;

        if (arrowPrefab == null || arrowSpawnPoint == null)
        {
            enemyCore.ExitAttack();
            Debug.LogWarning("[ArcherCombat] Arrow Prefab 또는 Arrow Spawn Point가 비어 있습니다.", this);
            return false;
        }

        PrepareArrow();
        StartAttack(AttackType.Ranged, rangedAttackTrigger);

        return true;
    }

    public bool TryStartMeleeAttack()
    {
        if (!CanStartAttack())
            return false;

        StartAttack(AttackType.Melee, meleeAttackTrigger);

        return true;
    }

    public void ReleaseArrow()
    {
        if (!isAttacking || currentAttackType != AttackType.Ranged || hasReleasedArrow || preparedArrow == null)
            return;

        hasReleasedArrow = true;

        Vector3 direction = GetArrowDirection();
        ArrowProjectile arrowToRelease = preparedArrow;
        preparedArrow = null;

        arrowToRelease.Launch(direction, transform, arrowDamage, arrowImpact, arrowKnockbackDistance);
    }

    public void CancelAttackForHit()
    {
        if (!isAttacking)
            return;

        ResetAttackTriggers();
        ClearAttackState();
        enemyCore.ExitAttack();
    }

    private bool CanStartAttack()
    {
        if (isAttacking || !enemyCore.TryEnterAttack())
            return false;

        if (animator != null)
            return true;

        enemyCore.ExitAttack();
        Debug.LogWarning("[ArcherCombat] Animator가 비어 있습니다.", this);

        return false;
    }

    private void PrepareArrow()
    {
        DestroyPreparedArrow();

        preparedArrow = Instantiate(arrowPrefab);
        preparedArrow.Prepare(arrowSpawnPoint);
    }

    private void StartAttack(AttackType attackType, string triggerName)
    {
        currentAttackType = attackType;
        isAttacking = true;
        hasEnteredAttackState = false;
        hasReleasedArrow = false;

        ResetAttackTriggers();
        animator.SetTrigger(triggerName);
    }

    private Vector3 GetArrowDirection()
    {
        Transform target = enemyTarget.Target;

        if (target == null)
            return transform.forward;

        Vector3 aimPosition = target.position + Vector3.up * targetAimHeight;
        Vector3 direction = aimPosition - arrowSpawnPoint.position;

        if (direction.sqrMagnitude < 0.01f)
            return transform.forward;

        return direction.normalized;
    }

    private void UpdateAttackState()
    {
        if (!isAttacking || animator == null)
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

    private void EndAttack()
    {
        ResetAttackTriggers();
        ClearAttackState();
        enemyCore.ExitAttack();
    }

    private void ResetAttackTriggers()
    {
        if (animator == null)
            return;

        animator.ResetTrigger(rangedAttackTrigger);
        animator.ResetTrigger(meleeAttackTrigger);
    }

    private void ClearAttackState()
    {
        currentAttackType = AttackType.None;
        isAttacking = false;
        hasEnteredAttackState = false;
        hasReleasedArrow = false;

        DestroyPreparedArrow();
    }

    private void DestroyPreparedArrow()
    {
        if (preparedArrow == null)
            return;

        Destroy(preparedArrow.gameObject);
        preparedArrow = null;
    }
}
