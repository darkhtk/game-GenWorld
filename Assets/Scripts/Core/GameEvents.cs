public struct MonsterKillEvent { public string monsterId, monsterName; public int killCount, totalKills; }
public struct PlayerDeathEvent { public float deathX, deathY; }
public struct LevelUpEvent { public int level, prevLevel; }
public struct QuestCompleteEvent { public string questTitle; public int completedCount; }
public struct QuestAcceptEvent { public string questId, npcId; }
public struct QuestAbandonEvent { public string questId; }
public struct RegionVisitEvent { public string regionId, regionName; }
public struct ItemCollectEvent { public string itemId; public int count, totalCollected; }
public struct GoldChangeEvent { public int gold; }
public struct EquipChangeEvent { public string slot, itemId; }
public struct CombatPowerChangeEvent { public int power; }
public struct DialogueStartEvent { public string npcId; }
public struct DialogueEndEvent { public string npcId; public int turns; }
public struct NpcTriggerEvent { public string npcId, eventType, target; }
public struct SaveEvent { }
public struct MonsterDespawnEvent { public string monsterId; }
public struct ComboEvent { public string name; }
