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
        statsText.color = Color.white;

        int hours = Mathf.FloorToInt(_playTimeSeconds / 3600f);
        int mins = Mathf.FloorToInt((_playTimeSeconds % 3600f) / 60f);
        int secs = Mathf.FloorToInt(_playTimeSeconds % 60f);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<b><color=#ffd900>Game Statistics</color></b>");
        sb.AppendLine();
        sb.AppendLine($"<color=#888888>Play Time</color>  <color=#66ff88><b>{hours:D2}:{mins:D2}:{secs:D2}</b></color>");
        sb.AppendLine($"<color=#888888>Level</color>  <color=#55ee88><b>{gm.PlayerState.Level}</b></color>");
        sb.AppendLine($"<color=#888888>Gold</color>  <color=#ffd900><b>\u25c6 {gm.PlayerState.Gold:N0}G</b></color>");
        string period = gm.TimeSystem.Period ?? "";
        string periodColor = period switch
        {
            "dawn"      => "#ffcc66",
            "morning"   => "#ffffdd",
            "afternoon" => "#ffffaa",
            "evening"   => "#ff9944",
            "night"     => "#88aaff",
            _           => "#aaaaaa"
        };
        string periodDisplay = period.Length > 0
            ? char.ToUpper(period[0]) + period.Substring(1)
            : period;
        sb.AppendLine($"<color=#888888>Game Hour</color>  <color=#88ccff><b>{gm.TimeSystem.GameHour:F1}</b></color>  <color={periodColor}>{periodDisplay}</color>");
        sb.AppendLine();

        var achievements = gm.Achievements.GetAll();
        int completed = 0;
        foreach (var a in achievements)
            if (gm.Achievements.IsCompleted(a.id)) completed++;
        string achColor = completed == achievements.Length ? "#ffd900" : completed > 0 ? "#66ff88" : "#888888";
        string achSuffix = completed == achievements.Length ? " <color=#ffd900>\u2605</color>" : "";
        sb.AppendLine($"<color=#888888>Achievements</color>  <color={achColor}><b>{completed}/{achievements.Length}</b></color>{achSuffix}");

        if (gm.WorldEvents.IsEventActive)
            sb.AppendLine($"\n<color=#ff9944>\u25cf Active Event: <b>{gm.WorldEvents.ActiveEvent?.name}</b></color>");

        statsText.text = sb.ToString();
    }

    public static float PlayTime => _playTimeSeconds;
    public static void SetPlayTime(float seconds) => _playTimeSeconds = seconds;
}
