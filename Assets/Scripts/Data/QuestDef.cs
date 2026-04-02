using System;

[Serializable] public class QuestDef { public string id, npcId, title, description; public QuestRequirement[] requirements; public QuestKillRequirement[] killRequirements; public QuestReward rewards; }
[Serializable] public class QuestRequirement { public string itemId; public int count; }
[Serializable] public class QuestKillRequirement { public string monsterId; public int count; }
[Serializable] public class QuestReward { public int gold, xp; public QuestRewardItem[] items; }
[Serializable] public class QuestRewardItem { public string itemId; public int count; }
[Serializable] public class QuestsData { public QuestDef[] quests; }
