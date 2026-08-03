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

    private PlayerSkillController playerSkillController;

    private void Start()
    {
        if (!TryBindPlayerSkillController())
        {
            enabled = false;
            return;
        }

        if (skillKeyText != null)
            skillKeyText.text = skillKeyLabel;

        UpdateCooldownUI();
    }

    private void Update()
    {
        UpdateCooldownUI();
    }

    private bool TryBindPlayerSkillController()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[PlayerSkillSlotUI] GameManager를 찾을 수 없습니다.", this);
            return false;
        }

        GameObject currentPlayer = GameManager.Instance.CurrentPlayer;

        if (currentPlayer == null)
        {
            Debug.LogError("[PlayerSkillSlotUI] 현재 플레이어를 찾을 수 없습니다.", this);
            return false;
        }

        playerSkillController = currentPlayer.GetComponent<PlayerSkillController>();

        if (playerSkillController == null)
        {
            Debug.LogError("[PlayerSkillSlotUI] 플레이어에 PlayerSkillController가 없습니다.", currentPlayer);
            return false;
        }

        if (cooldownFill == null)
        {
            Debug.LogError("[PlayerSkillSlotUI] Cooldown Fill이 연결되지 않았습니다.", this);
            return false;
        }

        return true;
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
}
