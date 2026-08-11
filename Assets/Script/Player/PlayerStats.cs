using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth), typeof(PlayerMovement), typeof(PlayerCombat))]
public class PlayerStats : MonoBehaviour
{
    private const int MaxDamageReductionPercent = 50;

    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private PlayerCombat playerCombat;
    private PlayerSkillController playerSkillController;

    private int appliedMaxHealthBonus;
    private int appliedAttackPowerBonus;
    private float appliedMoveSpeedBonus;
    private int appliedDamageReductionPercent;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
        playerCombat = GetComponent<PlayerCombat>();
        playerSkillController = GetComponent<PlayerSkillController>();
    }

    private void Start()
    {
        SubscribeProgressChanged();
        ApplyStats();
        RestoreCurrentHealth();
    }

    private void OnDestroy()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.ProgressChanged -= HandleProgressChanged;
    }

    private void SubscribeProgressChanged()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogError("[PlayerStats] GameProgressManager를 찾지 못했습니다.", this);
            return;
        }

        GameProgressManager.Instance.ProgressChanged -= HandleProgressChanged;
        GameProgressManager.Instance.ProgressChanged += HandleProgressChanged;
    }

    private void HandleProgressChanged()
    {
        ApplyStats();
    }

    public void ApplyStats()
    {
        GameProgressData progressData = GameProgressManager.Instance != null ? GameProgressManager.Instance.CurrentData : null;

        int maxHealthBonus = progressData != null ? progressData.maxHealthBonus : 0;
        int attackPowerBonus = progressData != null ? progressData.attackPowerBonus : 0;
        float moveSpeedBonus = progressData != null ? progressData.moveSpeedBonus : 0f;
        int damageReductionPercent = 0;

        ApplyEquippedRelicEffects(ref maxHealthBonus, ref attackPowerBonus, ref moveSpeedBonus, ref damageReductionPercent);

        maxHealthBonus = Mathf.Max(maxHealthBonus, 0);
        attackPowerBonus = Mathf.Max(attackPowerBonus, 0);
        moveSpeedBonus = Mathf.Max(moveSpeedBonus, 0f);
        damageReductionPercent = Mathf.Clamp(damageReductionPercent, 0, MaxDamageReductionPercent);

        bool maxHealthChanged = appliedMaxHealthBonus != maxHealthBonus;
        bool attackPowerChanged = appliedAttackPowerBonus != attackPowerBonus;
        bool moveSpeedChanged = !Mathf.Approximately(appliedMoveSpeedBonus, moveSpeedBonus);
        bool damageReductionChanged = appliedDamageReductionPercent != damageReductionPercent;

        if (maxHealthChanged)
            playerHealth.SetMaxHealth(playerHealth.MaxHealth - appliedMaxHealthBonus + maxHealthBonus, false);

        if (attackPowerChanged)
        {
            playerCombat.SetAttackPowerBonus(attackPowerBonus);
            playerSkillController?.SetAttackPowerBonus(attackPowerBonus);
        }

        if (moveSpeedChanged)
            playerMovement.SetMoveSpeedBonus(moveSpeedBonus);

        if (damageReductionChanged)
            playerHealth.SetDamageReductionPercent(damageReductionPercent);

        appliedMaxHealthBonus = maxHealthBonus;
        appliedAttackPowerBonus = attackPowerBonus;
        appliedMoveSpeedBonus = moveSpeedBonus;
        appliedDamageReductionPercent = damageReductionPercent;
    }

    private void RestoreCurrentHealth()
    {
        GameProgressData progressData = GameProgressManager.Instance != null ? GameProgressManager.Instance.CurrentData : null;

        if (progressData == null || progressData.currentHealth < 0)
        {
            playerHealth.SetCurrentHealth(playerHealth.MaxHealth);
            return;
        }

        playerHealth.SetCurrentHealth(progressData.currentHealth);
    }

    private void ApplyEquippedRelicEffects(ref int maxHealthBonus, ref int attackPowerBonus, ref float moveSpeedBonus, ref int damageReductionPercent)
    {
        GameProgressManager progressManager = GameProgressManager.Instance;

        if (progressManager == null)
            return;

        for (int i = 0; i < GameProgressData.RelicSlotCount; i++)
        {
            RelicInstanceData relicInstance = progressManager.GetEquippedRelic(i);

            if (relicInstance == null)
                continue;

            for (int effectIndex = 0; effectIndex < relicInstance.Effects.Count; effectIndex++)
            {
                RelicEffectValue effect = relicInstance.Effects[effectIndex];

                if (effect == null)
                    continue;

                switch (effect.EffectType)
                {
                    case RelicEffectType.MaxHealth:
                        maxHealthBonus += Mathf.RoundToInt(effect.Value);
                        break;

                    case RelicEffectType.AttackPower:
                        attackPowerBonus += Mathf.RoundToInt(effect.Value);
                        break;

                    case RelicEffectType.MoveSpeed:
                        moveSpeedBonus += effect.Value;
                        break;

                    case RelicEffectType.DamageReduction:
                        damageReductionPercent += Mathf.RoundToInt(effect.Value);
                        break;
                }
            }
        }
    }
}
