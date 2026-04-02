using System;
using System.Collections.Generic;

[Serializable] public class RegionDef { public string id, name; public int difficulty; public RegionBounds bounds; public Dictionary<string, float> tileWeights; public string[] monsterIds; public float monsterDensity; }
[Serializable] public class RegionBounds { public int x, y, width, height; }
[Serializable] public class RegionsData { public RegionDef[] regions; }
