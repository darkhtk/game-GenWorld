using System.Collections;
using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    const float RiseDuration = 0.8f;
    const float RiseHeight = 1.2f;
    const float CritScale = 1.5f;
    const float NormalFontSize = 4f;
    const float CritFontSize = 6f;

    static ObjectPool<DamageText> _pool;
    static Transform _poolParent;

    TextMeshPro _tmp;
    bool _inUse;

    static void EnsurePool()
    {
        if (_pool != null) return;
        _poolParent = new GameObject("DamageTextPool").transform;
        Object.DontDestroyOnLoad(_poolParent.gameObject);
        _pool = new ObjectPool<DamageText>(() =>
        {
            var go = new GameObject("DamageText");
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 200;
            var dt = go.AddComponent<DamageText>();
            dt._tmp = tmp;
            return dt;
        }, _poolParent, 30, 80);
    }

    public static void ClearPool()
    {
        _pool?.Clear();
        _pool = null;
        if (_poolParent != null) Object.Destroy(_poolParent.gameObject);
        _poolParent = null;
    }

    public static void Spawn(MonoBehaviour context, Vector2 pos, int amount, bool isCrit, Color? color = null)
    {
        if (context == null) return;
        Color c = color ?? (isCrit ? new Color(1f, 0.867f, 0.267f) : Color.white); // crit = #ffdd44
        EnsurePool();
        var dt = _pool.Get();
        if (dt != null)
            dt.Run(pos, amount.ToString(), isCrit, c);
    }

    public static void SpawnText(MonoBehaviour context, Vector2 pos, string msg, Color? color = null)
    {
        if (context == null) return;
        EnsurePool();
        var dt = _pool.Get();
        if (dt != null)
            dt.Run(pos, msg, false, color ?? Color.white);
    }

    void Run(Vector2 pos, string text, bool isCrit, Color color)
    {
        _inUse = true;
        transform.SetParent(null);
        transform.position = new Vector3(pos.x, pos.y, 0);
        transform.localScale = Vector3.one;

        if (_tmp == null) _tmp = GetComponent<TextMeshPro>();
        _tmp.text = isCrit ? $"<b>{text}!</b>" : text;
        _tmp.color = color;
        _tmp.fontSize = isCrit ? CritFontSize : NormalFontSize;

        StartCoroutine(Animate(isCrit, color));
    }

    IEnumerator Animate(bool isCrit, Color color)
    {
        float elapsed = 0f;
        Vector3 start = transform.position;
        float randomX = Random.Range(-0.3f, 0.3f);

        while (elapsed < RiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / RiseDuration);

            float y = RiseHeight * t;
            float x = randomX * t;
            transform.position = start + new Vector3(x, y, 0);

            if (isCrit)
            {
                float scale = CritScale * (1f + 0.3f * Mathf.Sin(t * Mathf.PI));
                transform.localScale = Vector3.one * scale;
            }

            float alpha = t > 0.6f ? 1f - (t - 0.6f) / 0.4f : 1f;
            _tmp.color = new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        ReturnSelf();
    }

    void ReturnSelf()
    {
        if (!_inUse) return;
        _inUse = false;
        _pool?.Return(this);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        ReturnSelf();
    }
}
