using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DeathMarker : MonoBehaviour
{
    const float InteractRange = 48f;
    const float TimeoutSeconds = 120f;
    const float BobAmplitude = 0.15f;
    const float BobSpeed = 2f;

    List<ItemInstance> _droppedItems;
    float _spawnTime;
    Vector3 _basePosition;
    TextMeshPro _text;

    public static DeathMarker Create(Vector2 position, List<ItemInstance> items)
    {
        var go = new GameObject("DeathMarker");
        go.transform.position = new Vector3(position.x, position.y, 0);

        var marker = go.AddComponent<DeathMarker>();
        marker._droppedItems = items ?? new List<ItemInstance>();
        marker._spawnTime = Time.time;
        marker._basePosition = go.transform.position;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "<color=#ff6655>\u2020</color>"; // † dagger symbol
        tmp.fontSize = 8f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.sortingOrder = 150;
        tmp.outlineColor = new Color32(0, 0, 0, 180);
        tmp.outlineWidth = 0.12f;
        marker._text = tmp;

        return marker;
    }

    public List<ItemInstance> DroppedItems => _droppedItems;
    public bool HasItems => _droppedItems != null && _droppedItems.Count > 0;

    void Update()
    {
        if (Time.time - _spawnTime > TimeoutSeconds)
        {
            Destroy(gameObject);
            return;
        }

        float bob = Mathf.Sin(Time.time * BobSpeed) * BobAmplitude;
        transform.position = _basePosition + new Vector3(0, bob, 0);

        float remaining = TimeoutSeconds - (Time.time - _spawnTime);
        if (remaining < 10f && _text != null)
        {
            float blink = Mathf.PingPong(Time.time * 4f, 1f);
            _text.alpha = 0.3f + 0.7f * blink;
            _text.text = $"<color=#ff6655>\u2020</color>\n<size=4><color=#ff9944><b>{Mathf.CeilToInt(remaining)}s</b></color></size>";
        }
        else if (remaining >= 10f && _text != null && _text.text != "<color=#ff6655>\u2020</color>")
        {
            _text.text = "<color=#ff6655>\u2020</color>";
            _text.alpha = 1f;
        }
    }

    public bool IsInRange(Vector2 playerPos)
    {
        return ((Vector2)transform.position - playerPos).sqrMagnitude <= InteractRange * InteractRange;
    }

    public List<ItemInstance> Recover()
    {
        var items = _droppedItems;
        _droppedItems = new List<ItemInstance>();
        Destroy(gameObject);
        return items;
    }
}
