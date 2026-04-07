using System;
using System.Collections.Generic;

public static class LootSystem
{
    public static List<DropResult> RollDrops(DropEntry[] drops, Func<float> roll = null, float dropMultiplier = 1f)
    {
        roll ??= () => UnityEngine.Random.value;
        var results = new List<DropResult>();
        if (drops == null) return results;
        foreach (var d in drops)
        {
            float chance = UnityEngine.Mathf.Min(1f, d.chance * dropMultiplier);
            if (roll() < chance)
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
