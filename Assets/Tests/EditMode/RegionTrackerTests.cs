using NUnit.Framework;

public class RegionTrackerTests
{
    RegionDef[] _regions;
    RegionTracker _tracker;

    [SetUp]
    public void Setup()
    {
        EventBus.Clear();

        _regions = new[]
        {
            new RegionDef
            {
                id = "village", name = "Village",
                bounds = new RegionBounds { x = 0, y = 0, width = 10, height = 10 }
            },
            new RegionDef
            {
                id = "forest", name = "Forest",
                bounds = new RegionBounds { x = 10, y = 0, width = 20, height = 20 }
            }
        };
        _tracker = new RegionTracker(_regions);
    }

    [TearDown]
    public void Teardown() => EventBus.Clear();

    [Test]
    public void GetRegionAt_ReturnsCorrectRegion()
    {
        // Tile (5, 5) → world (160, -160) — inside village (0,0,10,10)
        float worldX = 5 * GameConfig.TileSize;
        float worldY = -5 * GameConfig.TileSize;
        var region = _tracker.GetRegionAt(worldX, worldY);
        Assert.IsNotNull(region);
        Assert.AreEqual("village", region.id);
    }

    [Test]
    public void GetRegionAt_ReturnsNullOutsideAllRegions()
    {
        float worldX = 100 * GameConfig.TileSize;
        float worldY = -100 * GameConfig.TileSize;
        var region = _tracker.GetRegionAt(worldX, worldY);
        Assert.IsNull(region);
    }

    [Test]
    public void GetRegionAt_ForestRegion()
    {
        // Tile (15, 10) → inside forest (10,0,20,20)
        float worldX = 15 * GameConfig.TileSize;
        float worldY = -10 * GameConfig.TileSize;
        var region = _tracker.GetRegionAt(worldX, worldY);
        Assert.IsNotNull(region);
        Assert.AreEqual("forest", region.id);
    }

    [Test]
    public void UpdatePlayerRegion_SetsCurrentRegionId()
    {
        float worldX = 5 * GameConfig.TileSize;
        float worldY = -5 * GameConfig.TileSize;
        _tracker.UpdatePlayerRegion(worldX, worldY);
        Assert.AreEqual("village", _tracker.CurrentRegionId);
    }

    [Test]
    public void UpdatePlayerRegion_EmitsEventOnRegionChange()
    {
        RegionVisitEvent? received = null;
        EventBus.On<RegionVisitEvent>(e => received = e);

        float worldX = 5 * GameConfig.TileSize;
        float worldY = -5 * GameConfig.TileSize;
        _tracker.UpdatePlayerRegion(worldX, worldY);

        Assert.IsNotNull(received);
        Assert.AreEqual("village", received.Value.regionId);
        Assert.AreEqual("Village", received.Value.regionName);
    }

    [Test]
    public void UpdatePlayerRegion_DoesNotEmitWhenSameRegion()
    {
        float worldX = 5 * GameConfig.TileSize;
        float worldY = -5 * GameConfig.TileSize;
        _tracker.UpdatePlayerRegion(worldX, worldY);

        int emitCount = 0;
        EventBus.On<RegionVisitEvent>(e => emitCount++);
        _tracker.UpdatePlayerRegion(worldX, worldY);

        Assert.AreEqual(0, emitCount);
    }

    [Test]
    public void UpdatePlayerRegion_EmitsWhenMovingToNewRegion()
    {
        _tracker.UpdatePlayerRegion(5 * GameConfig.TileSize, -5 * GameConfig.TileSize);
        Assert.AreEqual("village", _tracker.CurrentRegionId);

        RegionVisitEvent? received = null;
        EventBus.On<RegionVisitEvent>(e => received = e);

        _tracker.UpdatePlayerRegion(15 * GameConfig.TileSize, -10 * GameConfig.TileSize);
        Assert.AreEqual("forest", _tracker.CurrentRegionId);
        Assert.IsNotNull(received);
        Assert.AreEqual("forest", received.Value.regionId);
    }

    [Test]
    public void UpdatePlayerRegion_ClearsRegionWhenOutsideBounds()
    {
        _tracker.UpdatePlayerRegion(5 * GameConfig.TileSize, -5 * GameConfig.TileSize);
        Assert.AreEqual("village", _tracker.CurrentRegionId);

        _tracker.UpdatePlayerRegion(100 * GameConfig.TileSize, -100 * GameConfig.TileSize);
        Assert.AreEqual("", _tracker.CurrentRegionId);
    }

    [Test]
    public void GetRegionAt_BoundaryExclusive()
    {
        // Tile (10, 0) is the first tile of forest (x=10), not village (x=0,w=10 → max x=9)
        float worldX = 10 * GameConfig.TileSize;
        float worldY = 0;
        var region = _tracker.GetRegionAt(worldX, worldY);
        Assert.IsNotNull(region);
        Assert.AreEqual("forest", region.id);
    }
}
