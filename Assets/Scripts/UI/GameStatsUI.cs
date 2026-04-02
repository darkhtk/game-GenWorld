using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameStatsUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI statsText;
    [SerializeField] Button closeButton;

    static float _playTimeSeconds;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(() => panel?.SetActive(false));
    }

    void Update()
    {
        _playTimeSeconds += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.P) && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
        {
            if (panel != null)
            {
                bool show = !panel.activeSelf;
                panel.SetActive(show);
                if (show) RefreshStats();
            }
        }
    }

    void RefreshStats()
    {
        if (statsText == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        int hours = Mathf.FloorToInt(_playTimeSeconds / 3600f);
        int mins = Mathf.FloorToInt((_playTimeSeconds % 3600f) / 60f);
        int secs = Mathf.FloorToInt(_playTimeSeconds % 60f);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b>Game Statistics</b>");
        sb.AppendLine();
        sb.AppendLine($"Play Time: {hours:D2}:{mins:D2}:{secs:D2}");
        sb.AppendLine($"Level: {gm.PlayerState.Level}");
        sb.AppendLine($"Gold: {gm.PlayerState.Gold:N0}");
        sb.AppendLine($"Game Hour: {gm.TimeSystem.GameHour:F1} ({gm.TimeSystem.Period})");
        sb.AppendLine();

        var achievements = gm.Achievements.GetAll();
        int completed = 0;
        foreach (var a in achievements)
            if (gm.Achievements.IsCompleted(a.id)) completed++;
        sb.AppendLine($"Achievements: {completed}/{achievements.Length}");

        if (gm.WorldEvents.IsEventActive)
            sb.AppendLine($"Active Event: {gm.WorldEvents.ActiveEvent?.name}");

        statsText.text = sb.ToString();
    }

    public static float PlayTime => _playTimeSeconds;
    public static void SetPlayTime(float seconds) => _playTimeSeconds = seconds;
}
