using UnityEngine;

public class RegionTracker
{
    readonly RegionDef[] _regions;
    public string CurrentRegionId { get; private set; } = "";

    public RegionTracker(RegionDef[] regions) => _regions = regions;

    public RegionDef GetRegionAt(float worldX, float worldY)
    {
        if (_regions == null || _regions.Length == 0) return null;
        int tileX = Mathf.FloorToInt(worldX / GameConfig.TileSize);
        int tileY = Mathf.FloorToInt(-worldY / GameConfig.TileSize);
        foreach (var r in _regions)
        {
            var b = r.bounds;
            if (tileX >= b.x && tileX < b.x + b.width && tileY >= b.y && tileY < b.y + b.height)
                return r;
        }
        return null;
    }

    public void UpdatePlayerRegion(float worldX, float worldY)
    {
        var region = GetRegionAt(worldX, worldY);
        string newId = region?.id ?? "";
        if (newId == CurrentRegionId) return;
        CurrentRegionId = newId;
        if (region != null)
            EventBus.Emit(new RegionVisitEvent { regionId = region.id, regionName = region.name });
    }
}
