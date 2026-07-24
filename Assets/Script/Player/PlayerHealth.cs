using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    [Header("Reference")]
    [SerializeField] private Animator animator;

    [Header("Knockback")]
    [SerializeField] private float lightKnockbackDuration = 0.08f;
    [SerializeField] private float heavyKnockbackDuration = 0.4f;

    private PlayerCore playerCore;
    private IAttackStateProvider attackStateProvider;

    [Header("CurrentHealth")]
    [SerializeField]    private int currentHealth;

    private bool isHitReacting;
    private bool hasEnteredHitState;

    private HitImpact currentHitReaction = HitImpact.None;
    private Vector3 knockbackDirection;
    private float currentKnockbackDistance;
    private float currentKnockbackDuration;
    private float remainingKnockbackTime;

    private void Awake()
    {
        playerCore = GetComponent<PlayerCore>();
        attackStateProvider = GetComponent<IAttackStateProvider>();

        if (animator == null)
            animator = GetComponent<Animator>();

        currentHealth = maxHealth;
    }

    private void Update()
    {
        UpdateHitState();
        UpdateKnockback();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        currentHealth -= damageInfo.Damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
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

        StartHitReaction(resolvedReaction, damageInfo.HitDirection, damageInfo.KnockbackDistance);
    }

    private void StartHitReaction(HitImpact reaction, Vector3 hitDirection, float knockbackDistance)
    {
        currentHitReaction = reaction;

        knockbackDirection = hitDirection;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude > 0.01f)
            knockbackDirection.Normalize();

        currentKnockbackDistance = Mathf.Max(knockbackDistance, 0f);
        currentKnockbackDuration = reaction == HitImpact.Light ? lightKnockbackDuration : heavyKnockbackDuration;
        currentKnockbackDuration = Mathf.Max(currentKnockbackDuration, 0.01f);

        remainingKnockbackTime = currentKnockbackDistance > 0f ? currentKnockbackDuration : 0f;

        isHitReacting = true;
        hasEnteredHitState = false;

        playerCore.EnterHit();

        ResetHitTriggers();

        if (currentHitReaction == HitImpact.Light)
            animator.SetTrigger("LightHit");
        else
            animator.SetTrigger("HeavyHit");
    }

    private void UpdateHitState()
    {
        if (!isHitReacting)
            return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (!hasEnteredHitState)
        {
            if (currentState.IsTag("Hit"))
                hasEnteredHitState = true;

            return;
        }

        if (IsAnimatorHit())
            return;

        EndHitReaction();
    }

    private bool IsAnimatorHit()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsTag("Hit"))
            return true;

        if (!animator.IsInTransition(0))
            return false;

        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        return nextState.IsTag("Hit");
    }

    private void UpdateKnockback()
    {
        if (!isHitReacting || !hasEnteredHitState || remainingKnockbackTime <= 0f)
            return;

        float moveTime = Mathf.Min(Time.deltaTime, remainingKnockbackTime);
        float knockbackSpeed = currentKnockbackDistance / currentKnockbackDuration;

        transform.position += knockbackDirection * knockbackSpeed * moveTime;
        remainingKnockbackTime -= moveTime;
    }

    private void EndHitReaction()
    {
        isHitReacting = false;
        hasEnteredHitState = false;

        currentHitReaction = HitImpact.None;
        currentKnockbackDistance = 0f;
        remainingKnockbackTime = 0f;

        ResetHitTriggers();
        playerCore.ExitHit();
    }

    private void ResetHitTriggers()
    {
        animator.ResetTrigger("LightHit");
        animator.ResetTrigger("HeavyHit");
    }

    private void Die()
    {
        gameObject.SetActive(false);
    }
}