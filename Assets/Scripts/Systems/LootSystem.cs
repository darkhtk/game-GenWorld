using System;
using System.Collections.Generic;

public static class LootSystem
{
    public static List<DropResult> RollDrops(DropEntry[] drops, Func<float> roll = null)
    {
        roll ??= () => UnityEngine.Random.value;
        var results = new List<DropResult>();
        if (drops == null) return results;
        foreach (var d in drops)
        {
            if (roll() < d.chance)
            {
                int count = d.minCount == d.maxCount
                    ? d.minCount
                    : UnityEngine.Random.Range(d.minCount, d.maxCount + 1);
                if (count > 0)
                    results.Add(new DropResult { itemId = d.itemId, count = count });
            }
        }
        return results;
    }
}
