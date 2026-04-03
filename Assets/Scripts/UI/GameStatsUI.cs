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
        sb.AppendLine("<b><color=#ffd900>Game Statistics</color></b>");
        sb.AppendLine();
        sb.AppendLine($"Play Time: <color=#aaffaa>{hours:D2}:{mins:D2}:{secs:D2}</color>");
        sb.AppendLine($"Level: <color=#99ff99>{gm.PlayerState.Level}</color>");
        sb.AppendLine($"Gold: <color=#ffd900>{gm.PlayerState.Gold:N0}G</color>");
        sb.AppendLine($"Game Hour: <color=#88ccff>{gm.TimeSystem.GameHour:F1}</color> ({gm.TimeSystem.Period})");
        sb.AppendLine();

        var achievements = gm.Achievements.GetAll();
        int completed = 0;
        foreach (var a in achievements)
            if (gm.Achievements.IsCompleted(a.id)) completed++;
        string achColor = completed == achievements.Length ? "#ffd900" : "#ffffff";
        sb.AppendLine($"Achievements: <color={achColor}>{completed}/{achievements.Length}</color>");

        if (gm.WorldEvents.IsEventActive)
            sb.AppendLine($"<color=#ff9966>Active Event: {gm.WorldEvents.ActiveEvent?.name}</color>");

        statsText.text = sb.ToString();
    }

    public static float PlayTime => _playTimeSeconds;
    public static void SetPlayTime(float seconds) => _playTimeSeconds = seconds;
}
