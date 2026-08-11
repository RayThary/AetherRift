using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance { get; private set; }

    [Header("Reference")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (promptRoot == null || promptText == null)
        {
            Debug.LogError("[InteractionPromptUI] Prompt Root와 Prompt Text를 연결해야 합니다.", this);
            enabled = false;
            return;
        }

        HidePrompt();
    }

    public void ShowPrompt(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            HidePrompt();
            return;
        }

        promptText.SetText(message);

        if (!promptRoot.activeSelf)
            promptRoot.SetActive(true);
    }

    public void HidePrompt()
    {
        if (promptRoot != null && promptRoot.activeSelf)
            promptRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
