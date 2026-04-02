using System;
using UnityEngine;

public class NpcQuestPanel : MonoBehaviour
{
    public Action<string> OnAcceptQuest;
    public Action<string> OnCompleteQuest;

    public void Show(string npcName, QuestDef quest, string status) { }
    public void Hide() { }
}
