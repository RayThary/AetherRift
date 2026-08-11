using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3f;
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Animation")]
    [SerializeField] private float idleDelay = 0.1f;
    [SerializeField] private float animationDampTime = 0.1f;

    private PlayerCore playerCore;
    private Animator animator;
    private Camera mainCamera;
    private float idleTimer;
    private float moveSpeedBonus;

    public float WalkSpeed => Mathf.Max(walkSpeed + moveSpeedBonus, 0f);
    public float RunSpeed => Mathf.Max(runSpeed + moveSpeedBonus, 0f);
    
    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Initialize(PlayerCore core)
    {
        playerCore = core;
        animator = core.Animator;
    }

    private void Update()
    {
        if (playerCore == null)
            return;

        if (!playerCore.CanMove)
            return;

        Move();     
    }

    private void Move()
    {
        if (Keyboard.current == null || mainCamera == null)
            return;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        bool isMoving = input.sqrMagnitude > 0.01f;

        if (isMoving)
            input.Normalize();

        bool isRunning = isMoving && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * input.y + cameraRight * input.x;

        if (isMoving && playerCore.CanRotate)
            RotateToDirection(moveDirection);

        float currentSpeed = isRunning ? RunSpeed : WalkSpeed;

        transform.position += moveDirection * currentSpeed * Time.deltaTime;
        UpdateMovementAnimation(moveDirection, isMoving, isRunning);
    }

    private void UpdateMovementAnimation(Vector3 moveDirection, bool isMoving, bool isRunning)
    {
        if (isMoving)
        {
            idleTimer = idleDelay;

            Vector3 localDirection = transform.InverseTransformDirection(moveDirection);

            animator.SetFloat("MoveX", localDirection.x, animationDampTime, Time.deltaTime);
            animator.SetFloat("MoveY", localDirection.z, animationDampTime, Time.deltaTime);

            float locomotionBlend = isRunning ? 1f : 0.5f;
            animator.SetFloat("LocomotionBlend", locomotionBlend, animationDampTime, Time.deltaTime);
        }
        else
        {
            idleTimer -= Time.deltaTime;

            if (idleTimer <= 0f)
                animator.SetFloat("LocomotionBlend", 0f, animationDampTime, Time.deltaTime);
        }
    }

    public void StopMovementAnimation()
    {
        idleTimer = 0f;

        animator.SetFloat("MoveX", 0f);
        animator.SetFloat("MoveY", 0f);
        animator.SetFloat("LocomotionBlend", 0f);
    }

    public void SetMoveSpeedBonus(float value)
    {
        moveSpeedBonus = value;
    }

    public Vector3 GetCameraRelativeInputDirection()
    {
        if (Keyboard.current == null || mainCamera == null)
            return Vector3.zero;

        Vector2 input = Vector2.zero;

        if (Keyboard.current.wKey.isPressed)
            input.y += 1f;

        if (Keyboard.current.sKey.isPressed)
            input.y -= 1f;

        if (Keyboard.current.aKey.isPressed)
            input.x -= 1f;

        if (Keyboard.current.dKey.isPressed)
            input.x += 1f;

        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;

        cameraForward.y = 0f;
        cameraRight.y = 0f;

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 inputDirection = cameraForward * input.y + cameraRight * input.x;
        return inputDirection.sqrMagnitude > 0.01f ? inputDirection.normalized : Vector3.zero;
    }

    public void RotateToCameraForward()
    {
        if (mainCamera == null)
            return;

        Vector3 cameraForward = mainCamera.transform.forward;
        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.LookRotation(cameraForward);
    }

    private void RotateToDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
