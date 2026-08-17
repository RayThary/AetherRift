using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerHealingPotionController : MonoBehaviour
{
    [Header("Potion")]
    [SerializeField] private int maxPotionCount = 3;
    [SerializeField, Range(0f, 1f)] private float healRatio = 0.3f;
    [SerializeField] private int minimumHealAmount = 1;
    [SerializeField] private float useCooldown = 1f;

    [Header("Input")]
    [SerializeField] private Key useKey = Key.Digit1;

    [Header("Battle Only")]
    [SerializeField] private bool battleOnly = true;
    [SerializeField] private int battleSceneIndex = 3;

    private PlayerCore playerCore;
    private PlayerHealth playerHealth;
    private int currentPotionCount;
    private float nextUsableTime;

    public int CurrentPotionCount => currentPotionCount;
    public int MaxPotionCount => maxPotionCount;
    public float CooldownRemaining => Mathf.Max(nextUsableTime - Time.time, 0f);
    public float CooldownNormalized => useCooldown > 0f ? Mathf.Clamp01(CooldownRemaining / useCooldown) : 0f;

    public event Action PotionChanged;

    private void Awake()
    {
        playerCore = GetComponent<PlayerCore>();
        playerHealth = GetComponent<PlayerHealth>();
        maxPotionCount = Mathf.Max(maxPotionCount, 0);
        minimumHealAmount = Mathf.Max(minimumHealAmount, 1);
    }

    private void Update()
    {
        if (Keyboard.current == null || useKey == Key.None)
            return;

        if (Keyboard.current[useKey].wasPressedThisFrame)
            TryUsePotion();
    }

    public void RefillPotions()
    {
        maxPotionCount = Mathf.Max(maxPotionCount, 0);
        currentPotionCount = maxPotionCount;
        nextUsableTime = 0f;
        NotifyPotionChanged();
        Debug.Log($"[Potion] Refilled: {currentPotionCount}", this);
    }

    public bool TryUsePotion()
    {
        if (!CanUsePotion())
            return false;

        int healAmount = Mathf.Max(Mathf.CeilToInt(playerHealth.MaxHealth * healRatio), minimumHealAmount);

        if (!playerHealth.Heal(healAmount))
            return false;

        currentPotionCount--;
        nextUsableTime = Time.time + Mathf.Max(useCooldown, 0f);
        NotifyPotionChanged();
        return true;
    }

    private bool CanUsePotion()
    {
        if (playerHealth == null || playerHealth.IsDead || currentPotionCount <= 0)
            return false;

        if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
            return false;

        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
            return false;

        if (playerCore != null && playerCore.CurrentState != PlayerState.Locomotion)
            return false;

        if (battleOnly && SceneManager.GetActiveScene().buildIndex != battleSceneIndex)
            return false;

        return Time.time >= nextUsableTime;
    }

    private void NotifyPotionChanged()
    {
        PotionChanged?.Invoke();
    }
}
