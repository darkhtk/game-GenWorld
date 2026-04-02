using UnityEngine;

public class TimeSystem
{
    public float GameHour { get; set; } = 8f; // Start at 8am

    public string Period => GameHour switch
    {
        < 6 => "night",
        < 9 => "dawn",
        < 12 => "morning",
        < 17 => "afternoon",
        < 20 => "evening",
        _ => "night"
    };

    const float RealSecondsPerGameHour = 60f;

    string _lastPeriod;

    public bool Update(float deltaTime)
    {
        string prevPeriod = Period;
        GameHour += deltaTime / RealSecondsPerGameHour;
        if (GameHour >= 24f) GameHour -= 24f;

        bool periodChanged = prevPeriod != Period;
        if (periodChanged)
            Debug.Log($"[TimeSystem] Period changed: {prevPeriod} → {Period} (hour {GameHour:F1})");
        return periodChanged;
    }
}
