using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryToggleController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Key toggleKey = Key.I;
    [SerializeField] private bool closeOnStart = true;

    private RelicInventoryUI relicInventoryUI;
    private bool isOpen;

    private void Awake()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[InventoryToggleController] Inventory Panel이 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        relicInventoryUI = inventoryPanel.GetComponentInChildren<RelicInventoryUI>(true);

        if (closeOnStart)
            SetInventoryOpen(false);
        else
            isOpen = inventoryPanel.activeSelf;
    }

    private void Update()
    {
        if (Keyboard.current == null || toggleKey == Key.None)
            return;

        if (Keyboard.current[toggleKey].wasPressedThisFrame)
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        SetInventoryOpen(!isOpen);
    }

    public void OpenInventory()
    {
        SetInventoryOpen(true);
    }

    public void CloseInventory()
    {
        SetInventoryOpen(false);
    }

    private void SetInventoryOpen(bool open)
    {
        isOpen = open;

        if (inventoryPanel != null && inventoryPanel.activeSelf != isOpen)
            inventoryPanel.SetActive(isOpen);

        if (isOpen && relicInventoryUI != null)
            relicInventoryUI.Refresh();

        if (GameManager.Instance != null)
            GameManager.Instance.SetGamePaused(GamePauseReason.Inventory, isOpen);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetGamePaused(GamePauseReason.Inventory, false);
    }
}
