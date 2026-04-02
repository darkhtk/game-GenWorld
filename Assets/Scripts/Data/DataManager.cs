using System.Collections.Generic;
using UnityEngine;

public class DataManager
{
    public Dictionary<string, ItemDef> Items { get; private set; } = new();
    public Dictionary<string, SkillDef> Skills { get; private set; } = new();
    public Dictionary<string, MonsterDef> Monsters { get; private set; } = new();
    public Dictionary<string, NpcDef> Npcs { get; private set; } = new();
    public Dictionary<string, QuestDef> Quests { get; private set; } = new();
    public Dictionary<string, RegionDef> Regions { get; private set; } = new();
    public Dictionary<string, SetBonusDef> SetBonuses { get; private set; } = new();
    public RecipeDef[] Recipes { get; private set; }
    public Dictionary<string, string> NpcProfiles { get; private set; } = new();
    public ItemDef[] ItemList { get; private set; }
    public SkillDef[] SkillList { get; private set; }
    public MonsterDef[] MonsterList { get; private set; }
    public NpcDef[] NpcList { get; private set; }
    public QuestDef[] QuestList { get; private set; }
    public RegionDef[] RegionList { get; private set; }

    public void LoadAll() { Debug.Log("[DataManager] LoadAll stub"); }
}
