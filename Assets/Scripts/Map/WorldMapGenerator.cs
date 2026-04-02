using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldMapGenerator : MonoBehaviour
{
    [SerializeField] Tilemap groundTilemap;
    [SerializeField] Tilemap collisionTilemap;

    int[,] _tileData;
    RegionDef[] _regions;

    public void Generate(RegionDef[] regions) { _regions = regions; Debug.Log("[WorldMap] Generate stub"); }
    public bool IsWalkable(int tileX, int tileY) => true;
    public bool IsVillageTile(int tileX, int tileY) => false;
}
