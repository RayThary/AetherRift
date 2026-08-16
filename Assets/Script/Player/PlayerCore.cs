using UnityEngine;

public enum PlayerState
{
    Locomotion,
    Attacking,
    UsingSkill,
    Dodging,
    Hit,
    Dead
}

[RequireComponent(typeof(PlayerMovement), typeof(PlayerCombat), typeof(PlayerDodge))]
[RequireComponent(typeof(PlayerSkillController))]
public class PlayerCore : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private PlayerSkillController playerSkillController;
    private PlayerDodge playerDodge;

    public Animator Animator => animator;
    public PlayerState CurrentState { get; private set; } = PlayerState.Locomotion;
    public bool IsPlayerPaused { get; private set; }

    public bool CanMove => !IsPlayerPaused && CurrentState == PlayerState.Locomotion;
    public bool CanAttack => !IsPlayerPaused && CurrentState == PlayerState.Locomotion;
    public bool CanUseSkill => !IsPlayerPaused && CurrentState == PlayerState.Locomotion;
    public bool CanDodge => !IsPlayerPaused && (CurrentState == PlayerState.Locomotion || CurrentState == PlayerState.Attacking || CurrentState == PlayerState.UsingSkill);
    public bool CanRotate => !IsPlayerPaused && CurrentState != PlayerState.UsingSkill && CurrentState != PlayerState.Dodging && CurrentState != PlayerState.Hit && CurrentState != PlayerState.Dead;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
        playerSkillController = GetComponent<PlayerSkillController>();
        playerDodge = GetComponent<PlayerDodge>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError("[PlayerCore] Animator를 찾을 수 없습니다.");
            return;
        }

        playerMovement.Initialize(this);
        playerCombat.Initialize(this);
        playerSkillController.Initialize(this);
        playerDodge.Initialize(this);
    }

    public bool TryEnterAttack()
    {
        if (!CanAttack)
            return false;

        ChangeState(PlayerState.Attacking);
        return true;
    }

    public void ExitAttack()
    {
        if (CurrentState != PlayerState.Attacking)
            return;

        ChangeState(PlayerState.Locomotion);
    }

    public bool TryEnterSkill()
    {
        if (!CanUseSkill)
            return false;

        ChangeState(PlayerState.UsingSkill);
        return true;
    }

    public void ExitSkill()
    {
        if (CurrentState != PlayerState.UsingSkill)
            return;

        ChangeState(PlayerState.Locomotion);
    }

    public bool TryEnterDodge()
    {
        if (!CanDodge)
            return false;

        if (CurrentState == PlayerState.Attacking)
            playerCombat.CancelAttackForDodge();

        if (CurrentState == PlayerState.UsingSkill)
            playerSkillController.CancelSkillForDodge();

        ChangeState(PlayerState.Dodging);
        return true;
    }

    public void ExitDodge()
    {
        if (CurrentState != PlayerState.Dodging)
            return;

        ChangeState(PlayerState.Locomotion);
    }

    private void ChangeState(PlayerState newState)
    {
        CurrentState = newState;

        if (CurrentState != PlayerState.Locomotion)
            playerMovement.StopMovementAnimation();
    }

    public void EnterHit()
    {
        if (CurrentState == PlayerState.Dead)
            return;

        ChangeState(PlayerState.Hit);
    }

    public void ExitHit()
    {
        if (CurrentState != PlayerState.Hit)
            return;

        ChangeState(PlayerState.Locomotion);
    }

    public void SetPlayerPaused(bool isPaused)
    {
        if (IsPlayerPaused == isPaused)
            return;

        IsPlayerPaused = isPaused;

        if (IsPlayerPaused)
            playerMovement.StopMovementAnimation();
    }
}
