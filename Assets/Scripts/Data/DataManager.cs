using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
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

    static string DataPath => Path.Combine(Application.streamingAssetsPath, "Data");

    public void LoadAll()
    {
        LoadItems();
        LoadSkills();
        LoadMonsters();
        LoadNpcs();
        LoadQuests();
        LoadRegions();
        LoadNpcProfiles();
        Debug.Log($"[DataManager] Loaded: {Items.Count} items, {Skills.Count} skills, " +
                  $"{Monsters.Count} monsters, {Npcs.Count} npcs, {Quests.Count} quests, " +
                  $"{Regions.Count} regions, {NpcProfiles.Count} profiles");
    }

    void LoadItems()
    {
        var data = LoadJson<ItemsData>("items.json");
        if (data == null) return;
        ItemList = data.items ?? System.Array.Empty<ItemDef>();
        foreach (var item in ItemList)
            Items[item.id] = item;
        SetBonuses = data.setBonuses ?? new();
        Recipes = data.recipes ?? System.Array.Empty<RecipeDef>();
    }

    void LoadSkills()
    {
        var data = LoadJson<SkillsData>("skills.json");
        if (data == null) return;
        SkillList = data.skills ?? System.Array.Empty<SkillDef>();
        foreach (var skill in SkillList)
            Skills[skill.id] = skill;
    }

    void LoadMonsters()
    {
        var data = LoadJson<MonstersData>("monsters.json");
        if (data == null) return;
        MonsterList = data.monsters ?? System.Array.Empty<MonsterDef>();
        foreach (var monster in MonsterList)
            Monsters[monster.id] = monster;
    }

    void LoadNpcs()
    {
        var data = LoadJson<NpcsData>("npcs.json");
        if (data == null) return;
        NpcList = data.npcs ?? System.Array.Empty<NpcDef>();
        foreach (var npc in NpcList)
            Npcs[npc.id] = npc;
    }

    void LoadQuests()
    {
        var data = LoadJson<QuestsData>("quests.json");
        if (data == null) return;
        QuestList = data.quests ?? System.Array.Empty<QuestDef>();
        foreach (var quest in QuestList)
            Quests[quest.id] = quest;
    }

    void LoadRegions()
    {
        var data = LoadJson<RegionsData>("regions.json");
        if (data == null) return;
        RegionList = data.regions ?? System.Array.Empty<RegionDef>();
        foreach (var region in RegionList)
            Regions[region.id] = region;
    }

    void LoadNpcProfiles()
    {
        string profileDir = Path.Combine(DataPath, "npc-profiles");
        if (!Directory.Exists(profileDir)) return;
        foreach (string file in Directory.GetFiles(profileDir, "*.md"))
        {
            string npcId = Path.GetFileNameWithoutExtension(file);
            NpcProfiles[npcId] = File.ReadAllText(file);
        }
    }

    static T LoadJson<T>(string filename) where T : class
    {
        string path = Path.Combine(DataPath, filename);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[DataManager] File not found: {path}");
            return null;
        }
        try
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DataManager] Failed to parse {filename}: {e.Message}");
            return null;
        }
    }
}
