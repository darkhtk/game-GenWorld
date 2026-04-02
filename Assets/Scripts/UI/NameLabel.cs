using UnityEngine;
using TMPro;

public static class NameLabel
{
    public static void Create(Transform parent, string text, Color color, float yOffset)
    {
        if (parent == null || string.IsNullOrEmpty(text)) return;

        var go = new GameObject("NameLabel");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, yOffset, 0);

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 3f;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 150;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
    }
}
