using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Target Selection")]
    [SerializeField, Range(1f, 180f)] private float frontAngle = 130f;

    private readonly HashSet<IInteractable> nearbyInteractables = new HashSet<IInteractable>();
    private readonly List<IInteractable> invalidInteractables = new List<IInteractable>();

    private PlayerCore playerCore;
    private IInteractable selectedInteractable;

    public bool HasSelectedTarget => IsInteractableAvailable(selectedInteractable);

    private void Awake()
    {
        playerCore = GetComponent<PlayerCore>();
    }

    private void Update()
    {
        RefreshSelectedInteractable();

        if (playerCore != null && playerCore.IsPlayerPaused)
            return;

        if (!HasSelectedTarget || Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
            return;

        selectedInteractable.Interact();
    }

    public void RegisterInteractable(IInteractable interactable)
    {
        if (!IsInteractableValid(interactable))
            return;

        nearbyInteractables.Add(interactable);
    }

    public void UnregisterInteractable(IInteractable interactable)
    {
        if (interactable == null)
            return;

        nearbyInteractables.Remove(interactable);

        if (selectedInteractable == interactable)
            selectedInteractable = null;
    }

    private void RefreshSelectedInteractable()
    {
        RemoveInvalidInteractables();

        if (playerCore == null || playerCore.IsPlayerPaused || playerCore.CurrentState != PlayerState.Locomotion)
        {
            selectedInteractable = null;
            InteractionPromptUI.Instance?.HidePrompt();
            return;
        }

        IInteractable nextInteractable = null;
        float smallestAngle = float.MaxValue;
        float shortestDistance = float.MaxValue;

        foreach (IInteractable interactable in nearbyInteractables)
        {
            if (!TryGetTargetScore(interactable, out float angle, out float distance))
                continue;

            bool isMoreCentered = angle < smallestAngle - 0.01f;
            bool isSameAngleAndCloser = Mathf.Abs(angle - smallestAngle) <= 0.01f && distance < shortestDistance;

            if (!isMoreCentered && !isSameAngleAndCloser)
                continue;

            nextInteractable = interactable;
            smallestAngle = angle;
            shortestDistance = distance;
        }

        selectedInteractable = nextInteractable;

        if (selectedInteractable == null)
        {
            InteractionPromptUI.Instance?.HidePrompt();
            return;
        }

        InteractionPromptUI.Instance?.ShowPrompt(selectedInteractable.PromptMessage);
    }

    private bool TryGetTargetScore(IInteractable interactable, out float angle, out float distance)
    {
        angle = 180f;
        distance = float.MaxValue;

        if (!IsInteractableAvailable(interactable))
            return false;

        Vector3 playerPosition = transform.position;
        Vector3 targetPosition = interactable.InteractionTransform.position;

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

        Vector3 playerForward = transform.forward;
        playerForward.y = 0f;

        if (playerForward.sqrMagnitude <= 0.01f)
            return false;

        playerForward.Normalize();
        angle = Vector3.Angle(playerForward, directionToTarget);

        return angle <= frontAngle * 0.5f;
    }

    private void RemoveInvalidInteractables()
    {
        invalidInteractables.Clear();

        foreach (IInteractable interactable in nearbyInteractables)
        {
            if (!IsInteractableValid(interactable))
                invalidInteractables.Add(interactable);
        }

        foreach (IInteractable interactable in invalidInteractables)
            nearbyInteractables.Remove(interactable);
    }

    private static bool IsInteractableAvailable(IInteractable interactable)
    {
        return IsInteractableValid(interactable) && interactable.CanInteract;
    }

    private static bool IsInteractableValid(IInteractable interactable)
    {
        if (interactable == null)
            return false;

        if (interactable is Object unityObject && unityObject == null)
            return false;

        return interactable.InteractionTransform != null;
    }

    private void OnDisable()
    {
        selectedInteractable = null;
        nearbyInteractables.Clear();
        InteractionPromptUI.Instance?.HidePrompt();
    }

    private void OnValidate()
    {
        frontAngle = Mathf.Clamp(frontAngle, 1f, 180f);
    }
}
