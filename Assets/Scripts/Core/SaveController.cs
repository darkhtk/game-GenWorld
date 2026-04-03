using System.Collections.Generic;
using UnityEngine;

public class SaveController
{
    public void Save(PlayerController player, PlayerStats state, InventorySystem inventory,
        SkillSystem skills, AIManager ai, QuestSystem quests,
        Dictionary<string, int> killCounts, int totalKills)
    {
        var (active, completed, killProgress) = quests.Serialize();
        SaveSystem.Save(new SaveData
        {
            playerX = player.Position.x, playerY = player.Position.y,
            level = state.Level, xp = state.Xp,
            gold = state.Gold, skillPoints = state.SkillPoints,
            statPoints = state.StatPoints,
            hp = state.Hp, mp = state.Mp,
            inventory = inventory.Slots,
            equipment = state.Equipment,
            learnedSkills = skills.GetLearnedSkills(),
            equippedSkills = skills.GetEquippedSkills(),
            npcBrains = ai.SerializeAllBrains(),
            questState = new QuestSaveData { active = active, completed = completed, killProgress = killProgress },
            killCounts = killCounts, totalKills = totalKills,
            bonusStats = state.BonusStats,
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    public void Load(PlayerController player, PlayerStats state, InventorySystem inventory,
        SkillSystem skills, AIManager ai, QuestSystem quests,
        Dictionary<string, int> killCounts, System.Action<int> setTotalKills, DataManager data)
    {
        var save = SaveSystem.Load();
        if (save == null) return;

        player.transform.position = new Vector3(save.playerX, save.playerY, 0);
        state.Level = save.level;
        state.Xp = save.xp;
        state.Gold = save.gold;
        state.SkillPoints = save.skillPoints;
        state.StatPoints = save.statPoints;
        state.Hp = save.hp;
        state.Mp = save.mp;
        if (save.bonusStats != null) state.BonusStats = save.bonusStats;

        if (save.inventory != null)
        {
            for (int i = 0; i < save.inventory.Length && i < inventory.MaxSlots; i++)
                inventory.Slots[i] = save.inventory[i];
        }

        if (save.equipment != null) state.Equipment = save.equipment;
        if (save.learnedSkills != null) skills.RestoreLearnedSkills(save.learnedSkills);
        if (save.equippedSkills != null) skills.RestoreEquipped(save.equippedSkills);
        if (save.npcBrains != null) ai.RestoreAllBrains(save.npcBrains);
        if (save.questState != null) quests.Restore((save.questState.active, save.questState.completed, save.questState.killProgress));
        if (save.killCounts != null)
        {
            foreach (var kv in save.killCounts) killCounts[kv.Key] = kv.Value;
        }
        setTotalKills(save.totalKills);

        state.RecalcStats(data.Items, data.SetBonuses);
        player.SetSpeed(state.CurrentStats.spd);

        Debug.Log($"[SaveController] Loaded save - Level {state.Level}, Gold {state.Gold}");
    }
}
