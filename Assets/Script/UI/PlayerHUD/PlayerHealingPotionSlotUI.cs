using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealingPotionSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private string keyLabel = "1";
    [SerializeField] private string countFormat = "x{0}";

    private GameObject currentPlayer;
    private PlayerHealingPotionController potionController;

    private void Awake()
    {
        if (keyText != null)
            keyText.text = keyLabel;

        ClearUI();
    }

    private void Update()
    {
        GameObject newPlayer = GameManager.Instance != null ? GameManager.Instance.CurrentPlayer : null;

        if (!object.ReferenceEquals(currentPlayer, newPlayer))
            BindCurrentPlayer(newPlayer);

        Refresh();
    }

    private void BindCurrentPlayer(GameObject newPlayer)
    {
        if (potionController != null)
            potionController.PotionChanged -= Refresh;

        currentPlayer = newPlayer;
        potionController = currentPlayer != null ? currentPlayer.GetComponent<PlayerHealingPotionController>() : null;

        if (potionController != null)
            potionController.PotionChanged += Refresh;

        Refresh();
    }

    private void Refresh()
    {
        if (potionController == null)
        {
            ClearUI();
            return;
        }

        if (countText != null)
            countText.text = string.Format(countFormat, potionController.CurrentPotionCount);

        if (cooldownFill == null)
            return;

        float cooldownNormalized = potionController.CooldownNormalized;
        cooldownFill.fillAmount = cooldownNormalized;
        cooldownFill.enabled = cooldownNormalized > 0f;
    }

    private void ClearUI()
    {
        if (countText != null)
            countText.text = string.Format(countFormat, 0);

        if (cooldownFill != null)
        {
            cooldownFill.fillAmount = 0f;
            cooldownFill.enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (potionController != null)
            potionController.PotionChanged -= Refresh;
    }
}
