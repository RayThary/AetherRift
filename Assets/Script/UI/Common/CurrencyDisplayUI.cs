using TMPro;
using UnityEngine;

public class CurrencyDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text currencyText;
    [SerializeField] private string format = "골드: {0}";

    private void OnEnable()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.ProgressChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (GameProgressManager.Instance != null)
            GameProgressManager.Instance.ProgressChanged -= Refresh;
    }

    public void Refresh()
    {
        if (currencyText == null)
            return;

        int currency = GameProgressManager.Instance != null ? GameProgressManager.Instance.Currency : 0;
        currencyText.text = string.Format(format, currency);
    }
}
