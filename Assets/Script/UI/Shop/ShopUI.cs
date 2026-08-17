using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShopUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool closeOnStart = true;
    [SerializeField] private Key closeKey = Key.Escape;

    [Header("Currency")]
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private string currencyFormat = "보유 골드: {0}";

    [Header("Shop Items")]
    [SerializeField] private RelicData[] shopRelics;
    [SerializeField] private ShopItemSlotUI[] itemSlots;
    [SerializeField] private bool regenerateStockOnOpen = true;
    [SerializeField] private bool restockPurchasedItem;

    private RelicInstanceData[] currentStock;
    private bool isOpen;

    private void Awake()
    {
        currentStock = new RelicInstanceData[GetShopRelicCount()];

        if (closeOnStart)
            SetOpen(false);
        else
        {
            isOpen = panelRoot != null && panelRoot.activeSelf;
            BuildStock();
            Refresh();
        }
    }

    private void OnEnable()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.ProgressChanged += Refresh;
    }

    private void OnDisable()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.ProgressChanged -= Refresh;

        if (GameManager.Instance != null)
            GameManager.Instance.SetGamePaused(GamePauseReason.Dialogue, false);
    }

    private void Update()
    {
        if (!isOpen || Keyboard.current == null || closeKey == Key.None)
            return;

        if (Keyboard.current[closeKey].wasPressedThisFrame)
            Close();
    }

    public void Open()
    {
        if (regenerateStockOnOpen || !HasValidStock())
            BuildStock();

        SetOpen(true);
        Refresh();
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void Refresh()
    {
        if (!isOpen)
            return;

        RefreshCurrency();
        RefreshItemSlots();
    }

    public void Purchase(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex))
            return;

        RelicInstanceData relicInstance = currentStock[slotIndex];

        if (relicInstance == null)
        {
            Refresh();
            return;
        }

        if (GameProgressManager.Instance == null)
        {
            Refresh();
            return;
        }

        if (GameProgressManager.Instance.IsRelicInventoryFull())
        {
            Refresh();
            return;
        }

        if (GameProgressManager.Instance.Currency < relicInstance.Price)
        {
            Refresh();
            return;
        }

        if (!GameProgressManager.Instance.TryPurchaseRelic(relicInstance))
        {
            Refresh();
            return;
        }

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        if (restockPurchasedItem)
            currentStock[slotIndex] = CreateStockInstance(slotIndex);
        else
            currentStock[slotIndex] = null;

        Refresh();
    }

    private void SetOpen(bool open)
    {
        isOpen = open;

        if (panelRoot != null)
        {
            if (panelRoot.activeSelf != isOpen)
                panelRoot.SetActive(isOpen);
        }
        else
        {
            Debug.LogError("[ShopUI] Panel Root가 연결되지 않았습니다.", this);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.SetGamePaused(GamePauseReason.Dialogue, isOpen);
    }

    private void BuildStock()
    {
        int relicCount = GetShopRelicCount();

        if (currentStock == null || currentStock.Length != relicCount)
            currentStock = new RelicInstanceData[relicCount];

        for (int i = 0; i < relicCount; i++)
            currentStock[i] = CreateStockInstance(i);
    }

    private RelicInstanceData CreateStockInstance(int slotIndex)
    {
        if (shopRelics == null || slotIndex < 0 || slotIndex >= shopRelics.Length || RelicManager.Instance == null)
            return null;

        return RelicManager.Instance.CreateRelicInstance(shopRelics[slotIndex]);
    }

    private bool HasValidStock()
    {
        if (currentStock == null || currentStock.Length == 0)
            return false;

        for (int i = 0; i < currentStock.Length; i++)
        {
            if (currentStock[i] != null)
                return true;
        }

        return false;
    }

    private void RefreshCurrency()
    {
        if (currencyText == null)
            return;

        int currency = GameProgressManager.Instance != null ? GameProgressManager.Instance.Currency : 0;
        currencyText.text = string.Format(currencyFormat, currency);
    }

    private void RefreshItemSlots()
    {
        if (itemSlots == null)
            return;

        for (int i = 0; i < itemSlots.Length; i++)
        {
            ShopItemSlotUI itemSlot = itemSlots[i];

            if (itemSlot == null)
                continue;

            if (shopRelics == null || i >= shopRelics.Length || shopRelics[i] == null)
            {
                itemSlot.Clear();
                continue;
            }

            RelicInstanceData relicInstance = currentStock != null && i < currentStock.Length ? currentStock[i] : null;

            if (relicInstance == null)
            {
                itemSlot.Clear();
                continue;
            }

            bool canPurchase = CanPurchase(relicInstance);
            itemSlot.Set(i, shopRelics[i], relicInstance, canPurchase, Purchase);
        }
    }

    private bool CanPurchase(RelicInstanceData relicInstance)
    {
        if (relicInstance == null)
            return false;

        if (GameProgressManager.Instance == null)
            return false;

        if (GameProgressManager.Instance.IsRelicInventoryFull())
            return false;

        if (GameProgressManager.Instance.Currency < relicInstance.Price)
            return false;

        return true;
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return currentStock != null && slotIndex >= 0 && slotIndex < currentStock.Length;
    }

    private int GetShopRelicCount()
    {
        return shopRelics != null ? shopRelics.Length : 0;
    }
}
