using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("Target Selection")]
    [SerializeField, Range(1f, 180f)] private float frontAngle = 130f;

    private readonly HashSet<IInteractionTarget> registeredTargets = new HashSet<IInteractionTarget>();
    private readonly List<IInteractionTarget> invalidTargets = new List<IInteractionTarget>();

    private IInteractionTarget selectedTarget;

    public bool HasSelectedTarget => IsTargetAvailable(selectedTarget);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        RefreshSelectedTarget();
        HandleInteractionInput();
    }

    public void RegisterTarget(IInteractionTarget target)
    {
        if (!IsTargetValid(target))
            return;

        registeredTargets.Add(target);
    }

    public void UnregisterTarget(IInteractionTarget target)
    {
        if (target == null)
            return;

        registeredTargets.Remove(target);

        if (selectedTarget != target)
            return;

        selectedTarget = null;
        InteractionPromptUI.Instance?.HidePrompt();
    }

    private void RefreshSelectedTarget()
    {
        RemoveInvalidTargets();

        IInteractionTarget nextTarget = null;
        float smallestAngle = float.MaxValue;
        float shortestDistance = float.MaxValue;

        foreach (IInteractionTarget target in registeredTargets)
        {
            if (!TryGetTargetScore(target, out float angle, out float distance))
                continue;

            bool isMoreCentered = angle < smallestAngle - 0.01f;
            bool isSameAngleAndCloser = Mathf.Abs(angle - smallestAngle) <= 0.01f && distance < shortestDistance;

            if (!isMoreCentered && !isSameAngleAndCloser)
                continue;

            nextTarget = target;
            smallestAngle = angle;
            shortestDistance = distance;
        }

        selectedTarget = nextTarget;

        if (selectedTarget == null)
        {
            InteractionPromptUI.Instance?.HidePrompt();
            return;
        }

        InteractionPromptUI.Instance?.ShowPrompt(selectedTarget.PromptMessage);
    }

    private void HandleInteractionInput()
    {
        if (!HasSelectedTarget || Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
            return;

        selectedTarget.Interact();
    }

    private bool TryGetTargetScore(IInteractionTarget target, out float angle, out float distance)
    {
        angle = 180f;
        distance = float.MaxValue;

        if (!IsTargetAvailable(target))
            return false;

        Vector3 playerPosition = target.PlayerInRange.transform.position;
        Vector3 targetPosition = target.TargetTransform.position;

        playerPosition.y = 0f;
        targetPosition.y = 0f;

        Vector3 directionToTarget = targetPosition - playerPosition;
        distance = directionToTarget.magnitude;

        if (distance <= 0.01f)
        {
            angle = 0f;
            return true;
        }

        directionToTarget /= distance;

        Vector3 playerForward = target.PlayerInRange.transform.forward;
        playerForward.y = 0f;

        if (playerForward.sqrMagnitude <= 0.01f)
            return false;

        playerForward.Normalize();
        angle = Vector3.Angle(playerForward, directionToTarget);

        return angle <= frontAngle * 0.5f;
    }

    private void RemoveInvalidTargets()
    {
        invalidTargets.Clear();

        foreach (IInteractionTarget target in registeredTargets)
        {
            if (!IsTargetValid(target))
                invalidTargets.Add(target);
        }

        foreach (IInteractionTarget target in invalidTargets)
            registeredTargets.Remove(target);
    }

    private static bool IsTargetAvailable(IInteractionTarget target)
    {
        return IsTargetValid(target) && !target.PlayerInRange.IsPlayerPaused && target.CanInteract;
    }

    private static bool IsTargetValid(IInteractionTarget target)
    {
        if (target == null)
            return false;

        if (target is Object unityObject && unityObject == null)
            return false;

        return target.TargetTransform != null && target.PlayerInRange != null;
    }

    private void OnDisable()
    {
        selectedTarget = null;
        InteractionPromptUI.Instance?.HidePrompt();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        frontAngle = Mathf.Clamp(frontAngle, 1f, 180f);
    }
}
