using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillVFX : MonoBehaviour
{
    static readonly Dictionary<string, Sprite[]> _frameCache = new();
    static readonly Dictionary<string, Sprite[]> _fallbackCache = new();
    static readonly Queue<(GameObject go, SpriteRenderer sr)> _vfxPool = new();
    static Transform _vfxParent;

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

    public static void ClearPool()
    {
        _frameCache.Clear();
        _fallbackCache.Clear();
        while (_vfxPool.Count > 0)
        {
            var item = _vfxPool.Dequeue();
            if (item.go != null) Object.Destroy(item.go);
        }
        if (_vfxParent != null) Object.Destroy(_vfxParent.gameObject);
        _vfxParent = null;
    }

    static void EnsureParent()
    {
        if (_vfxParent != null) return;
        var go = new GameObject("SkillVFXPool");
        Object.DontDestroyOnLoad(go);
        _vfxParent = go.transform;
    }

    static (GameObject go, SpriteRenderer sr) GetVFXObject(string name)
    {
        EnsureParent();
        if (_vfxPool.Count > 0)
        {
            var item = _vfxPool.Dequeue();
            item.go.name = $"VFX_{name}";
            item.go.transform.SetParent(_vfxParent);
            item.go.SetActive(true);
            return item;
        }
        var go = new GameObject($"VFX_{name}");
        go.transform.SetParent(_vfxParent);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 100;
        return (go, sr);
    }

    static void ReturnVFXObject(GameObject go, SpriteRenderer sr)
    {
        if (go == null) return;
        sr.sprite = null;
        go.SetActive(false);
        if (_vfxParent == null) { Object.Destroy(go); return; }
        go.transform.SetParent(_vfxParent);
        _vfxPool.Enqueue((go, sr));
    }

    static IEnumerator PlayEffect(string vfxName, float fromX, float fromY, float toX, float toY)
    {
        var frames = LoadFrames(vfxName);
        var (go, sr) = GetVFXObject(vfxName);
        go.transform.position = new Vector3(fromX, fromY, 0);

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

        ReturnVFXObject(go, sr);
    }

    static IEnumerator PlayBurst(string vfxName, float x, float y)
    {
        var frames = LoadFrames(vfxName);
        var (go, sr) = GetVFXObject(vfxName);
        go.transform.position = new Vector3(x, y, 0);
        go.transform.localScale = Vector3.one;

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

        ReturnVFXObject(go, sr);
    }

    static Sprite[] LoadFrames(string vfxName)
    {
        if (_frameCache.TryGetValue(vfxName, out var cached))
            return cached;

        var sprites = Resources.LoadAll<Sprite>($"VFX/{vfxName}");
        if (sprites == null || sprites.Length == 0)
            sprites = Resources.LoadAll<Sprite>(vfxName);

        if (!HasVisibleContent(sprites))
            sprites = CreateFallbackFrames(vfxName);

        _frameCache[vfxName] = sprites;
        return sprites;
    }

    static bool HasVisibleContent(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return false;
        var first = sprites[0];
        if (first == null || first.texture == null) return false;
        try
        {
            var rect = first.textureRect;
            int cx = Mathf.RoundToInt(rect.x + rect.width * 0.5f);
            int cy = Mathf.RoundToInt(rect.y + rect.height * 0.5f);
            for (int dy = -4; dy <= 4; dy += 2)
                for (int dx = -4; dx <= 4; dx += 2)
                    if (first.texture.GetPixel(cx + dx, cy + dy).a > 0.05f)
                        return true;
            return false;
        }
        catch { return true; }
    }

    static Sprite[] CreateFallbackFrames(string vfxName)
    {
        if (_fallbackCache.TryGetValue(vfxName, out var cached)) return cached;

        Color color = vfxName switch
        {
            "vfx_slash" => new Color(1f, 0.85f, 0.2f),
            "vfx_fireball" => new Color(1f, 0.4f, 0.1f),
            "vfx_ice_bolt" => new Color(0.4f, 0.85f, 1f),
            "vfx_heal" => new Color(0.3f, 1f, 0.4f),
            "vfx_lightning" => new Color(0.85f, 0.85f, 1f),
            _ => new Color(1f, 0.9f, 0.5f)
        };

        const int size = 32;
        const int frameCount = 4;
        var sprites = new Sprite[frameCount];
        for (int f = 0; f < frameCount; f++)
        {
            float alpha = Mathf.Lerp(1f, 0.4f, f / (float)(frameCount - 1));
            var tex = CreateCircleTexture(size, new Color(color.r, color.g, color.b, alpha));
            sprites[f] = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            Object.DontDestroyOnLoad(tex);
        }

        _fallbackCache[vfxName] = sprites;
        return sprites;
    }

    static Texture2D CreateCircleTexture(int size, Color fill)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float cx = size * 0.5f, cy = size * 0.5f, r = size * 0.44f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = dist <= r ? (byte)(fill.a * 255f) : (byte)0;
                pixels[y * size + x] = new Color32(
                    (byte)(fill.r * 255f), (byte)(fill.g * 255f), (byte)(fill.b * 255f), a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    public static string ResolveVFXName(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return "vfx_hit_impact";
        return skillId switch
        {
            "slash" or "power_slash" or "whirlwind"
                or "heavy_strike" or "spin_slash" or "shield_charge"
                or "ground_slam" or "earthquake" or "berserker_fury"
                or "war_cry" or "rage" => "vfx_slash",
            "fireball" or "meteor" or "fire_nova"
                or "explosive_arrow" or "arcane_storm" => "vfx_fireball",
            "ice_bolt" or "blizzard" or "frost_nova" => "vfx_ice_bolt",
            "heal" or "rejuvenation" or "holy_light"
                or "mana_shield" => "vfx_heal",
            "lightning" or "chain_lightning" or "thunder" => "vfx_lightning",
            "poison_arrow" => "vfx_hit_impact",
            "piercing_arrow" or "multi_shot" or "arrow_rain"
                or "trap" or "stealth" or "assassinate" or "shadow_step" => "vfx_hit_impact",
            _ => "vfx_hit_impact"
        };
    }

    public static void ValidateSkillAnimations(Dictionary<string, SkillDef> skills)
    {
        if (skills == null) return;
        string[] stages = { "cast", "projectile", "impact" };
        int missing = 0;
        foreach (var kv in skills)
        {
            var def = kv.Value;
            if (def.animationDef == null) continue;
            foreach (var stage in stages)
            {
                if (!def.animationDef.HasClip(stage))
                {
                    Debug.LogWarning($"[SkillVFX] Skill '{def.name}' missing animation clip: {stage}");
                    missing++;
                }
            }
        }
        if (missing == 0) Debug.Log("[SkillVFX] All skill animations validated OK");
    }
}
