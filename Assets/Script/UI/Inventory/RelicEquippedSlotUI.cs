using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RelicEquippedSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image relicIconImage;
    private int slotIndex;
    private RelicInstanceData currentRelicInstance;
    private RelicData currentRelicData;
    private RelicTooltipUI tooltipUI;
    private string tooltipEffectText;
    private Action<int> rightClickCallback;

    private void Awake()
    {
        relicIconImage = GetOrCreateIconImage();
    }

    public void Set(int slotIndex, RelicInstanceData relicInstance, RelicData relicData, Sprite emptySlotIcon, RelicTooltipUI tooltipUI, string tooltipEffectText, Action<int> onRightClick)
    {
        relicIconImage ??= GetOrCreateIconImage();
        this.slotIndex = slotIndex;
        currentRelicInstance = relicInstance;
        currentRelicData = relicData;
        this.tooltipUI = tooltipUI;
        this.tooltipEffectText = tooltipEffectText;
        rightClickCallback = onRightClick;

        Sprite icon = relicData != null && relicData.Icon != null ? relicData.Icon : emptySlotIcon;

        if (relicIconImage != null)
        {
            relicIconImage.sprite = icon;
            relicIconImage.enabled = icon != null;
        }
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

        rightClickCallback?.Invoke(slotIndex);
    }
}
