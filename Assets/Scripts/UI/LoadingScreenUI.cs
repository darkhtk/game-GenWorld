using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Image progressBar;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] TextMeshProUGUI percentText;

    public static LoadingScreenUI Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public void Show() { if (panel != null) panel.SetActive(true); SetProgress(0, "<color=#888888>Loading...</color>"); }
    public void Hide() { if (panel != null) panel.SetActive(false); }

    public void SetProgress(float progress, string status = null)
    {
        if (progressBar != null) progressBar.fillAmount = Mathf.Clamp01(progress);
        if (percentText != null)
        {
            percentText.color = Color.white;
            int pct = Mathf.RoundToInt(progress * 100);
            string pctColor = pct >= 100 ? "#66ff88" : pct >= 50 ? "#88ddff" : "#6699aa";
            percentText.text = $"<color={pctColor}><b>{pct}%</b></color>";
        }
        if (statusText != null && !string.IsNullOrEmpty(status)) { statusText.color = Color.white; statusText.text = status; }
    }

    public IEnumerator SimulateLoading(string[] steps)
    {
        Show();
        for (int i = 0; i < steps.Length; i++)
        {
            SetProgress((float)i / steps.Length, steps[i]);
            yield return new WaitForSeconds(0.15f);
        }
        SetProgress(1f, "<color=#66ff88><b>Complete!</b></color>");
        yield return new WaitForSeconds(0.3f);
        Hide();
    }
}
