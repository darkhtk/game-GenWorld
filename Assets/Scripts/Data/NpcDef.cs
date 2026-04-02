using System;

[Serializable]
public class NpcDef
{
    public string id, name, color, sprite, personality;
    public string[] loreDocs, actions, idlePhrases, thinkingPhrases;
    public DialogueTraits dialogueTraits;
    public NpcDialogue dialogue;
    public NpcPatrol patrol;
    public AutoActionDef[] autoActions;
    public NpcTrigger[] triggers;
    public ConditionalDialogue[] conditionalDialogues;
}

[Serializable] public class DialogueTraits { public int friendliness, generosity, secretive, stubbornness, curiosity; }
[Serializable] public class NpcDialogue { [Newtonsoft.Json.JsonProperty("default")] public string defaultText; public string hasQuest, questComplete; }
[Serializable] public class NpcPatrol { public int cx, cy, radius; }
[Serializable] public class AutoActionDef { public string type, condition, message, giftItemId; public float range, cooldown; public int giftCount; }
[Serializable] public class NpcTrigger { public string target, memory, talkReason; [Newtonsoft.Json.JsonProperty("event")] public string eventType; public int threshold, relationship; public bool wantToTalk; }
[Serializable] public class ConditionalDialogue { public string id, condition, greeting; public string[] options; public int priority; }
[Serializable] public class NpcsData { public NpcDef[] npcs; }
