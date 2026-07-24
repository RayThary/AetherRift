using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyCombat : MonoBehaviour, IAttackStateProvider
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
    [SerializeField] private HitImpact heavyFirstAttackImpact = HitImpact.Light;
    [SerializeField] private float heavyFirstAttackKnockbackDistance = 0.2f;

    [SerializeField] private int heavySecondAttackDamage = 30;
    [SerializeField] private HitImpact heavySecondAttackImpact = HitImpact.Heavy;
    [SerializeField] private float heavySecondAttackKnockbackDistance = 0.8f;

    private bool isAttacking;
    private bool hasEnteredAttackState;

    public int CurrentAttackDamage { get; private set; }
    public HitImpact CurrentAttackImpact { get; private set; } = HitImpact.None;
    public float CurrentAttackKnockbackDistance { get; private set; }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        HandleTestInput();
        UpdateAttackState();
    }

    private void HandleTestInput()
    {
        if (Keyboard.current == null || isAttacking)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            StartLightAttack();
            return;
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            StartHeavyAttack();
    }

    private void StartLightAttack()
    {
        isAttacking = true;
        hasEnteredAttackState = false;

        SetCurrentAttack(lightFirstAttackDamage, lightFirstAttackImpact, lightFirstAttackKnockbackDistance);

        ResetAttackTriggers();
        animator.SetTrigger("LightAttack");
    }

    private void StartHeavyAttack()
    {
        isAttacking = true;
        hasEnteredAttackState = false;

        SetCurrentAttack(heavyFirstAttackDamage, heavyFirstAttackImpact, heavyFirstAttackKnockbackDistance);

        ResetAttackTriggers();
        animator.SetTrigger("HeavyAttack");
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

        EndAttack();
    }

    private void EndAttack()
    {
        isAttacking = false;
        hasEnteredAttackState = false;

        SetCurrentAttack(0, HitImpact.None, 0f);
        ResetAttackTriggers();
    }

    private void ResetAttackTriggers()
    {
        animator.ResetTrigger("LightAttack");
        animator.ResetTrigger("HeavyAttack");
    }
}