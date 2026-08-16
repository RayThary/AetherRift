using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RelicTooltipUI : MonoBehaviour
{
    [SerializeField] private RectTransform tooltipRoot;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Vector2 screenOffset = new Vector2(16f, -16f);

    private void Awake()
    {
        DisableTooltipRaycast();
        Hide();
    }

    public void Show(string relicName, string relicEffectText, Vector2 screenPosition)
    {
        if (tooltipRoot == null)
            return;

        DisableTooltipRaycast();

        if (nameText != null)
            nameText.text = relicName;

        if (effectText != null)
            effectText.text = relicEffectText;

        tooltipRoot.position = screenPosition + screenOffset;
        tooltipRoot.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (tooltipRoot != null)
            tooltipRoot.gameObject.SetActive(false);
    }

    private void DisableTooltipRaycast()
    {
        if (tooltipRoot == null)
            return;

        CanvasGroup canvasGroup = tooltipRoot.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = tooltipRoot.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        Graphic[] graphics = tooltipRoot.GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }
}