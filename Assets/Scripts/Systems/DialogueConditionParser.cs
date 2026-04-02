using System;
using UnityEngine;

public static class DialogueConditionParser
{
    public static bool Evaluate(string condition, QuestSystem quests,
        InventorySystem inventory, NPCBrain brain)
    {
        if (string.IsNullOrEmpty(condition)) return false;

        try
        {
            if (condition.StartsWith("quest_active:"))
            {
                string questId = condition.Substring("quest_active:".Length);
                return quests != null && quests.IsActive(questId);
            }

            if (condition.StartsWith("quest_done:"))
            {
                string questId = condition.Substring("quest_done:".Length);
                return quests != null && quests.IsCompleted(questId);
            }

            if (condition.StartsWith("has_item:"))
            {
                string[] parts = condition.Substring("has_item:".Length).Split(':');
                if (parts.Length < 2) return false;
                string itemId = parts[0];
                if (!int.TryParse(parts[1], out int count)) return false;
                return inventory != null && inventory.GetCount(itemId) >= count;
            }

            if (condition.StartsWith("relationship>="))
            {
                string valStr = condition.Substring("relationship>=".Length);
                if (!int.TryParse(valStr, out int threshold)) return false;
                return brain != null && brain.GetRelationship("player") >= threshold;
            }

            if (condition.StartsWith("relationship<="))
            {
                string valStr = condition.Substring("relationship<=".Length);
                if (!int.TryParse(valStr, out int threshold)) return false;
                return brain != null && brain.GetRelationship("player") <= threshold;
            }

            Debug.LogWarning($"[DialogueConditionParser] Unknown condition: {condition}");
            return false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DialogueConditionParser] Error evaluating '{condition}': {e.Message}");
            return false;
        }
    }

    public static ConditionalDialogue FindBestMatch(ConditionalDialogue[] dialogues,
        QuestSystem quests, InventorySystem inventory, NPCBrain brain)
    {
        if (dialogues == null || dialogues.Length == 0) return null;

        ConditionalDialogue best = null;
        int bestPriority = int.MinValue;

        foreach (var d in dialogues)
        {
            if (d == null) continue;
            if (Evaluate(d.condition, quests, inventory, brain) && d.priority > bestPriority)
            {
                best = d;
                bestPriority = d.priority;
            }
        }

        return best;
    }
}
