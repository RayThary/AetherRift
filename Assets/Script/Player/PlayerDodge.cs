using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerDodge : MonoBehaviour
{
    [Header("Dodge")]
    [SerializeField] private float dodgeDistance = 3f;
    [SerializeField] private float dodgeMoveDuration = 0.3f;

    private PlayerCore playerCore;
    private Animator animator;

    private Vector3 dodgeDirection;
    private float remainingMoveTime;
    private float currentMoveDuration;

    private bool isDodging;
    private bool hasEnteredDodgeState;

    public void Initialize(PlayerCore core)
    {
        playerCore = core;
        animator = core.Animator;
    }

    private void Update()
    {
        if (playerCore == null)
            return;

        HandleDodgeInput();
        UpdateDodgeState();
        UpdateDodgeMovement();
    }

    private void HandleDodgeInput()
    {
        if (Keyboard.current == null || !Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        if (isDodging)
            return;

        StartDodge();
    }

    private void StartDodge()
    {
        if (!playerCore.TryEnterDodge())
            return;

        isDodging = true;
        hasEnteredDodgeState = false;

        dodgeDirection = -transform.forward;
        dodgeDirection.y = 0f;
        dodgeDirection.Normalize();

        currentMoveDuration = Mathf.Max(dodgeMoveDuration, 0.01f);
        remainingMoveTime = currentMoveDuration;

        animator.ResetTrigger("Dodge");
        animator.SetTrigger("Dodge");
    }

    private void UpdateDodgeState()
    {
        if (!isDodging)
            return;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (!hasEnteredDodgeState)
        {
            if (currentState.IsTag("Dodge"))
                hasEnteredDodgeState = true;

            return;
        }

        if (IsAnimatorDodging())
            return;

        EndDodge();
    }

    private void UpdateDodgeMovement()
    {
        if (!isDodging || !hasEnteredDodgeState || remainingMoveTime <= 0f)
            return;

        float moveTime = Mathf.Min(Time.deltaTime, remainingMoveTime);
        float dodgeSpeed = dodgeDistance / currentMoveDuration;

        transform.position += dodgeDirection * dodgeSpeed * moveTime;
        remainingMoveTime -= moveTime;
    }

    private bool IsAnimatorDodging()
    {
        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);

        if (currentState.IsTag("Dodge"))
            return true;

        if (!animator.IsInTransition(0))
            return false;

        AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(0);
        return nextState.IsTag("Dodge");
    }

    private void EndDodge()
    {
        isDodging = false;
        hasEnteredDodgeState = false;
        remainingMoveTime = 0f;

        animator.ResetTrigger("Dodge");
        playerCore.ExitDodge();
    }
}