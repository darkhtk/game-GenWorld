using System.Collections;
using UnityEngine;

public class LootDropVFX : MonoBehaviour
{
    const float BounceDuration = 0.4f;
    const float BounceHeight = 0.8f;
    const float GlowFrameRate = 8f;
    const float GlowLifetime = 5f;

    static Sprite[] _glowFrames;
    static Sprite[] _pickupFrames;

    public static void SpawnDrop(MonoBehaviour context, Vector2 origin, Vector2 offset)
    {
        if (context == null) return;
        Vector2 target = origin + offset;
        context.StartCoroutine(PlayDrop(origin, target));
    }

    public static void SpawnPickup(MonoBehaviour context, Vector2 pos)
    {
        if (context == null) return;
        SkillVFX.ShowAtPosition(context, "vfx_loot_pickup", pos.x, pos.y);
    }

    static IEnumerator PlayDrop(Vector2 origin, Vector2 target)
    {
        var go = new GameObject("LootDrop_VFX");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 90;

        LoadGlowFrames();

        // Phase 1: bounce arc from origin to target
        float elapsed = 0f;
        while (elapsed < BounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / BounceDuration);

            Vector2 pos = Vector2.Lerp(origin, target, t);
            float arc = BounceHeight * Mathf.Sin(t * Mathf.PI);
            go.transform.position = new Vector3(pos.x, pos.y + arc, 0);

            if (_glowFrames != null && _glowFrames.Length > 0)
            {
                int frame = (int)(t * 3) % _glowFrames.Length;
                sr.sprite = _glowFrames[frame];
            }

            float scale = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }

        go.transform.position = new Vector3(target.x, target.y, 0);
        go.transform.localScale = Vector3.one;

        // Phase 2: idle glow loop until lifetime expires
        float glowElapsed = 0f;
        float frameTimer = 0f;
        int frameIdx = 0;

        while (glowElapsed < GlowLifetime)
        {
            glowElapsed += Time.deltaTime;
            frameTimer += Time.deltaTime;

            if (_glowFrames != null && _glowFrames.Length > 0 && frameTimer >= 1f / GlowFrameRate)
            {
                frameTimer = 0f;
                frameIdx = (frameIdx + 1) % _glowFrames.Length;
                sr.sprite = _glowFrames[frameIdx];
            }

            // Fade out in last second
            if (glowElapsed > GlowLifetime - 1f)
            {
                float fade = 1f - (glowElapsed - (GlowLifetime - 1f));
                sr.color = new Color(1f, 1f, 1f, fade);
            }

            yield return null;
        }

        Object.Destroy(go);
    }

    static void LoadGlowFrames()
    {
        if (_glowFrames != null) return;
        _glowFrames = Resources.LoadAll<Sprite>("VFX/vfx_loot_glow");
        if (_glowFrames == null || _glowFrames.Length == 0)
            _glowFrames = Resources.LoadAll<Sprite>("vfx_loot_glow");
    }
}
