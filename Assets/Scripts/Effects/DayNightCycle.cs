using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightCycle : MonoBehaviour
{
    [SerializeField] Light2D globalLight;

    static readonly Dictionary<string, (Color color, float intensity)> LightPresets = new()
    {
        { "dawn",      (new Color(1.0f, 0.85f, 0.7f), 0.7f) },
        { "morning",   (new Color(1.0f, 1.0f, 0.95f), 1.0f) },
        { "afternoon", (new Color(1.0f, 0.95f, 0.9f), 1.0f) },
        { "evening",   (new Color(0.9f, 0.6f, 0.4f), 0.6f) },
        { "night",     (new Color(0.3f, 0.3f, 0.6f), 0.3f) },
    };

    string _currentPeriod;
    Color _fromColor, _toColor;
    float _fromIntensity, _toIntensity;
    float _lerpProgress = 1f;
    const float TransitionDuration = 10f;

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm?.TimeSystem == null) return;
        _currentPeriod = gm.TimeSystem.Period;
        ApplyImmediate(_currentPeriod);
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm?.TimeSystem == null || globalLight == null) return;

        string period = gm.TimeSystem.Period;
        if (period != _currentPeriod)
        {
            StartTransition(_currentPeriod, period);
            _currentPeriod = period;
        }

        if (_lerpProgress < 1f)
        {
            _lerpProgress += Time.deltaTime / TransitionDuration;
            float t = Mathf.Clamp01(_lerpProgress);
            globalLight.color = Color.Lerp(_fromColor, _toColor, t);
            globalLight.intensity = Mathf.Lerp(_fromIntensity, _toIntensity, t);
        }
    }

    void StartTransition(string from, string to)
    {
        _fromColor = globalLight.color;
        _fromIntensity = globalLight.intensity;
        if (LightPresets.TryGetValue(to, out var preset))
        {
            _toColor = preset.color;
            _toIntensity = preset.intensity;
        }
        _lerpProgress = 0f;
    }

    void ApplyImmediate(string period)
    {
        if (globalLight == null) return;
        if (LightPresets.TryGetValue(period, out var preset))
        {
            globalLight.color = preset.color;
            globalLight.intensity = preset.intensity;
        }
    }
}
