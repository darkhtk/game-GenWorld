using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelUpVFX : MonoBehaviour
{
    const int ParticleCount = 16;
    const float Duration = 1.0f;
    const float BurstDuration = 0.55f;

    static Sprite[] _burstFrames;
    static Sprite _particleSprite;
    static Transform _vfxParent;

    public static void Spawn(MonoBehaviour context, Vector2 position)
    {
        if (context == null) return;
        EnsureAssets();
        context.StartCoroutine(PlayBurst(position));
        context.StartCoroutine(PlayParticles(position));
    }

    static void EnsureParent()
    {
        if (_vfxParent != null) return;
        var go = new GameObject("LevelUpVFXPool");
        Object.DontDestroyOnLoad(go);
        _vfxParent = go.transform;
    }

    static void EnsureAssets()
    {
        if (_burstFrames == null)
        {
            _burstFrames = Resources.LoadAll<Sprite>("VFX/vfx_levelup_burst");
            if (_burstFrames == null || _burstFrames.Length == 0)
                _burstFrames = CreateBurstFallback();
        }
        if (_particleSprite == null)
            _particleSprite = CreateParticleSprite();
    }

    static IEnumerator PlayBurst(Vector2 origin)
    {
        EnsureParent();
        var go = new GameObject("LevelUpBurst");
        go.transform.SetParent(_vfxParent);
        go.transform.position = new Vector3(origin.x, origin.y + 0.4f, 0);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 110;

        float elapsed = 0f;
        while (elapsed < BurstDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / BurstDuration);

            if (_burstFrames != null && _burstFrames.Length > 0)
            {
                int idx = Mathf.Min((int)(t * _burstFrames.Length), _burstFrames.Length - 1);
                sr.sprite = _burstFrames[idx];
            }

            float scale = Mathf.Lerp(0.6f, 2.4f, t);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            float alpha = t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f;
            sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
            yield return null;
        }

        Object.Destroy(go);
    }

    struct Spark
    {
        public GameObject go;
        public SpriteRenderer sr;
        public Vector2 vel;
        public float startScale;
        public float spin;
        public float spinSpeed;
    }

    static IEnumerator PlayParticles(Vector2 origin)
    {
        EnsureParent();
        var sparks = new List<Spark>(ParticleCount);
        for (int i = 0; i < ParticleCount; i++)
        {
            float angle = (i / (float)ParticleCount) * Mathf.PI * 2f + Random.Range(-0.18f, 0.18f);
            Vector2 dir = new(Mathf.Cos(angle), Mathf.Sin(angle));
            float speed = Random.Range(1.4f, 2.4f);
            Vector2 vel = dir * speed + Vector2.up * 0.6f;

            var go = new GameObject($"LevelUpSpark_{i}");
            go.transform.SetParent(_vfxParent);
            go.transform.position = new Vector3(origin.x, origin.y + 0.3f, 0);
            float startScale = Random.Range(0.18f, 0.28f);
            go.transform.localScale = new Vector3(startScale, startScale, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _particleSprite;
            sr.sortingOrder = 109;
            float warmth = Random.Range(0.85f, 1f);
            sr.color = new Color(1f, Mathf.Lerp(0.85f, 1f, warmth), Mathf.Lerp(0.3f, 0.7f, warmth), 1f);

            float spin = Random.Range(0f, 360f);
            float spinSpeed = Random.Range(-260f, 260f);
            go.transform.localEulerAngles = new Vector3(0, 0, spin);

            sparks.Add(new Spark
            {
                go = go, sr = sr, vel = vel,
                startScale = startScale, spin = spin, spinSpeed = spinSpeed
            });
        }

        float elapsed = 0f;
        while (elapsed < Duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            float t = Mathf.Clamp01(elapsed / Duration);
            float drag = Mathf.Lerp(1f, 0.65f, t);

            for (int i = 0; i < sparks.Count; i++)
            {
                var s = sparks[i];
                if (s.go == null) continue;

                s.vel = s.vel * drag - new Vector2(0, 1.4f * dt);
                s.go.transform.position += new Vector3(s.vel.x * dt, s.vel.y * dt, 0);

                float scale = Mathf.Lerp(s.startScale, s.startScale * 0.35f, t);
                s.go.transform.localScale = new Vector3(scale, scale, 1f);
                s.go.transform.localEulerAngles = new Vector3(0, 0, s.spin + s.spinSpeed * elapsed);

                float alpha = 1f - t * t;
                var c = s.sr.color;
                s.sr.color = new Color(c.r, c.g, c.b, alpha);

                sparks[i] = s;
            }
            yield return null;
        }

        for (int i = 0; i < sparks.Count; i++)
        {
            if (sparks[i].go != null) Object.Destroy(sparks[i].go);
        }
    }

    static Sprite[] CreateBurstFallback()
    {
        const int size = 32;
        var sprites = new Sprite[8];
        for (int f = 0; f < 8; f++)
        {
            float t = f / 7f;
            float radius = Mathf.Lerp(2f, 14f, Mathf.Sin(t * Mathf.PI));
            float alpha = Mathf.Sin(t * Mathf.PI);
            var tex = CreateRadialTexture(size, radius, new Color(1f, 0.92f, 0.5f, alpha));
            sprites[f] = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            Object.DontDestroyOnLoad(tex);
        }
        return sprites;
    }

    static Sprite CreateParticleSprite()
    {
        const int size = 8;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color32[size * size];
        float cx = size * 0.5f, cy = size * 0.5f, r = size * 0.42f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = dist <= r ? Mathf.Clamp01(1f - dist / r) : 0f;
                pixels[y * size + x] = new Color32(255, 240, 180, (byte)(a * 255f));
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        Object.DontDestroyOnLoad(tex);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }

    static Texture2D CreateRadialTexture(int size, float radius, Color color)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        float cx = size * 0.5f, cy = size * 0.5f;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = dist <= radius ? Mathf.Clamp01((1f - dist / radius) * color.a) : 0f;
                pixels[y * size + x] = new Color32(
                    (byte)(color.r * 255f), (byte)(color.g * 255f), (byte)(color.b * 255f), (byte)(a * 255f));
            }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }
}
