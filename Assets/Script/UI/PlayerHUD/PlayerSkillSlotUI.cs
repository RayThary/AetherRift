using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSkillSlotUI : MonoBehaviour
{
    [Header("Skill")]
    [SerializeField] private PlayerSkillSlot skillSlot = PlayerSkillSlot.Skill1;

    [Header("UI")]
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TMP_Text cooldownText;
    [SerializeField] private TMP_Text skillKeyText;
    [SerializeField] private string skillKeyLabel = "Q";

    private GameObject currentPlayer;
    private PlayerSkillController playerSkillController;

    private void Awake()
    {
        if (cooldownFill == null)
        {
            Debug.LogError("[PlayerSkillSlotUI] Cooldown Fill이 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        if (skillKeyText != null)
            skillKeyText.text = skillKeyLabel;

        ClearCooldownUI();
    }

    private void Update()
    {
        GameObject newPlayer = GameManager.Instance != null ? GameManager.Instance.CurrentPlayer : null;

        if (!object.ReferenceEquals(currentPlayer, newPlayer))
            BindCurrentPlayer(newPlayer);

        if (playerSkillController == null)
            return;

        UpdateCooldownUI();
    }

    private void BindCurrentPlayer(GameObject newPlayer)
    {
        currentPlayer = newPlayer;
        playerSkillController = currentPlayer != null ? currentPlayer.GetComponent<PlayerSkillController>() : null;

        if (currentPlayer != null && playerSkillController == null)
            Debug.LogError("[PlayerSkillSlotUI] 현재 플레이어에 PlayerSkillController가 없습니다.", currentPlayer);

        if (playerSkillController == null)
        {
            ClearCooldownUI();
            return;
        }

        UpdateCooldownUI();
    }

    private void UpdateCooldownUI()
    {
        float cooldownRemaining = playerSkillController.GetSkillCooldownRemaining(skillSlot);
        bool isOnCooldown = cooldownRemaining > 0f;

        cooldownFill.fillAmount = playerSkillController.GetSkillCooldownNormalized(skillSlot);
        cooldownFill.enabled = isOnCooldown;

        if (cooldownText == null)
            return;

        cooldownText.text = isOnCooldown ? Mathf.CeilToInt(cooldownRemaining).ToString() : string.Empty;
    }

    private void ClearCooldownUI()
    {
        cooldownFill.fillAmount = 0f;
        cooldownFill.enabled = false;

        if (cooldownText != null)
            cooldownText.text = string.Empty;
    }
}
