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
                existing.totalDuration = existing.expiresAt - now;
                return;
            }
        }
        else if (type == "slow")
        {
            value = Mathf.Clamp(value, MinSlow, 1f);
            if (_effects.TryGetValue("slow", out var existing))
            {
                float now = Time.time * 1000f;
                existing.expiresAt = Mathf.Max(existing.expiresAt, expiresAt);
                existing.value = Mathf.Min(existing.value, value);
                existing.totalDuration = existing.expiresAt - now;
                return;
            }
        }
        float nowMs = Time.time * 1000f;
        _effects[type] = new ActiveEffect { expiresAt = expiresAt, value = value, totalDuration = expiresAt - nowMs };
    }

    public void ApplyDot(float expiresAt, float damage, float interval = 1000f)
    {
        float now = Time.time * 1000f;
        if (_effects.TryGetValue("dot", out var existing))
        {
            existing.expiresAt = Mathf.Max(existing.expiresAt, expiresAt);
            existing.totalDuration = existing.expiresAt - now;
            if (damage > existing.value)
                existing.value = damage;
            return;
        }
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

    readonly List<string> _tickRemoveBuffer = new();

    public List<string> Tick(float now)
    {
        _tickRemoveBuffer.Clear();
        foreach (var kv in _effects)
        {
            if (kv.Value.expiresAt <= now)
                _tickRemoveBuffer.Add(kv.Key);
        }
        for (int i = 0; i < _tickRemoveBuffer.Count; i++)
            _effects.Remove(_tickRemoveBuffer[i]);
        return _tickRemoveBuffer;
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

    readonly List<ActiveEffectInfo> _activeBuffer = new();

    public List<ActiveEffectInfo> GetActive(float now)
    {
        _activeBuffer.Clear();
        foreach (var kv in _effects)
        {
            if (kv.Value.expiresAt > now)
            {
                _activeBuffer.Add(new ActiveEffectInfo
                {
                    type = kv.Key,
                    expires = kv.Value.expiresAt,
                    totalDuration = kv.Value.totalDuration,
                    value = kv.Value.value
                });
            }
        }
        return _activeBuffer;
    }

    public void Clear() => _effects.Clear();
}
