using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text stateText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private TMP_Text purchaseButtonText;
    [SerializeField] private Sprite fallbackIcon;

    private int slotIndex = -1;
    private Action<int> purchaseCallback;

    private void Awake()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(HandlePurchaseButtonClicked);
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(HandlePurchaseButtonClicked);
    }

    public void Set(int index, RelicData relicData, RelicInstanceData relicInstance, bool canPurchase, string stateMessage, Action<int> onPurchase)
    {
        slotIndex = index;
        purchaseCallback = onPurchase;

        if (iconImage != null)
        {
            iconImage.sprite = relicData != null && relicData.Icon != null ? relicData.Icon : fallbackIcon;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (nameText != null)
            nameText.text = GetRelicName(relicData);

        if (descriptionText != null)
            descriptionText.text = relicData != null ? relicData.Description : string.Empty;

        if (priceText != null)
            priceText.text = relicInstance != null ? $"가격: {relicInstance.Price} 골드" : "가격: -";

        if (stateText != null)
            stateText.text = stateMessage;

        if (purchaseButtonText != null)
            purchaseButtonText.text = canPurchase ? "구매" : "구매 불가";

        if (purchaseButton != null)
            purchaseButton.interactable = canPurchase;

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        slotIndex = -1;
        purchaseCallback = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (nameText != null)
            nameText.text = string.Empty;

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (priceText != null)
            priceText.text = string.Empty;

        if (stateText != null)
            stateText.text = string.Empty;

        if (purchaseButton != null)
            purchaseButton.interactable = false;

        gameObject.SetActive(false);
    }

    private void HandlePurchaseButtonClicked()
    {
        if (slotIndex < 0)
            return;

        purchaseCallback?.Invoke(slotIndex);
    }

    private string GetRelicName(RelicData relicData)
    {
        if (relicData == null)
            return string.Empty;

        return string.IsNullOrWhiteSpace(relicData.DisplayName) ? relicData.RelicId : relicData.DisplayName;
    }
}
