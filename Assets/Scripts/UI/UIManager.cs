using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public Action OnUseHpPotion;
    public Action OnUseMpPotion;

    [SerializeField] HUD hud;
    [SerializeField] InventoryUI inventory;
    [SerializeField] ShopUI shop;
    [SerializeField] CraftingUI crafting;
    [SerializeField] EnhanceUI enhance;
    [SerializeField] SkillTreeUI skillTree;
    [SerializeField] QuestUI quest;
    [SerializeField] DialogueUI dialogue;
    [SerializeField] NpcProfilePanel npcProfile;
    [SerializeField] NpcQuestPanel npcQuest;
    [SerializeField] PauseMenuUI pauseMenu;

    public HUD Hud => hud;
    public InventoryUI Inventory => inventory;
    public ShopUI Shop => shop;
    public CraftingUI Crafting => crafting;
    public EnhanceUI Enhance => enhance;
    public SkillTreeUI SkillTree => skillTree;
    public QuestUI Quest => quest;
    public DialogueUI Dialogue => dialogue;
    public NpcProfilePanel NpcProfile => npcProfile;
    public NpcQuestPanel NpcQuest => npcQuest;
    public PauseMenuUI PauseMenu => pauseMenu;

    bool _dialogueOpen;

    void Update()
    {
        if (_dialogueOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsAnyPanelOpen())
                HideAll();
            else if (pauseMenu != null)
                pauseMenu.Toggle();
            return;
        }

        if (Input.GetKeyDown(KeyCode.I) && inventory != null) inventory.Toggle();
        if (Input.GetKeyDown(KeyCode.K) && skillTree != null) skillTree.Toggle();
        if (Input.GetKeyDown(KeyCode.J) && quest != null) quest.Toggle();
        if (Input.GetKeyDown(KeyCode.R)) OnUseHpPotion?.Invoke();
        if (Input.GetKeyDown(KeyCode.T)) OnUseMpPotion?.Invoke();
    }

    public void HideAll()
    {
        if (inventory != null) inventory.Hide();
        if (shop != null) shop.Close();
        if (crafting != null) crafting.Close();
        if (enhance != null) enhance.Close();
        if (skillTree != null) skillTree.Hide();
        if (quest != null) quest.Hide();
        if (dialogue != null) dialogue.Hide();
        if (npcProfile != null) npcProfile.Hide();
        if (npcQuest != null) npcQuest.Hide();
        if (pauseMenu != null) pauseMenu.Close();
        _dialogueOpen = false;
    }

    public void SetDialogueOpen(bool open)
    {
        _dialogueOpen = open;
    }

    bool IsAnyPanelOpen()
    {
        return (inventory != null && inventory.IsOpen)
            || (shop != null && shop.IsOpen)
            || (crafting != null && crafting.IsOpen)
            || (enhance != null && enhance.IsOpen)
            || (skillTree != null && skillTree.IsOpen)
            || (quest != null && quest.IsOpen);
    }
}
