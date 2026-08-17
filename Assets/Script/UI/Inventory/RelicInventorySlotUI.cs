using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RelicInventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private GameObject equippedMarkObject;

    private Image relicIconImage;
    private RelicInstanceData currentRelicInstance;
    private RelicData currentRelicData;
    private RelicTooltipUI tooltipUI;
    private string tooltipEffectText;
    private Action<RelicInstanceData> rightClickCallback;

    private void Awake()
    {
        relicIconImage = GetOrCreateIconImage();
    }

    public void Set(RelicInstanceData relicInstance, RelicData relicData, Sprite temporaryIcon, bool isEquipped, RelicTooltipUI tooltipUI, string tooltipEffectText, Action<RelicInstanceData> onRightClick)
    {
        relicIconImage ??= GetOrCreateIconImage();
        currentRelicInstance = relicInstance;
        currentRelicData = relicData;
        this.tooltipUI = tooltipUI;
        this.tooltipEffectText = tooltipEffectText;
        rightClickCallback = onRightClick;

        Sprite icon = relicData != null && relicData.Icon != null ? relicData.Icon : temporaryIcon;

        if (relicIconImage != null)
        {
            relicIconImage.sprite = icon;
            relicIconImage.enabled = icon != null;
        }

        SetEquipped(isEquipped);
    }

    public void Clear()
    {
        relicIconImage ??= GetOrCreateIconImage();
        currentRelicInstance = null;
        currentRelicData = null;
        tooltipUI = null;
        tooltipEffectText = string.Empty;
        rightClickCallback = null;

        if (relicIconImage != null)
        {
            relicIconImage.sprite = null;
            relicIconImage.enabled = false;
        }

        SetEquipped(false);
    }

    private Image GetOrCreateIconImage()
    {
        Transform iconTransform = transform.Find("IconImage");

        if (iconTransform != null && iconTransform.TryGetComponent(out Image existingIconImage))
            return existingIconImage;

        GameObject iconObject = new GameObject("IconImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(transform, false);

        RectTransform iconRectTransform = iconObject.GetComponent<RectTransform>();
        iconRectTransform.anchorMin = Vector2.zero;
        iconRectTransform.anchorMax = Vector2.one;
        iconRectTransform.offsetMin = Vector2.zero;
        iconRectTransform.offsetMax = Vector2.zero;

        Image createdIconImage = iconObject.GetComponent<Image>();
        createdIconImage.raycastTarget = false;
        createdIconImage.enabled = false;
        return createdIconImage;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentRelicInstance == null || tooltipUI == null)
            return;

        string displayName = currentRelicData != null ? currentRelicData.DisplayName : currentRelicInstance.RelicId;
        tooltipUI.Show(displayName, tooltipEffectText, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipUI != null)
            tooltipUI.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || currentRelicInstance == null)
            return;

        rightClickCallback?.Invoke(currentRelicInstance);
    }

    private void SetEquipped(bool isEquipped)
    {
        if (equippedMarkObject != null)
            equippedMarkObject.SetActive(isEquipped);
    }
}
