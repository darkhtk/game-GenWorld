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

    public static void Spawn(MonoBehaviour context, Vector2 pos, int amount, bool isCrit, Color? color = null)
    {
        if (context == null) return;
        Color c = color ?? (isCrit ? Color.yellow : Color.white);
        context.StartCoroutine(AnimateText(pos, amount.ToString(), isCrit, c));
    }

    public static void SpawnText(MonoBehaviour context, Vector2 pos, string msg, Color? color = null)
    {
        if (context == null) return;
        context.StartCoroutine(AnimateText(pos, msg, false, color ?? Color.white));
    }

    static IEnumerator AnimateText(Vector2 pos, string text, bool isCrit, Color color)
    {
        var go = new GameObject("DamageText");
        go.transform.position = new Vector3(pos.x, pos.y, 0);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.color = color;
        tmp.fontSize = isCrit ? CritFontSize : NormalFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 200;

        float elapsed = 0f;
        Vector3 start = go.transform.position;
        float randomX = Random.Range(-0.3f, 0.3f);

        while (elapsed < RiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / RiseDuration);

            float y = RiseHeight * t;
            float x = randomX * t;
            go.transform.position = start + new Vector3(x, y, 0);

            if (isCrit)
            {
                float scale = CritScale * (1f + 0.3f * Mathf.Sin(t * Mathf.PI));
                go.transform.localScale = Vector3.one * scale;
            }

            float alpha = t > 0.6f ? 1f - (t - 0.6f) / 0.4f : 1f;
            tmp.color = new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        Object.Destroy(go);
    }
}
