using System;
using UnityEngine;
using UnityEngine.AI;

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

    [Header("Weight")]
    [SerializeField] private EnemyWeightClass weightClass = EnemyWeightClass.Normal;

    [Header("Reference")]
    [SerializeField] private Animator animator;

    [Header("Knockback")]
    [SerializeField] private float lightKnockbackDuration = 0.08f;
    [SerializeField] private float middleKnockbackDuration = 0.2f;
    [SerializeField] private float heavyKnockbackDuration = 0.4f;

    private EnemyCore enemyCore;
    private IAttackStateProvider attackStateProvider;
    private NavMeshAgent agent;

    private int currentHealth;

    private bool isHitReacting;
    private bool hasEnteredHitState;

    private HitImpact currentHitReaction = HitImpact.None;
    private Vector3 knockbackDirection;
    private float currentKnockbackDistance;
    private float currentKnockbackDuration;
    private float remainingKnockbackTime;

    public event Action<EnemyHealth> Died;

    public bool IsDead => enemyCore != null && enemyCore.IsDead;

    private void Awake()
    {
        enemyCore = GetComponent<EnemyCore>();
        attackStateProvider = GetComponent<IAttackStateProvider>();
        agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
    }

    private void Update()
    {
        UpdateHitState();
        UpdateKnockback();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (IsDead || IsInvincible())
            return;

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

        float knockbackDistance = damageInfo.KnockbackDistance * GetKnockbackMultiplier();
        StartHitReaction(resolvedReaction, damageInfo.HitDirection, knockbackDistance);
    }

    private void StartHitReaction(HitImpact reaction, Vector3 hitDirection, float knockbackDistance)
    {
        currentHitReaction = reaction;

        knockbackDirection = hitDirection;
        knockbackDirection.y = 0f;

        if (knockbackDirection.sqrMagnitude > 0.01f)
            knockbackDirection.Normalize();

        currentKnockbackDistance = Mathf.Max(knockbackDistance, 0f);
        currentKnockbackDuration = reaction switch
        {
            HitImpact.Light => lightKnockbackDuration,
            HitImpact.Middle => middleKnockbackDuration,
            _ => heavyKnockbackDuration
        };
        currentKnockbackDuration = Mathf.Max(currentKnockbackDuration, 0.01f);

        remainingKnockbackTime = currentKnockbackDistance > 0f ? currentKnockbackDuration : 0f;

        isHitReacting = true;
        hasEnteredHitState = false;

        enemyCore.EnterHit();

        ResetHitTriggers();

        switch (currentHitReaction)
        {
            case HitImpact.Light:
                animator.SetTrigger("LightHit");
                break;

            case HitImpact.Middle:
                animator.SetTrigger("MiddleHit");
                break;

            default:
                animator.SetTrigger("HeavyHit");
                break;
        }
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
        Vector3 movement = knockbackDirection * knockbackSpeed * moveTime;

        if (agent != null && agent.isOnNavMesh)
            agent.Move(movement);
        else
            transform.position += movement;

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
        enemyCore.ExitHit();
    }

    private float GetKnockbackMultiplier()
    {
        return weightClass switch
        {
            EnemyWeightClass.Heavy => 0.4f,
            EnemyWeightClass.Boss => 0f,
            _ => 1f
        };
    }

    private void ResetHitTriggers()
    {
        animator.ResetTrigger("LightHit");
        animator.ResetTrigger("MiddleHit");
        animator.ResetTrigger("HeavyHit");
    }

    private bool IsInvincible()
    {
        return attackStateProvider != null && attackStateProvider.CurrentAttackImpact == HitImpact.Invincible;
    }

    private void Die()
    {
        if (IsDead)
            return;

        enemyCore.EnterDead();

        isHitReacting = false;
        remainingKnockbackTime = 0f;

        ResetHitTriggers();
        Died?.Invoke(this);
        gameObject.SetActive(false);
    }
}
