using System.Collections.Generic;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] GameObject monsterPrefab;
    readonly List<MonsterController> _monsters = new();

    public List<MonsterController> ActiveMonsters => _monsters;
    public void SpawnForRegion(RegionDef region, Dictionary<string, MonsterDef> monsterDefs, WorldMapGenerator worldMap) { }
    public void RemoveMonster(MonsterController m) { _monsters.Remove(m); if (m != null) Destroy(m.gameObject, 0.5f); }
}
