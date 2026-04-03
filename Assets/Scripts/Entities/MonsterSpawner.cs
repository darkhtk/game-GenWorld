using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] GameObject monsterPrefab;

    readonly List<MonsterController> _monsters = new();
    readonly List<string> _nightPoolBuffer = new();

    const float DespawnDistance = 50f * GameConfig.TileSize;
    const float DespawnCheckInterval = 2f;
    const float SpawnGracePeriod = 5f;
    const float CombatGracePeriod = 3f;

    Transform _playerTransform;

    public List<MonsterController> ActiveMonsters => _monsters;

    public void ClearAllMonsters()
    {
        for (int i = _monsters.Count - 1; i >= 0; i--)
        {
            var m = _monsters[i];
            if (m != null)
            {
                EventBus.Emit(new MonsterDespawnEvent { monsterId = m.Def.id });
                Destroy(m.gameObject);
            }
        }
        _monsters.Clear();
    }

    public void SpawnForRegion(RegionDef region, Dictionary<string, MonsterDef> monsterDefs,
        WorldMapGenerator worldMap)
    {
        ClearAllMonsters();
        if (region == null || monsterDefs == null || worldMap == null) return;
        if (region.monsterIds == null || region.monsterIds.Length == 0) return;

        int area = region.bounds.width * region.bounds.height;
        bool isNight = GameManager.Instance?.TimeSystem?.Period == "night";
        float densityMult = isNight && region.nightDensityMult > 0 ? region.nightDensityMult : 1f;
        int count = Mathf.RoundToInt(area / 100f * region.monsterDensity * densityMult);

        string[] monsterPool = region.monsterIds;
        if (isNight && region.nightMonsterIds != null && region.nightMonsterIds.Length > 0)
        {
            _nightPoolBuffer.Clear();
            _nightPoolBuffer.AddRange(region.monsterIds);
            _nightPoolBuffer.AddRange(region.nightMonsterIds);
        }

        for (int i = 0; i < count; i++)
        {
            string monsterId = _nightPoolBuffer.Count > 0
                ? _nightPoolBuffer[Random.Range(0, _nightPoolBuffer.Count)]
                : monsterPool[Random.Range(0, monsterPool.Length)];
            if (!monsterDefs.TryGetValue(monsterId, out var def)) continue;

            Vector2 pos = Vector2.zero;
            bool found = false;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int tx = region.bounds.x + Random.Range(0, region.bounds.width);
                int ty = region.bounds.y + Random.Range(0, region.bounds.height);
                if (worldMap.IsWalkable(tx, ty) && !worldMap.IsVillageTile(tx, ty))
                {
                    // Y flipped for Unity coordinate system
                    pos = new Vector2((tx + 0.5f) * GameConfig.TileSize,
                                     -(ty + 0.5f) * GameConfig.TileSize);
                    found = true;
                    break;
                }
            }
            if (!found) continue;

            var go = Instantiate(monsterPrefab, pos, Quaternion.identity, transform);
            var mc = go.GetComponent<MonsterController>();
            if (mc == null)
            {
                Debug.LogError("[MonsterSpawner] MonsterController missing on prefab");
                Destroy(go);
                continue;
            }
            mc.Init(def, pos);
            _monsters.Add(mc);
        }
    }

    void Start()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null) _playerTransform = player.transform;
        StartCoroutine(DespawnRoutine());
    }

    IEnumerator DespawnRoutine()
    {
        var wait = new WaitForSeconds(DespawnCheckInterval);
        while (true)
        {
            yield return wait;
            if (_playerTransform == null) continue;

            float now = Time.time;
            Vector2 playerPos = (Vector2)_playerTransform.position;

            for (int i = _monsters.Count - 1; i >= 0; i--)
            {
                var m = _monsters[i];
                if (m == null) { _monsters.RemoveAt(i); continue; }
                if (now - m.SpawnTime < SpawnGracePeriod) continue;
                if (now - m.LastHitByPlayerTime < CombatGracePeriod) continue;

                float distSq = (playerPos - m.Position).sqrMagnitude;
                if (distSq > DespawnDistance * DespawnDistance)
                {
                    EventBus.Emit(new MonsterDespawnEvent { monsterId = m.Def.id });
                    _monsters.RemoveAt(i);
                    Destroy(m.gameObject);
                }
            }
        }
    }

    public void RemoveMonster(MonsterController m)
    {
        _monsters.Remove(m);
        if (m != null) Destroy(m.gameObject, 0.5f);
    }
}
