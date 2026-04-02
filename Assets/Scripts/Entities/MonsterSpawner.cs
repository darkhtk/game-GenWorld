using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] GameObject monsterPrefab;

    readonly List<MonsterController> _monsters = new();

    public List<MonsterController> ActiveMonsters => _monsters;

    public void SpawnForRegion(RegionDef region, Dictionary<string, MonsterDef> monsterDefs,
        WorldMapGenerator worldMap)
    {
        if (region == null || monsterDefs == null || worldMap == null) return;
        if (region.monsterIds == null || region.monsterIds.Length == 0) return;

        int area = region.bounds.width * region.bounds.height;
        int count = Mathf.RoundToInt(area / 100f * region.monsterDensity);

        for (int i = 0; i < count; i++)
        {
            string monsterId = region.monsterIds[Random.Range(0, region.monsterIds.Length)];
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

    public void RemoveMonster(MonsterController m)
    {
        _monsters.Remove(m);
        if (m != null) Destroy(m.gameObject, 0.5f);
    }
}
