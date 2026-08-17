using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Sprite fallbackIcon;
    [SerializeField] private string priceFormat = "{0}";
    [SerializeField, Range(10, 100)] private int detailSizePercent = 60;

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

    public void Set(int index, RelicData relicData, RelicInstanceData relicInstance, bool canPurchase, Action<int> onPurchase)
    {
        slotIndex = index;
        purchaseCallback = onPurchase;

        if (iconImage != null)
        {
            iconImage.sprite = relicData != null && relicData.Icon != null ? relicData.Icon : fallbackIcon;
            iconImage.enabled = iconImage.sprite != null;
        }

        if (descriptionText != null)
            descriptionText.text = BuildDescriptionText(relicData, relicInstance);

        if (priceText != null)
            priceText.text = relicInstance != null ? string.Format(priceFormat, relicInstance.Price) : string.Empty;

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

        if (descriptionText != null)
            descriptionText.text = string.Empty;

        if (priceText != null)
            priceText.text = string.Empty;

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

    private string BuildDescriptionText(RelicData relicData, RelicInstanceData relicInstance)
    {
        if (relicData == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder();
        builder.Append(GetRelicName(relicData));

        string effectText = BuildEffectText(relicInstance);
        string detailText = !string.IsNullOrWhiteSpace(effectText) ? effectText : relicData.Description;

        if (!string.IsNullOrWhiteSpace(detailText))
        {
            builder.AppendLine();
            builder.Append("<size=40%>");
            builder.AppendLine();
            builder.Append("</size>");
            builder.Append("<size=");
            builder.Append(detailSizePercent);
            builder.Append("%>");
            builder.Append(detailText);
            builder.Append("</size>");
        }

        return builder.ToString();
    }

    private string BuildEffectText(RelicInstanceData relicInstance)
    {
        if (relicInstance == null || relicInstance.Effects == null || relicInstance.Effects.Count == 0)
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < relicInstance.Effects.Count; i++)
        {
            RelicEffectValue effect = relicInstance.Effects[i];

            if (effect == null)
                continue;

            if (builder.Length > 0)
                builder.AppendLine();

            builder.Append(GetEffectDisplayName(effect.EffectType));
            builder.Append(" +");
            builder.Append(FormatEffectValue(effect.Value));
        }

        return builder.ToString();
    }

    private string FormatEffectValue(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.##");
    }

    private string GetEffectDisplayName(RelicEffectType effectType)
    {
        switch (effectType)
        {
            case RelicEffectType.AttackPower:
                return "공격력";
            case RelicEffectType.MaxHealth:
                return "최대 체력";
            case RelicEffectType.MoveSpeed:
                return "이동 속도";
            case RelicEffectType.DamageReduction:
                return "피해 감소";
            default:
                return effectType.ToString();
        }
    }

    private string GetRelicName(RelicData relicData)
    {
        if (relicData == null)
            return string.Empty;

        return string.IsNullOrWhiteSpace(relicData.DisplayName) ? relicData.RelicId : relicData.DisplayName;
    }
}
