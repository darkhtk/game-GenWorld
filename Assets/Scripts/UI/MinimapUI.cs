using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [SerializeField] RawImage mapImage;
    [SerializeField] Transform iconContainer;
    [SerializeField] RectTransform playerIcon;
    [SerializeField] GameObject monsterIconPrefab;
    [SerializeField] GameObject npcIconPrefab;

    Texture2D _mapTexture;
    float _viewRadius = 30f;
    float _lastUpdateTime;
    const float UpdateInterval = 0.2f;
    const int MapPixelSize = 128;

    readonly List<RectTransform> _monsterIcons = new();
    readonly List<RectTransform> _npcIcons = new();

    int _mapWidth, _mapHeight;

    public void Init(bool[,] walkability, int width, int height)
    {
        _mapWidth = width;
        _mapHeight = height;
        _mapTexture = GenerateMapTexture(walkability, width, height);
        if (mapImage != null) mapImage.texture = _mapTexture;
    }

    static Texture2D GenerateMapTexture(bool[,] walkability, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.filterMode = FilterMode.Point;
        var walkColor = new Color(0.15f, 0.3f, 0.15f);
        var blockColor = new Color(0.2f, 0.15f, 0.1f);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, walkability[x, y] ? walkColor : blockColor);

        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (Time.time - _lastUpdateTime < UpdateInterval) return;
        _lastUpdateTime = Time.time;

        if (Input.GetKeyDown(KeyCode.M))
            _viewRadius = _viewRadius > 45f ? 30f : 60f;

        UpdateMapView();
        UpdateEntityIcons();
    }

    void UpdateMapView()
    {
        if (mapImage == null || _mapTexture == null) return;
        var gm = GameManager.Instance;
        if (gm == null) return;

        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        float tileX = player.Position.x / GameConfig.TileSize;
        float tileY = -player.Position.y / GameConfig.TileSize;

        float uvW = _viewRadius * 2f / _mapWidth;
        float uvH = _viewRadius * 2f / _mapHeight;
        float uvX = tileX / _mapWidth - uvW * 0.5f;
        float uvY = tileY / _mapHeight - uvH * 0.5f;

        mapImage.uvRect = new Rect(uvX, uvY, uvW, uvH);

        if (playerIcon != null)
            playerIcon.anchoredPosition = Vector2.zero;
    }

    void UpdateEntityIcons()
    {
        var gm = GameManager.Instance;
        if (gm == null || iconContainer == null) return;

        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        Vector2 playerPos = player.Position;

        var monsters = gm.GetComponentInChildren<MonsterSpawner>()?.ActiveMonsters;
        int mCount = 0;
        if (monsters != null)
        {
            foreach (var m in monsters)
            {
                if (m == null || m.IsDead) continue;
                float dist = Vector2.Distance(playerPos, m.Position);
                if (dist > _viewRadius * GameConfig.TileSize) continue;

                var icon = GetMonsterIcon(mCount);
                icon.gameObject.SetActive(true);
                icon.anchoredPosition = WorldToMinimap(playerPos, m.Position);
                mCount++;
            }
        }
        for (int i = mCount; i < _monsterIcons.Count; i++)
            _monsterIcons[i].gameObject.SetActive(false);

        var npcs = FindObjectsByType<VillageNPC>(FindObjectsSortMode.None);
        int nCount = 0;
        foreach (var npc in npcs)
        {
            float dist = Vector2.Distance(playerPos, npc.Position);
            if (dist > _viewRadius * GameConfig.TileSize) continue;

            var icon = GetNpcIcon(nCount);
            icon.gameObject.SetActive(true);
            icon.anchoredPosition = WorldToMinimap(playerPos, npc.Position);
            nCount++;
        }
        for (int i = nCount; i < _npcIcons.Count; i++)
            _npcIcons[i].gameObject.SetActive(false);
    }

    Vector2 WorldToMinimap(Vector2 playerPos, Vector2 worldPos)
    {
        float dx = (worldPos.x - playerPos.x) / (_viewRadius * GameConfig.TileSize * 2f);
        float dy = (worldPos.y - playerPos.y) / (_viewRadius * GameConfig.TileSize * 2f);
        return new Vector2(dx * MapPixelSize, dy * MapPixelSize);
    }

    RectTransform GetMonsterIcon(int index)
    {
        while (_monsterIcons.Count <= index)
        {
            var go = monsterIconPrefab != null
                ? Instantiate(monsterIconPrefab, iconContainer)
                : CreateDefaultIcon(iconContainer, new Color(1f, 0.3f, 0.3f));
            _monsterIcons.Add(go.GetComponent<RectTransform>());
        }
        return _monsterIcons[index];
    }

    RectTransform GetNpcIcon(int index)
    {
        while (_npcIcons.Count <= index)
        {
            var go = npcIconPrefab != null
                ? Instantiate(npcIconPrefab, iconContainer)
                : CreateDefaultIcon(iconContainer, new Color(0.3f, 1f, 0.3f));
            _npcIcons.Add(go.GetComponent<RectTransform>());
        }
        return _npcIcons[index];
    }

    static GameObject CreateDefaultIcon(Transform parent, Color color)
    {
        var go = new GameObject("MinimapIcon");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(4, 4);
        return go;
    }
}
