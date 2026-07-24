using UnityEngine;

public enum EnemyWeightClass
{
    Normal,
    Heavy,
    Boss
}

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Reaction")]
    [SerializeField] private EnemyWeightClass weightClass = EnemyWeightClass.Normal;
    [SerializeField] private Animator animator;

    [Header("Weight Multipliers")]
    [SerializeField] private float normalKnockbackMultiplier = 1f;
    [SerializeField] private float heavyKnockbackMultiplier = 0.4f;
    [SerializeField] private float bossKnockbackMultiplier;

    [Header("Knockback")]
    [SerializeField] private float lightKnockbackDuration = 0.08f;
    [SerializeField] private float heavyKnockbackDuration = 0.4f;

    private IAttackStateProvider attackStateProvider;

    private int currentHealth;

    private Vector3 knockbackDirection;

    private float currentKnockbackDistance;
    private float currentKnockbackDuration;
    private float remainingKnockbackTime;

    private HitImpact currentHitReaction = HitImpact.None;

    private bool isHitReacting;
    private bool hasEnteredHitState;
    private bool isDead;

    public bool CanAct => !isHitReacting && !isDead;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        attackStateProvider = GetComponent<IAttackStateProvider>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("[EnemyHealth] Animator를 찾을 수 없습니다.");
    }

    private void Update()
    {
        if (!isHitReacting || isDead)
            return;

        UpdateHitState();
        UpdateKnockback();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (damageInfo.Damage <= 0 || isDead)
            return;

        currentHealth = Mathf.Max(currentHealth - damageInfo.Damage, 0);

        Debug.Log($"[EnemyHealth] {gameObject.name} 피해: {damageInfo.Damage}, 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        ApplyHitReaction(damageInfo);
    }

    private void ApplyHitReaction(DamageInfo damageInfo)
    {
        if (isHitReacting)
            return;

        HitImpact currentAttackImpact = attackStateProvider != null ? attackStateProvider.CurrentAttackImpact : HitImpact.None;
        HitImpact resolvedReaction = HitReactionResolver.Resolve(damageInfo.Impact, currentAttackImpact);

        if (resolvedReaction == HitImpact.None)
            return;

        if (attackStateProvider != null)
            attackStateProvider.CancelAttackForHit();

        float knockbackDistance = damageInfo.KnockbackDistance * GetKnockbackMultiplier();

        StartHitReaction(resolvedReaction, damageInfo.HitDirection, knockbackDistance);
    }

    private float GetKnockbackMultiplier()
    {
        switch (weightClass)
        {
            case EnemyWeightClass.Normal:
                return Mathf.Max(normalKnockbackMultiplier, 0f);

            case EnemyWeightClass.Heavy:
                return Mathf.Max(heavyKnockbackMultiplier, 0f);

            case EnemyWeightClass.Boss:
                return Mathf.Max(bossKnockbackMultiplier, 0f);

            default:
                return 0f;
        }
    }

    private void StartHitReaction(HitImpact reaction, Vector3 hitDirection, float knockbackDistance)
    {
        if (animator == null)
            return;

        currentHitReaction = reaction;
        currentKnockbackDistance = Mathf.Max(knockbackDistance, 0f);

        knockbackDirection = hitDirection;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude <= 0.01f)
            knockbackDirection = -transform.forward;

        knockbackDirection.Normalize();

        float knockbackDuration = reaction == HitImpact.Light ? lightKnockbackDuration : heavyKnockbackDuration;

        currentKnockbackDuration = Mathf.Max(knockbackDuration, 0.01f);
        remainingKnockbackTime = currentKnockbackDistance > 0f ? currentKnockbackDuration : 0f;

        isHitReacting = true;
        hasEnteredHitState = false;

        ResetHitTriggers();

        if (currentHitReaction == HitImpact.Light)
            animator.SetTrigger("LightHit");
        else
            animator.SetTrigger("HeavyHit");
    }

    private void UpdateHitState()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (!hasEnteredHitState)
        {
            if (currentState.IsTag("Hit"))
                hasEnteredHitState = true;

            return;
        }

        if (IsAnimatorInHitState())
            return;

        EndHitReaction();
    }

    private void UpdateKnockback()
    {
        if (!hasEnteredHitState || remainingKnockbackTime <= 0f)
            return;

        float moveTime = Mathf.Min(Time.deltaTime, remainingKnockbackTime);
        float knockbackSpeed = currentKnockbackDistance / currentKnockbackDuration;

        transform.position += knockbackDirection * knockbackSpeed * moveTime;
        remainingKnockbackTime -= moveTime;
    }

    private bool IsAnimatorInHitState()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsTag("Hit"))
            return true;

        if (!animator.IsInTransition(0))
            return false;

        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        return nextState.IsTag("Hit");
    }

    private void EndHitReaction()
    {
        isHitReacting = false;
        hasEnteredHitState = false;

        currentHitReaction = HitImpact.None;
        currentKnockbackDistance = 0f;
        currentKnockbackDuration = 0f;
        remainingKnockbackTime = 0f;

        ResetHitTriggers();
    }

    private void ResetHitTriggers()
    {
        animator.ResetTrigger("LightHit");
        animator.ResetTrigger("HeavyHit");
    }

    private void Die()
    {
        isDead = true;
        isHitReacting = false;
        hasEnteredHitState = false;

        currentHitReaction = HitImpact.None;
        currentKnockbackDistance = 0f;
        currentKnockbackDuration = 0f;
        remainingKnockbackTime = 0f;

        if (animator != null)
            ResetHitTriggers();

        gameObject.SetActive(false);
    }
}
