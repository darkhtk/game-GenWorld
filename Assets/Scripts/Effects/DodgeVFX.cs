using UnityEngine;

public static class DodgeVFX
{
    static readonly Color TrailColor = new(0.533f, 0.867f, 1f, 0.3f); // #88ddff

    static Sprite[] _ghostFrames;
    static int _ghostIndex;
    static bool _ghostLoaded;

    static Sprite[] LoadGhostFrames()
    {
        if (_ghostLoaded) return _ghostFrames;
        _ghostLoaded = true;
        var loaded = Resources.LoadAll<Sprite>("VFX/vfx_dodge_trail");
        if (loaded != null && loaded.Length > 0)
        {
            System.Array.Sort(loaded, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            _ghostFrames = loaded;
        }
        return _ghostFrames;
    }

    public static void SpawnTrail(SpriteRenderer source, Vector2 position)
    {
        if (source == null || source.sprite == null) return;

        var go = new GameObject("DodgeTrail");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.color = TrailColor;
        sr.sortingOrder = source.sortingOrder > 0 ? source.sortingOrder - 1 : 0;
        go.transform.position = position;
        go.transform.localScale = source.transform.localScale;
        Object.Destroy(go, 0.15f);

        var frames = LoadGhostFrames();
        if (frames == null || frames.Length == 0) return;

        var ghost = new GameObject("DodgeTrailGhost");
        var gr = ghost.AddComponent<SpriteRenderer>();
        gr.sprite = frames[_ghostIndex % frames.Length];
        _ghostIndex++;
        gr.sortingOrder = sr.sortingOrder - 1;
        ghost.transform.position = position;
        ghost.transform.localScale = source.transform.localScale;
        Object.Destroy(ghost, 0.18f);
    }
}
