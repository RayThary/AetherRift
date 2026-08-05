using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Image currentHealthFill;
    [SerializeField] private Image delayedHealthFill;

    [Header("Delayed Health")]
    [SerializeField] private float delayedHealthWaitTime = 0.4f;
    [SerializeField] private float delayedHealthDecreaseDuration = 0.5f;

    private PlayerHealth playerHealth;

    private float lastHealthNormalized;
    private float delayedHealthStartFill;
    private float delayedHealthWaitTimer;
    private float delayedHealthDecreaseTimer;

    private void Awake()
    {
        if (currentHealthFill == null || delayedHealthFill == null)
        {
            Debug.LogError("[PlayerHealthUI] 체력바 이미지가 연결되지 않았습니다.", this);
            enabled = false;
            return;
        }

        InitializeHealthBar(0f);
    }

    private void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentPlayer == null)
        {
            Debug.LogError("[PlayerHealthUI] 현재 플레이어를 찾을 수 없습니다.", this);
            return;
        }

        playerHealth = GameManager.Instance.CurrentPlayer.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            Debug.LogError("[PlayerHealthUI] 현재 플레이어에 PlayerHealth가 없습니다.", GameManager.Instance.CurrentPlayer);
            return;
        }

        InitializeHealthBar(playerHealth.HealthNormalized);
    }

    private void Update()
    {
        if (playerHealth != null)
            UpdateHealthBar();
    }

    private void InitializeHealthBar(float healthNormalized)
    {
        healthNormalized = Mathf.Clamp01(healthNormalized);

        currentHealthFill.fillAmount = healthNormalized;
        delayedHealthFill.fillAmount = healthNormalized;

        lastHealthNormalized = healthNormalized;
        delayedHealthStartFill = healthNormalized;
        delayedHealthWaitTimer = 0f;
        delayedHealthDecreaseTimer = 0f;
    }

    private void UpdateHealthBar()
    {
        float targetHealth = Mathf.Clamp01(playerHealth.HealthNormalized);

        if (targetHealth < lastHealthNormalized)
        {
            currentHealthFill.fillAmount = targetHealth;

            delayedHealthStartFill = delayedHealthFill.fillAmount;
            delayedHealthWaitTimer = delayedHealthWaitTime;
            delayedHealthDecreaseTimer = 0f;
        }
        else if (targetHealth > lastHealthNormalized)
        {
            currentHealthFill.fillAmount = targetHealth;
            delayedHealthFill.fillAmount = targetHealth;

            delayedHealthStartFill = targetHealth;
            delayedHealthWaitTimer = 0f;
            delayedHealthDecreaseTimer = 0f;
        }

        lastHealthNormalized = targetHealth;

        if (delayedHealthFill.fillAmount <= targetHealth)
        {
            delayedHealthFill.fillAmount = targetHealth;
            return;
        }

        if (delayedHealthWaitTimer > 0f)
        {
            delayedHealthWaitTimer -= Time.deltaTime;
            return;
        }

        delayedHealthDecreaseTimer += Time.deltaTime;

        float duration = Mathf.Max(delayedHealthDecreaseDuration, 0.01f);
        float decreaseProgress = Mathf.Clamp01(delayedHealthDecreaseTimer / duration);

        delayedHealthFill.fillAmount = Mathf.Lerp(delayedHealthStartFill, targetHealth, decreaseProgress);
    }
}