using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    [SerializeField] private bool lockCursorOnStart = true;

    private void Start()
    {
        if (lockCursorOnStart)
            LockCursor();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (lockCursorOnStart)
            {
                UnlockCursor();
                lockCursorOnStart = false;
            }
            else
            {
                LockCursor();
                lockCursorOnStart = true;
            }

        }
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
