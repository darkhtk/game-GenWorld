using System;
using Newtonsoft.Json;

[Serializable]
public class ItemDef
{
    public string id;
    public string name;
    public string type;
    public string grade;
    public string description;
    public string icon;
    public bool stackable;
    public int maxStack = 1;
    public ItemStats stats = new();
    public string setId;
    public int healHp;
    public int healMp;
    public int shopPrice;
    public int sellPrice; // explicit sell-to-vendor price; if 0, falls back to shopPrice/2

    [JsonIgnore] public ItemType TypeEnum => ItemTypeUtil.Parse(type);
    [JsonIgnore] public ItemGrade GradeEnum => ItemGradeUtil.Parse(grade);
}

[Serializable]
public class ItemStats
{
    public int atk, def, maxHp, maxMp, spd, crit;
    public Stats ToStats() => new Stats { atk = atk, def = def, maxHp = maxHp, maxMp = maxMp, spd = spd, crit = crit };
}

[Serializable]
public class SetBonusDef
{
    public string name;
    [JsonProperty("2pc")] public ItemStats pc2;
    [JsonProperty("3pc")] public ItemStats pc3;
    [JsonProperty("4pc")] public ItemStats pc4;
    [JsonProperty("5pc")] public ItemStats pc5;
}

[Serializable]
public class RecipeDef
{
    public string resultId;
    public RecipeMaterial[] materials;
}

[Serializable]
public class RecipeMaterial
{
    public string itemId;
    public int count;
}

[Serializable]
public class ItemsData
{
    public ItemDef[] items;
    public System.Collections.Generic.Dictionary<string, SetBonusDef> setBonuses;
    public RecipeDef[] recipes;
}
