using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WorldMapGenerator : MonoBehaviour
{
    [SerializeField] Tilemap groundTilemap;
    [SerializeField] Tilemap collisionTilemap;

    [Header("Tile Assets")]
    [SerializeField] TileBase grassTile;
    [SerializeField] TileBase dirtTile;
    [SerializeField] TileBase waterTile;
    [SerializeField] TileBase stoneFloorTile;
    [SerializeField] TileBase treeTile;
    [SerializeField] TileBase bushTile;
    [SerializeField] TileBase wallTile;

    RegionDef[] _regions;
    bool[,] _walkable;

    public void Generate(RegionDef[] regions)
    {
        _regions = regions;
        int w = GameConfig.MapWidthTiles;
        int h = GameConfig.MapHeightTiles;
        _walkable = new bool[w, h];

        for (int tx = 0; tx < w; tx++)
        {
            for (int ty = 0; ty < h; ty++)
            {
                _walkable[tx, ty] = true;

                RegionDef region = GetRegionForTile(tx, ty);
                string tileType = PickTileTypeBlended(tx, ty, region);
                TileBase tile = GetTileAsset(tileType);
                Vector3Int cell = TileToCell(tx, ty);

                if (tile != null)
                    groundTilemap.SetTile(cell, tile);

                if (tileType == "wall" || tileType == "tree")
                {
                    _walkable[tx, ty] = false;
                    if (collisionTilemap != null && tile != null)
                        collisionTilemap.SetTile(cell, tile);
                }
            }
        }

        Debug.Log($"[WorldMap] Generated {w}x{h} map with {regions.Length} regions");
    }

    public bool[,] Walkable => _walkable;

    public bool IsWalkable(int tileX, int tileY)
    {
        if (_walkable == null) return true;
        if (tileX < 0 || tileX >= GameConfig.MapWidthTiles ||
            tileY < 0 || tileY >= GameConfig.MapHeightTiles)
            return false;
        return _walkable[tileX, tileY];
    }

    public bool IsVillageTile(int tileX, int tileY)
    {
        if (_regions == null) return false;
        foreach (var r in _regions)
        {
            if (r.id != "village") continue;
            var b = r.bounds;
            if (tileX >= b.x && tileX < b.x + b.width &&
                tileY >= b.y && tileY < b.y + b.height)
                return true;
        }
        return false;
    }

    // Picks the most specific (smallest area) region containing this tile
    RegionDef GetRegionForTile(int tileX, int tileY)
    {
        RegionDef best = null;
        int bestArea = int.MaxValue;

        foreach (var r in _regions)
        {
            var b = r.bounds;
            if (tileX >= b.x && tileX < b.x + b.width &&
                tileY >= b.y && tileY < b.y + b.height)
            {
                int area = b.width * b.height;
                if (area < bestArea)
                {
                    best = r;
                    bestArea = area;
                }
            }
        }
        return best;
    }

    const int BlendRadius = 3;

    string PickTileType(RegionDef region)
    {
        return PickFromWeights(region);
    }

    string PickTileTypeBlended(int tx, int ty, RegionDef primary)
    {
        if (primary == null) return "grass";

        // Check if near a region boundary
        var b = primary.bounds;
        int distToEdge = Mathf.Min(tx - b.x, b.x + b.width - 1 - tx,
                                    ty - b.y, b.y + b.height - 1 - ty);

        if (distToEdge >= BlendRadius) return PickFromWeights(primary);

        // Find neighbor region at closest edge
        RegionDef neighbor = null;
        if (tx - b.x < BlendRadius) neighbor = GetRegionForTile(tx - BlendRadius, ty);
        else if (b.x + b.width - 1 - tx < BlendRadius) neighbor = GetRegionForTile(tx + BlendRadius, ty);
        else if (ty - b.y < BlendRadius) neighbor = GetRegionForTile(tx, ty - BlendRadius);
        else neighbor = GetRegionForTile(tx, ty + BlendRadius);

        if (neighbor == null || neighbor == primary) return PickFromWeights(primary);

        // Blend: closer to edge = more neighbor tiles
        float blendFactor = 1f - (float)distToEdge / BlendRadius;
        return Random.value < blendFactor * 0.6f
            ? PickFromWeights(neighbor)
            : PickFromWeights(primary);
    }

    static string PickFromWeights(RegionDef region)
    {
        if (region == null || region.tileWeights == null) return "grass";

        float roll = Random.value;
        float cumulative = 0f;
        foreach (var kv in region.tileWeights)
        {
            cumulative += kv.Value;
            if (roll <= cumulative) return kv.Key;
        }
        foreach (var kv in region.tileWeights) return kv.Key;
        return "grass";
    }

    TileBase GetTileAsset(string tileType)
    {
        return tileType switch
        {
            "grass" => grassTile,
            "dirt" => dirtTile,
            "water" => waterTile,
            "stone_floor" => stoneFloorTile,
            "tree" => treeTile,
            "bush" => bushTile,
            "wall" => wallTile,
            _ => grassTile
        };
    }

    // Phaser tile coords (Y down) -> Unity Tilemap cell (Y up)
    Vector3Int TileToCell(int tileX, int tileY)
    {
        return new Vector3Int(tileX, -tileY - 1, 0);
    }
}
