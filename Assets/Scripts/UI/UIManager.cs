using UnityEngine;

public class UIManager : MonoBehaviour
{
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
            else
                pauseMenu.Toggle();
            return;
        }

        if (Input.GetKeyDown(KeyCode.I)) inventory.Toggle();
        if (Input.GetKeyDown(KeyCode.K)) skillTree.Toggle();
        if (Input.GetKeyDown(KeyCode.J)) quest.Toggle();
    }

    public void HideAll()
    {
        inventory.Hide();
        shop.Close();
        crafting.Close();
        enhance.Close();
        skillTree.Hide();
        quest.Hide();
        dialogue.Hide();
        npcProfile.Hide();
        npcQuest.Hide();
        pauseMenu.Close();
        _dialogueOpen = false;
    }

    public void SetDialogueOpen(bool open)
    {
        _dialogueOpen = open;
    }

    bool IsAnyPanelOpen()
    {
        return inventory.gameObject.activeSelf
            || shop.gameObject.activeSelf
            || crafting.gameObject.activeSelf
            || enhance.gameObject.activeSelf
            || skillTree.gameObject.activeSelf
            || quest.gameObject.activeSelf;
    }
}
