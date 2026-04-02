using System.Collections;
using UnityEngine;

public class SkillVFX : MonoBehaviour
{
    [SerializeField] SpriteRenderer vfxRenderer;
    [SerializeField] Sprite[] effectFrames;

    static readonly Color DefaultColor = Color.white;

    public static void Show(MonoBehaviour context, string skillId, float fromX, float fromY, float toX, float toY)
    {
        if (context == null) return;
        context.StartCoroutine(PlayEffect(context.transform, skillId, fromX, fromY, toX, toY));
    }

    static IEnumerator PlayEffect(Transform parent, string skillId, float fromX, float fromY, float toX, float toY)
    {
        var go = new GameObject($"VFX_{skillId}");
        go.transform.position = new Vector3(fromX, fromY, 0);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 100;
        sr.color = GetSkillColor(skillId);

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

            float alpha = t < 0.5f ? 1f : 1f - (t - 0.5f) * 2f;
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
            yield return null;
        }

        Object.Destroy(go);
    }

    static Color GetSkillColor(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return DefaultColor;
        int hash = skillId.GetHashCode();
        float h = Mathf.Abs(hash % 360) / 360f;
        return Color.HSVToRGB(h, 0.7f, 1f);
    }
}
