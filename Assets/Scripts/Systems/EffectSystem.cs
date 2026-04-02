using System.Collections.Generic;
using UnityEngine;

public class ActiveEffect
{
    public float expiresAt;
    public float value;
    public float interval;
    public float lastTick;
    public float totalDuration;
}

public struct ActiveEffectInfo
{
    public string type;
    public float expires;
    public float totalDuration;
    public float value;
}

public class EffectHolder
{
    public const float MaxStunMs = 10000f;
    public const float MinSlow = 0.1f;

    readonly Dictionary<string, ActiveEffect> _effects = new();

    public void Apply(string type, float expiresAt, float value)
    {
        if (type == "stun")
        {
            float now = Time.time * 1000f;
            float maxExpiry = now + MaxStunMs;
            expiresAt = Mathf.Min(expiresAt, maxExpiry);
            if (_effects.TryGetValue("stun", out var existing))
            {
                existing.expiresAt = Mathf.Max(existing.expiresAt, expiresAt);
                return;
            }
        }
        else if (type == "slow")
        {
            value = Mathf.Clamp(value, MinSlow, 1f);
            if (_effects.TryGetValue("slow", out var existing))
            {
                existing.expiresAt = Mathf.Max(existing.expiresAt, expiresAt);
                existing.value = Mathf.Min(existing.value, value);
                return;
            }
        }
        float now = Time.time * 1000f;
        _effects[type] = new ActiveEffect { expiresAt = expiresAt, value = value, totalDuration = expiresAt - now };
    }

    public void ApplyDot(float expiresAt, float damage, float interval = 1000f)
    {
        float now = Time.time * 1000f;
        _effects["dot"] = new ActiveEffect
        {
            expiresAt = expiresAt, value = damage,
            interval = interval, lastTick = 0f,
            totalDuration = expiresAt - now
        };
    }

    public bool Has(string type) => _effects.ContainsKey(type);

    public float GetValue(string type) =>
        _effects.TryGetValue(type, out var e) ? e.value : 0f;

    public float GetExpiresAt(string type) =>
        _effects.TryGetValue(type, out var e) ? e.expiresAt : 0f;

    public void Remove(string type) => _effects.Remove(type);

    public List<string> Tick(float now)
    {
        var expired = new List<string>();
        var keys = new List<string>(_effects.Keys);
        foreach (var key in keys)
        {
            if (_effects[key].expiresAt <= now)
            {
                expired.Add(key);
                _effects.Remove(key);
            }
        }
        return expired;
    }

    public float TickDot(float now)
    {
        if (!_effects.TryGetValue("dot", out var dot)) return 0f;
        if (now - dot.lastTick >= dot.interval)
        {
            dot.lastTick = now;
            return dot.value;
        }
        return 0f;
    }

    public void Clear() => _effects.Clear();
}
