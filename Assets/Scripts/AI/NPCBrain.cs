using System;
using System.Collections.Generic;
using System.Linq;

public class NPCBrain
{
    public string NpcId { get; }
    public string NpcName { get; }
    public string Personality { get; }
    public Mood CurrentMood { get; set; } = Mood.Neutral;
    public bool WantToTalk { get; set; }
    public string TalkReason { get; set; } = "";
    public float WantToTalkTime { get; set; }
    public string AlertType { get; set; } = "";
    public string Profile { get; set; } = "";

    readonly Dictionary<string, int> _relationships = new();
    readonly List<MemoryEntry> _memories = new();

    public NPCBrain(string npcId, string npcName, string personality) { NpcId = npcId; NpcName = npcName; Personality = personality; }
    public int GetRelationship(string targetId) => _relationships.TryGetValue(targetId, out int v) ? v : 0;
    public void UpdateRelationship(string targetId, int change) { _relationships.TryGetValue(targetId, out int c); _relationships[targetId] = Math.Clamp(c + change, -100, 100); }
    public void AddMemory(string eventText, int importance) { _memories.Add(new MemoryEntry { eventText = eventText, timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), importance = importance }); if (_memories.Count > 20) { _memories.Sort((a, b) => b.importance - a.importance); _memories.RemoveRange(20, _memories.Count - 20); } }
    public List<MemoryEntry> GetTopMemories(int count) => _memories.OrderByDescending(m => m.importance).Take(count).ToList();
    public NPCBrainData Serialize() => new() { mood = CurrentMood.ToString().ToLower(), relationships = new Dictionary<string, int>(_relationships), memories = new List<MemoryEntry>(_memories), wantToTalk = WantToTalk, talkReason = TalkReason, alertType = AlertType };
    public void Restore(NPCBrainData data) { if (data == null) return; CurrentMood = Enum.TryParse<Mood>(data.mood, true, out var m) ? m : Mood.Neutral; _relationships.Clear(); if (data.relationships != null) foreach (var k in data.relationships) _relationships[k.Key] = k.Value; _memories.Clear(); if (data.memories != null) _memories.AddRange(data.memories); WantToTalk = data.wantToTalk; TalkReason = data.talkReason ?? ""; AlertType = data.alertType ?? ""; }
}
