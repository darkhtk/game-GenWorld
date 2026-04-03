using System;
using System.Collections.Generic;
using UnityEngine;

public static class CombatSystem
{
    static Camera _cachedCam;

    public static bool IsOnScreen(Vector2 worldPos, float margin = 0.05f)
    {
        if (_cachedCam == null) _cachedCam = Camera.main;
        if (_cachedCam == null) return true;
        Vector3 vp = _cachedCam.WorldToViewportPoint(worldPos);
        return vp.x >= -margin && vp.x <= 1f + margin
            && vp.y >= -margin && vp.y <= 1f + margin;
    }

    public static int CalcDamage(int atk, int def, bool isCrit)
    {
        int baseDmg = Mathf.Max(1, atk - def);
        return isCrit ? baseDmg * 2 : baseDmg;
    }

    public static bool CalcCrit(int critChance, Func<float> roll = null)
    {
        float r = roll != null ? roll() : UnityEngine.Random.value;
        return r < critChance / 100f;
    }

    public static T FindClosest<T>(Vector2 origin, T[] targets, float range,
        Func<T, Vector2> getPos, Func<T, bool> isAlive) where T : class
    {
        T closest = null;
        float bestDist = range * range;
        foreach (var t in targets)
        {
            if (!isAlive(t)) continue;
            float dist = (getPos(t) - origin).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                closest = t;
            }
        }
        return closest;
    }

    public static T FindClosest<T>(Vector2 origin, List<T> targets, float range,
        Func<T, Vector2> getPos, Func<T, bool> isAlive) where T : class
    {
        T closest = null;
        float bestDist = range * range;
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (!isAlive(t)) continue;
            float dist = (getPos(t) - origin).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                closest = t;
            }
        }
        return closest;
    }
}
