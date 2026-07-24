using UnityEngine;

public enum PlayerState
{
    Locomotion,
    Attacking,
    Dodging,
    Hit,
    Dead
}

[RequireComponent(typeof(PlayerMovement), typeof(PlayerCombat), typeof(PlayerDodge))]
public class PlayerCore : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private PlayerDodge playerDodge;

    public Animator Animator => animator;
    public PlayerState CurrentState { get; private set; } = PlayerState.Locomotion;

    public bool CanMove => CurrentState == PlayerState.Locomotion;
    public bool CanAttack => CurrentState == PlayerState.Locomotion;
    public bool CanDodge => CurrentState == PlayerState.Locomotion || CurrentState == PlayerState.Attacking;
    public bool CanRotate => CurrentState != PlayerState.Dodging && CurrentState != PlayerState.Hit && CurrentState != PlayerState.Dead;

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
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

    public bool TryEnterDodge()
    {
        if (!CanDodge)
            return false;

        if (CurrentState == PlayerState.Attacking)
            playerCombat.CancelAttackForDodge();

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
}