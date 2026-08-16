using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerSkillSlot
{
    Skill1,
    Skill2,
    Skill3,
    Skill4
}

[System.Serializable]
public class PlayerSkillData
{
    [Header("Info")]
    [SerializeField] private string skillName = "Forward Slam";
    [SerializeField] private PlayerSkillSlot slot = PlayerSkillSlot.Skill1;

    [Header("Animation")]
    [SerializeField] private string animatorTrigger = "SkillAttack";

    [Header("Attack")]
    [SerializeField] private int damage = 40;
    [SerializeField] private HitImpact impact = HitImpact.Heavy;
    [SerializeField] private float knockbackDistance = 1.2f;

    [Header("Movement")]
    [SerializeField] private float moveDistance = 2f;
    [SerializeField] private float moveDuration = 0.35f;

    [Header("Defense")]
    [SerializeField, Range(0f, 1f)] private float damageReductionRate = 0.5f;

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 5f;

    public string SkillName => skillName;
    public PlayerSkillSlot Slot => slot;
    public string AnimatorTrigger => animatorTrigger;
    public int Damage => damage;
    public HitImpact Impact => impact;
    public float KnockbackDistance => knockbackDistance;
    public float MoveDistance => moveDistance;
    public float MoveDuration => moveDuration;
    public float DamageReductionRate => damageReductionRate;
    public float Cooldown => cooldown;
}

public class PlayerSkillController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Key skill1Key = Key.Q;
    [SerializeField] private Key skill2Key = Key.E;
    [SerializeField] private Key skill3Key = Key.R;
    [SerializeField] private Key skill4Key = Key.F;

    [Header("Skills")]
    [SerializeField] private List<PlayerSkillData> skills = new List<PlayerSkillData> { new PlayerSkillData() };

    [Header("State")]
    [SerializeField] private float skillStateCheckDelay = 0.1f;

    private PlayerCore playerCore;
    private PlayerMovement playerMovement;
    private PlayerAttackHitbox playerAttackHitbox;
    private Animator animator;

    private float[] cooldownRemainingBySkill;
    private int currentSkillIndex = -1;
    private int attackPowerBonus;

    private Vector3 skillMoveDirection;
    private float remainingSkillMoveTime;
    private float currentSkillMoveDuration;
    private float currentSkillMoveDistance;
    private float currentSkillStateCheckDelay;

    private bool isUsingSkill;
    private bool hasEnteredSkillState;
    private bool isSkillMoving;

    private PlayerSkillData CurrentSkill => IsValidSkillIndex(currentSkillIndex) ? skills[currentSkillIndex] : null;

    public bool IsUsingSkill => isUsingSkill;
    public bool IsReady => IsSkillReady(PlayerSkillSlot.Skill1);
    public float Cooldown => GetSkillCooldown(PlayerSkillSlot.Skill1);
    public float CooldownRemaining => GetSkillCooldownRemaining(PlayerSkillSlot.Skill1);
    public float CooldownNormalized => GetSkillCooldownNormalized(PlayerSkillSlot.Skill1);
    public int SkillCount => skills != null ? skills.Count : 0;

    public int CurrentAttackDamage { get; private set; }
    public HitImpact CurrentAttackImpact { get; private set; } = HitImpact.None;
    public float CurrentAttackKnockbackDistance { get; private set; }

    public void Initialize(PlayerCore core)
    {
        playerCore = core;
        playerMovement = GetComponent<PlayerMovement>();
        playerAttackHitbox = GetComponent<PlayerAttackHitbox>();
        animator = core.Animator;

        InitializeCooldowns();
    }

    public void SetAttackPowerBonus(int value)
    {
        attackPowerBonus = Mathf.Max(value, 0);
    }

    private void Update()
    {
        if (playerCore == null)
            return;

        if (playerCore.IsPlayerPaused)
            return;

        UpdateCooldowns();
        HandleSkillInput();
        UpdateSkillState();
        UpdateSkillMovement();
    }

    private void HandleSkillInput()
    {
        if (Keyboard.current == null || isUsingSkill || skills == null)
            return;

        if (Keyboard.current.fKey.wasPressedThisFrame && InteractionPromptUI.Instance != null)
            return;

        for (int i = 0; i < skills.Count; i++)
        {
            PlayerSkillData skill = skills[i];

            if (skill == null || !IsSkillSlotPressed(skill.Slot))
                continue;

            TryStartSkill(i);
            return;
        }
    }

    private bool IsSkillSlotPressed(PlayerSkillSlot slot)
    {
        Key key = slot switch
        {
            PlayerSkillSlot.Skill1 => skill1Key,
            PlayerSkillSlot.Skill2 => skill2Key,
            PlayerSkillSlot.Skill3 => skill3Key,
            PlayerSkillSlot.Skill4 => skill4Key,
            _ => Key.None
        };

        return key != Key.None && Keyboard.current[key].wasPressedThisFrame;
    }

    private void TryStartSkill(int skillIndex)
    {
        if (!IsValidSkillIndex(skillIndex) || !IsSkillReady(skillIndex))
            return;

        PlayerSkillData skill = skills[skillIndex];

        if (string.IsNullOrWhiteSpace(skill.AnimatorTrigger) || !playerCore.TryEnterSkill())
            return;

        playerMovement.RotateToCameraForward();

        currentSkillIndex = skillIndex;
        isUsingSkill = true;
        hasEnteredSkillState = false;
        currentSkillStateCheckDelay = Mathf.Max(skillStateCheckDelay, 0f);
        cooldownRemainingBySkill[skillIndex] = Mathf.Max(skill.Cooldown, 0f);

        SetCurrentAttack(skill.Damage, skill.Impact, skill.KnockbackDistance);
        StopSkillMovement();
        ResetCurrentSkillTrigger();

        animator.ResetTrigger("AttackCancel");
        animator.SetTrigger(skill.AnimatorTrigger);
    }

    private void InitializeCooldowns()
    {
        cooldownRemainingBySkill = new float[SkillCount];
    }

    private void UpdateCooldowns()
    {
        if (cooldownRemainingBySkill == null || cooldownRemainingBySkill.Length != SkillCount)
            InitializeCooldowns();

        for (int i = 0; i < cooldownRemainingBySkill.Length; i++)
        {
            if (cooldownRemainingBySkill[i] <= 0f)
                continue;

            cooldownRemainingBySkill[i] = Mathf.Max(cooldownRemainingBySkill[i] - Time.deltaTime, 0f);
        }
    }

    private void UpdateSkillState()
    {
        if (!isUsingSkill)
            return;

        if (currentSkillStateCheckDelay > 0f)
        {
            currentSkillStateCheckDelay -= Time.deltaTime;
            return;
        }

        bool animatorIsUsingSkill = IsAnimatorUsingSkill();

        if (!hasEnteredSkillState)
        {
            if (animatorIsUsingSkill)
            {
                hasEnteredSkillState = true;
                return;
            }

            EndSkill();
            return;
        }

        if (!animatorIsUsingSkill)
            EndSkill();
    }

    private bool IsAnimatorUsingSkill()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsTag("Attack"))
            return true;

        if (!animator.IsInTransition(0))
            return false;

        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        return nextState.IsTag("Attack");
    }

    public void BeginSkillMovement()
    {
        PlayerSkillData skill = CurrentSkill;

        if (!isUsingSkill || skill == null)
            return;

        skillMoveDirection = transform.forward;
        skillMoveDirection.y = 0f;

        if (skillMoveDirection.sqrMagnitude <= 0.01f)
            return;

        skillMoveDirection.Normalize();

        currentSkillMoveDistance = Mathf.Max(skill.MoveDistance, 0f);
        currentSkillMoveDuration = Mathf.Max(skill.MoveDuration, 0.01f);
        remainingSkillMoveTime = currentSkillMoveDuration;
        isSkillMoving = currentSkillMoveDistance > 0f;
    }

    public void EndSkillMovement()
    {
        StopSkillMovement();
    }

    private void UpdateSkillMovement()
    {
        if (!isUsingSkill || !isSkillMoving || remainingSkillMoveTime <= 0f)
            return;

        float moveTime = Mathf.Min(Time.deltaTime, remainingSkillMoveTime);
        float moveSpeed = currentSkillMoveDistance / currentSkillMoveDuration;

        transform.position += skillMoveDirection * moveSpeed * moveTime;
        remainingSkillMoveTime -= moveTime;

        if (remainingSkillMoveTime <= 0f)
            StopSkillMovement();
    }

    private void StopSkillMovement()
    {
        isSkillMoving = false;
        remainingSkillMoveTime = 0f;
        currentSkillMoveDuration = 0f;
        currentSkillMoveDistance = 0f;
    }

    public int CalculateReducedDamage(int damage)
    {
        int safeDamage = Mathf.Max(damage, 0);
        float damageReductionRate = CurrentSkill != null ? CurrentSkill.DamageReductionRate : 0f;
        float damageMultiplier = 1f - Mathf.Clamp01(damageReductionRate);
        return Mathf.CeilToInt(safeDamage * damageMultiplier);
    }

    public bool IsSkillReady(PlayerSkillSlot slot)
    {
        int skillIndex = FindSkillIndex(slot);
        return IsSkillReady(skillIndex);
    }

    public float GetSkillCooldown(PlayerSkillSlot slot)
    {
        int skillIndex = FindSkillIndex(slot);
        return IsValidSkillIndex(skillIndex) ? Mathf.Max(skills[skillIndex].Cooldown, 0f) : 0f;
    }

    public float GetSkillCooldownRemaining(PlayerSkillSlot slot)
    {
        int skillIndex = FindSkillIndex(slot);

        if (!IsValidSkillIndex(skillIndex) || cooldownRemainingBySkill == null || skillIndex >= cooldownRemainingBySkill.Length)
            return 0f;

        return cooldownRemainingBySkill[skillIndex];
    }

    public float GetSkillCooldownNormalized(PlayerSkillSlot slot)
    {
        float cooldown = GetSkillCooldown(slot);
        return cooldown <= 0f ? 0f : GetSkillCooldownRemaining(slot) / cooldown;
    }

    public void CancelSkillForDodge()
    {
        if (!isUsingSkill)
            return;

        ResetCurrentSkillTrigger();
        ClearSkillState();

        animator.ResetTrigger("AttackCancel");
        animator.SetTrigger("AttackCancel");
    }

    private void EndSkill()
    {
        ResetCurrentSkillTrigger();
        ClearSkillState();
        playerCore.ExitSkill();
    }

    private void ClearSkillState()
    {
        isUsingSkill = false;
        hasEnteredSkillState = false;
        currentSkillStateCheckDelay = 0f;

        SetCurrentAttack(0, HitImpact.None, 0f);
        StopSkillMovement();

        currentSkillIndex = -1;
    }

    private void SetCurrentAttack(int damage, HitImpact impact, float knockbackDistance)
    {
        CurrentAttackDamage = Mathf.Max(damage + attackPowerBonus, 0);
        CurrentAttackImpact = impact;
        CurrentAttackKnockbackDistance = Mathf.Max(knockbackDistance, 0f);

        if (playerAttackHitbox != null)
            playerAttackHitbox.SetAttackData(CurrentAttackDamage, CurrentAttackImpact, CurrentAttackKnockbackDistance);
    }

    private bool IsSkillReady(int skillIndex)
    {
        if (!IsValidSkillIndex(skillIndex))
            return false;

        if (cooldownRemainingBySkill == null || skillIndex >= cooldownRemainingBySkill.Length)
            return true;

        return cooldownRemainingBySkill[skillIndex] <= 0f;
    }

    private int FindSkillIndex(PlayerSkillSlot slot)
    {
        if (skills == null)
            return -1;

        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null && skills[i].Slot == slot)
                return i;
        }

        return -1;
    }

    private bool IsValidSkillIndex(int skillIndex)
    {
        return skills != null && skillIndex >= 0 && skillIndex < skills.Count && skills[skillIndex] != null;
    }

    private void ResetCurrentSkillTrigger()
    {
        if (animator == null || CurrentSkill == null || string.IsNullOrWhiteSpace(CurrentSkill.AnimatorTrigger))
            return;

        animator.ResetTrigger(CurrentSkill.AnimatorTrigger);
    }

    private void OnDisable()
    {
        if (!isUsingSkill)
            return;

        ResetCurrentSkillTrigger();
        ClearSkillState();

        if (animator != null)
            animator.ResetTrigger("AttackCancel");

        if (playerCore != null)
            playerCore.ExitSkill();
    }
}
