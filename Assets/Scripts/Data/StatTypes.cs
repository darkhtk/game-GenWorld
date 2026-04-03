using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Stats
{
    public int hp, maxHp, mp, maxMp;
    public int atk, def, spd, crit;

    public static Stats operator +(Stats a, Stats b) => new Stats
    {
        hp = a.hp + b.hp, maxHp = a.maxHp + b.maxHp,
        mp = a.mp + b.mp, maxMp = a.maxMp + b.maxMp,
        atk = a.atk + b.atk, def = a.def + b.def,
        spd = a.spd + b.spd, crit = a.crit + b.crit
    };
}

public enum ItemType { Material, Potion, Weapon, Helmet, Armor, Boots, Accessory }
public enum ItemGrade { Common, Uncommon, Rare, Legendary }
public enum SkillTree { Melee, Ranged, Magic }
public enum EffectType { Stun, Slow, Dot, Knockback }
public enum BuffType { Rage, SpeedUp, DefDown, Stealth, ManaShield, Heal }
public enum Mood { Happy, Neutral, Angry, Scared, Grateful }
public enum MonsterAIState { Patrol, Chase, Attack, Return }

public static class ItemGradeUtil
{
    public static ItemGrade Parse(string s) => s switch
    {
        "uncommon" => ItemGrade.Uncommon, "rare" => ItemGrade.Rare,
        "legendary" => ItemGrade.Legendary, _ => ItemGrade.Common
    };
}

public static class ItemTypeUtil
{
    public static ItemType Parse(string s) => s switch
    {
        "potion" => ItemType.Potion, "weapon" => ItemType.Weapon,
        "helmet" => ItemType.Helmet, "armor" => ItemType.Armor,
        "boots" => ItemType.Boots, "accessory" => ItemType.Accessory,
        _ => ItemType.Material
    };
    public static bool IsEquipment(ItemType t) =>
        t is ItemType.Weapon or ItemType.Helmet or ItemType.Armor or ItemType.Boots or ItemType.Accessory;
    public static string GetEquipSlot(ItemType t) => t switch
    {
        ItemType.Weapon => "weapon", ItemType.Helmet => "helmet",
        ItemType.Armor => "armor", ItemType.Boots => "boots",
        ItemType.Accessory => "accessory", _ => ""
    };
}

[Serializable]
public class ItemInstance
{
    public string itemId;
    public int count = 1;
    public int enhanceLevel;
}

public struct PlayerLevelState
{
    public int level, xp, skillPoints, statPoints;
}

public struct UseSkillResult
{
    public bool success;
    public string reason;
    public SkillDef skill;
    public int skillLevel;
    public int mpCost;
}

public struct LearnResult
{
    public bool learned;
    public int remainingPoints;
}

public struct DropResult
{
    public string itemId;
    public int count;
}

[Serializable]
public class DialogueResponse
{
    public string dialogue;
    public string[] options;
    public string action;
    public int relationshipChange;
    public string newMemory;
    public bool offerQuest;
}

[Serializable]
public class DialogueEntry
{
    public string role;
    public string text;
}

[Serializable]
public class MemoryEntry
{
    public string eventText;
    public long timestamp;
    public int importance;
}

[Serializable]
public class NPCBrainData
{
    public string mood;
    public Dictionary<string, int> relationships;
    public List<MemoryEntry> memories;
    public bool wantToTalk;
    public string talkReason;
    public string alertType;
    public List<string> triggeredEvents;
}

[Serializable]
public class SaveData
{
    public float playerX, playerY;
    public int level, xp, gold, skillPoints, statPoints;
    public int hp, mp;
    public ItemInstance[] inventory;
    public Dictionary<string, ItemInstance> equipment;
    public Dictionary<string, int> learnedSkills;
    public string[] equippedSkills;
    public Dictionary<string, NPCBrainData> npcBrains;
    public QuestSaveData questState;
    public Dictionary<string, int> killCounts;
    public int totalKills;
    public string[] firedTriggers;
    public Dictionary<string, int> bonusStats;
    public long timestamp;

    public List<string> Validate()
    {
        var fixes = new List<string>();

        if (level < 1) { fixes.Add($"level {level} -> 1"); level = 1; }
        if (hp <= 0) { fixes.Add($"hp {hp} -> 50"); hp = 50; }
        if (mp < 0) { fixes.Add($"mp {mp} -> 0"); mp = 0; }
        if (xp < 0) { fixes.Add($"xp {xp} -> 0"); xp = 0; }
        if (gold < 0) { fixes.Add($"gold {gold} -> 0"); gold = 0; }
        if (skillPoints < 0) { fixes.Add($"skillPoints {skillPoints} -> 0"); skillPoints = 0; }
        if (statPoints < 0) { fixes.Add($"statPoints {statPoints} -> 0"); statPoints = 0; }
        if (totalKills < 0) { fixes.Add($"totalKills {totalKills} -> 0"); totalKills = 0; }

        if (inventory == null) { fixes.Add("inventory null -> empty"); inventory = new ItemInstance[0]; }
        if (equipment == null)
        {
            fixes.Add("equipment null -> default slots");
            equipment = new Dictionary<string, ItemInstance>
            {
                ["weapon"] = null, ["helmet"] = null,
                ["armor"] = null, ["boots"] = null, ["accessory"] = null
            };
        }
        if (learnedSkills == null) { fixes.Add("learnedSkills null -> empty"); learnedSkills = new Dictionary<string, int>(); }
        if (equippedSkills == null) { fixes.Add("equippedSkills null -> empty"); equippedSkills = new string[0]; }
        if (killCounts == null) { fixes.Add("killCounts null -> empty"); killCounts = new Dictionary<string, int>(); }
        if (bonusStats == null)
        {
            fixes.Add("bonusStats null -> default");
            bonusStats = new Dictionary<string, int>
            {
                ["str"] = 0, ["dex"] = 0, ["wis"] = 0, ["luc"] = 0
            };
        }

        return fixes;
    }
}

[Serializable]
public class QuestSaveData
{
    public string[] active;
    public string[] completed;
    public Dictionary<string, Dictionary<string, int>> killProgress;
}
