using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillVFX : MonoBehaviour
{
    static readonly Dictionary<string, Sprite[]> _frameCache = new();

    public static void Show(MonoBehaviour context, string skillId, float fromX, float fromY, float toX, float toY)
    {
        if (context == null) return;
        string vfxName = ResolveVFXName(skillId);
        context.StartCoroutine(PlayEffect(vfxName, fromX, fromY, toX, toY));
    }

    public static void ShowAtPosition(MonoBehaviour context, string vfxName, float x, float y)
    {
        if (context == null) return;
        context.StartCoroutine(PlayBurst(vfxName ?? "vfx_hit_impact", x, y));
    }

    public static void ShowAtPosition(MonoBehaviour context, float x, float y)
    {
        ShowAtPosition(context, "vfx_hit_impact", x, y);
    }

    static IEnumerator PlayEffect(string vfxName, float fromX, float fromY, float toX, float toY)
    {
        var frames = LoadFrames(vfxName);
        var go = new GameObject($"VFX_{vfxName}");
        go.transform.position = new Vector3(fromX, fromY, 0);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 100;

        Vector3 from = new(fromX, fromY, 0);
        Vector3 to = new(toX, toY, 0);
        float distance = Vector3.Distance(from, to);
        float duration = Mathf.Clamp(distance / 300f, 0.15f, 0.6f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            go.transform.position = Vector3.Lerp(from, to, t);

            if (frames != null && frames.Length > 0)
            {
                int frameIdx = Mathf.Min((int)(t * frames.Length), frames.Length - 1);
                sr.sprite = frames[frameIdx];
            }

            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            sr.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        Object.Destroy(go);
    }

    static IEnumerator PlayBurst(string vfxName, float x, float y)
    {
        var frames = LoadFrames(vfxName);
        var go = new GameObject($"VFX_{vfxName}");
        go.transform.position = new Vector3(x, y, 0);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 100;

        float elapsed = 0f;
        const float duration = 0.3f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (frames != null && frames.Length > 0)
            {
                int frameIdx = Mathf.Min((int)(t * frames.Length), frames.Length - 1);
                sr.sprite = frames[frameIdx];
            }

            float scale = 0.5f + t * 1.5f;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        Object.Destroy(go);
    }

    static Sprite[] LoadFrames(string vfxName)
    {
        if (_frameCache.TryGetValue(vfxName, out var cached))
            return cached;

        var sprites = Resources.LoadAll<Sprite>($"VFX/{vfxName}");
        if (sprites == null || sprites.Length == 0)
            sprites = Resources.LoadAll<Sprite>(vfxName);

        _frameCache[vfxName] = sprites;
        return sprites;
    }

    static string ResolveVFXName(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return "vfx_hit_impact";
        return skillId switch
        {
            "slash" or "power_slash" or "whirlwind" => "vfx_slash",
            "fireball" or "meteor" or "fire_nova" => "vfx_fireball",
            "ice_bolt" or "blizzard" or "frost_nova" => "vfx_ice_bolt",
            "heal" or "rejuvenation" or "holy_light" => "vfx_heal",
            "lightning" or "chain_lightning" or "thunder" => "vfx_lightning",
            _ => "vfx_hit_impact"
        };
    }
}
