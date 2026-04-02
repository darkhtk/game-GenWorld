using System.Collections.Generic;
using System.Text;

public class DialogueContext
{
    public string playerInput;
    public List<DialogueEntry> history;
    public int playerLevel, playerGold;
    public InventorySystem inventory;
    public Dictionary<string, ItemDef> itemDefs;
    public QuestSystem questSystem;
    public string loreContext;
    public string[] npcActions;
    public DialogueTraits traits;
    public int rejectionCount;
}

public static class PromptBuilder
{
    public static readonly string SYSTEM_RULES =
        "너는 RPG 게임의 NPC다. 반드시 한국어로만 대화하라.\n" +
        "응답은 반드시 아래 JSON 형식으로:\n" +
        "{\"dialogue\":\"대사\",\"options\":[\"선택1\",\"선택2\"],\"action\":null," +
        "\"relationshipChange\":1,\"newMemory\":\"요약\",\"offerQuest\":false}\n\n" +
        "규칙:\n" +
        "- dialogue: 한국어 대사 (2~4문장)\n" +
        "- options: 한국어 선택지 2~3개. 각 선택지는 다른 방향 분기. 3턴 이후 작별 선택지 포함.\n" +
        "- 선택지 금지어: 치료, 제작, 강화, 구매, 판매, 포션, 회복\n" +
        "- action: NPC 기능 중 하나 또는 null. 플레이어가 명시적으로 요청할 때만.\n" +
        "- relationshipChange: 최소 1. 도움이면 2, 무례하면 -1.\n" +
        "- newMemory: 이 턴 요약 10자 이내 한국어. null 금지.\n" +
        "- offerQuest: 대화가 자연스럽게 퀘스트로 이어질 때만 true.\n" +
        "- 플레이어가 작별하면: 작별로 응답, options를 빈 배열 [].\n" +
        "- 퀘스트 내용/보상은 AI가 만들지 않는다. offerQuest:true만 설정.\n";

    public static string BuildDialoguePrompt(NPCBrain brain, DialogueContext ctx)
    {
        var sb = new StringBuilder();

        sb.AppendLine("[SYSTEM]");
        sb.AppendLine(SYSTEM_RULES);

        // NPC identity
        sb.AppendLine("[NPC]");
        sb.AppendLine($"이름: {brain.NpcName}");
        sb.AppendLine($"성격: {brain.Personality}");
        sb.AppendLine($"기분: {brain.CurrentMood}");

        int rel = brain.GetRelationship("player");
        sb.AppendLine($"플레이어와 호감도: {rel}");
        sb.AppendLine($"태도: {GetAttitudeGuide(rel)}");

        if (!string.IsNullOrEmpty(brain.Profile))
        {
            sb.AppendLine();
            sb.AppendLine("[NPC 프로필]");
            sb.AppendLine(brain.Profile);
        }

        // Dialogue traits
        if (ctx.traits != null)
        {
            sb.AppendLine();
            sb.AppendLine("[성격 특성]");
            sb.AppendLine($"친절함: {ctx.traits.friendliness}/10");
            sb.AppendLine($"관대함: {ctx.traits.generosity}/10");
            sb.AppendLine($"비밀스러움: {ctx.traits.secretive}/10");
            sb.AppendLine($"고집: {ctx.traits.stubbornness}/10");
            sb.AppendLine($"호기심: {ctx.traits.curiosity}/10");

            int infoLevel = ctx.traits.secretive <= 3 ? 3 : ctx.traits.secretive <= 6 ? 2 : 1;
            sb.AppendLine($"정보 공개 수준: {infoLevel} (3=개방, 1=경계)");
        }

        // Player stats
        sb.AppendLine();
        sb.AppendLine("[STATS]");
        sb.AppendLine($"레벨: {ctx.playerLevel}");
        sb.AppendLine($"골드: {ctx.playerGold}");

        if (ctx.inventory != null && ctx.itemDefs != null)
        {
            var items = new List<string>();
            for (int i = 0; i < ctx.inventory.MaxSlots; i++)
            {
                var slot = ctx.inventory.GetSlot(i);
                if (slot != null && ctx.itemDefs.TryGetValue(slot.itemId, out var def))
                    items.Add($"{def.name}x{slot.count}");
            }
            if (items.Count > 0)
                sb.AppendLine($"인벤토리: {string.Join(", ", items)}");
        }

        sb.AppendLine($"퀘스트 거절 횟수: {ctx.rejectionCount}");

        // Memories
        var memories = brain.GetTopMemories(10);
        if (memories.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[Memories]");
            foreach (var mem in memories)
                sb.AppendLine($"- {mem.eventText}");
        }

        // Active quest context
        if (ctx.questSystem != null)
        {
            var questInfo = ctx.questSystem.GetQuestStatusForNpc(brain.NpcId, ctx.inventory);
            if (questInfo.HasValue)
            {
                sb.AppendLine();
                sb.AppendLine("[퀘스트 상태]");
                sb.AppendLine($"퀘스트: {questInfo.Value.quest.title}");
                sb.AppendLine($"상태: {questInfo.Value.status}");
            }
        }

        // Lore context
        if (!string.IsNullOrEmpty(ctx.loreContext))
        {
            sb.AppendLine();
            sb.AppendLine("[월드 정보]");
            sb.AppendLine(ctx.loreContext);
        }

        // Available actions
        if (ctx.npcActions != null && ctx.npcActions.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[사용 가능한 action]");
            foreach (var action in ctx.npcActions)
                sb.AppendLine($"- {action}");
            sb.AppendLine("플레이어가 명시적으로 요청할 때만 action을 설정하라.");
        }

        // Conversation history
        int turnNumber = 0;
        if (ctx.history != null && ctx.history.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[대화 기록]");
            foreach (var entry in ctx.history)
            {
                string role = entry.role == "player" ? "플레이어" : brain.NpcName;
                sb.AppendLine($"{role}: {entry.text}");
                if (entry.role == "npc") turnNumber++;
            }
        }

        // Turn guidance
        sb.AppendLine();
        if (turnNumber == 0)
            sb.AppendLine("[지시] 첫 턴: 인사 또는 용건 파악.");
        else if (turnNumber == 1)
            sb.AppendLine("[지시] 둘째 턴: 정보 제공 또는 질문 답변.");
        else
            sb.AppendLine("[지시] 3턴 이상: 퀘스트 제안, 마지막 조언, 또는 작별. 반드시 작별 선택지 포함.");

        // Current player input
        sb.AppendLine();
        sb.AppendLine($"플레이어: {ctx.playerInput}");
        sb.AppendLine();
        sb.AppendLine("JSON으로 응답:");

        return sb.ToString();
    }

    static string GetAttitudeGuide(int relationship)
    {
        if (relationship >= 10) return "친근. 반말 섞어도 됨. 공유한 기억 언급.";
        if (relationship >= 5) return "호의적. 적극적으로 도움.";
        if (relationship >= 0) return "중립. 예의 바르게.";
        if (relationship >= -5) return "경계. 짧게 답변.";
        return "적대. 도움 거부.";
    }
}
