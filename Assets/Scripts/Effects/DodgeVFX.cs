using UnityEngine;

public static class DodgeVFX
{
    static readonly Color TrailColor = new(0.533f, 0.867f, 1f, 0.3f); // #88ddff

    public static void SpawnTrail(SpriteRenderer source, Vector2 position)
    {
        if (source == null || source.sprite == null) return;
        var go = new GameObject("DodgeTrail");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.color = TrailColor;
        sr.sortingOrder = source.sortingOrder - 1;
        go.transform.position = position;
        go.transform.localScale = source.transform.localScale;
        Object.Destroy(go, 0.15f);
    }
}
