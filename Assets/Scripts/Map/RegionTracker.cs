using UnityEngine;

public class RegionTracker
{
    readonly RegionDef[] _regions;
    public string CurrentRegionId { get; private set; } = "";

    public RegionTracker(RegionDef[] regions) => _regions = regions;
    public RegionDef GetRegionAt(float worldX, float worldY) => null;
    public void UpdatePlayerRegion(float worldX, float worldY) { }
}
