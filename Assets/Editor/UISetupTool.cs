using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Creates all UI child hierarchies, prefabs, and wires SerializeField references.
/// "Setup Everything" creates all 3 scenes with full wiring in one step.
/// </summary>
public static class UISetupTool
{
    const string PrefabPath = "Assets/Prefabs/UI/";
    const string ScenePath = "Assets/Scenes/";

    [MenuItem("GenWorld/Setup Everything")]
    public static void SetupEverything()
    {
        EnsureDir(PrefabPath);

        // Build UI prefabs first (shared across scenes)
        var prefabs = CreatePrefabs();

        // 1. BootScene — recreate with controller wiring
        SceneSetupTool.CreateBootScene();
        Debug.Log("[SetupAll] BootScene done");

        // 2. MainMenuScene — recreate with controller wiring
        SceneSetupTool.CreateMainMenuScene();
        Debug.Log("[SetupAll] MainMenuScene done");

        // 3. GameScene — create base, then tiles + full UI
        SceneSetupTool.CreateGameScene();
        // Tile assets (creates .asset files, can wire immediately since GameScene is open)
        SceneSetupTool.SetupTiles();
        // Full UI hierarchy
        SetupGameSceneUI(prefabs);

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SetupAll] All 3 scenes created, wired, and saved!");
    }

    [MenuItem("GenWorld/Setup UI (current scene)")]
    public static void SetupUI()
    {
        EnsureDir(PrefabPath);
        var prefabs = CreatePrefabs();

        // Detect which scene is open and configure accordingly
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name == "GameScene" || Object.FindFirstObjectByType<UIManager>() != null)
        {
            SetupGameSceneUI(prefabs);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[UISetup] GameScene UI wired. Save the scene!");
        }
        else if (scene.name == "BootScene" || Object.FindFirstObjectByType<BootSceneController>() != null)
        {
            Debug.Log("[UISetup] BootScene — already wired by SceneSetupTool. No extra UI needed.");
        }
        else if (scene.name == "MainMenuScene" || Object.FindFirstObjectByType<MainMenuController>() != null)
        {
            Debug.Log("[UISetup] MainMenuScene — already wired by SceneSetupTool. No extra UI needed.");
        }
        else
        {
            Debug.LogWarning("[UISetup] Unknown scene. Open GameScene, BootScene, or MainMenuScene.");
        }
    }

    static void SetupGameSceneUI(Dictionary<string, GameObject> prefabs)
    {
        var canvas = FindCanvas("UICanvas");
        if (canvas == null)
        {
            Debug.LogError("[UISetup] No UICanvas found in GameScene.");
            return;
        }

        var hud = SetupHUD(canvas.transform, prefabs);
        var inventoryUI = SetupInventoryUI(canvas.transform, prefabs);
        var shopUI = SetupShopUI(canvas.transform, prefabs);
        var craftingUI = SetupCraftingUI(canvas.transform, prefabs);
        var enhanceUI = SetupEnhanceUI(canvas.transform, prefabs);
        var skillTreeUI = SetupSkillTreeUI(canvas.transform, prefabs);
        var questUI = SetupQuestUI(canvas.transform, prefabs);
        var dialogueUI = SetupDialogueUI(canvas.transform, prefabs);
        var npcProfilePanel = SetupNpcProfilePanel(canvas.transform, prefabs);
        var npcQuestPanel = SetupNpcQuestPanel(canvas.transform);
        var pauseMenuUI = SetupPauseMenuUI(canvas.transform);

        var uiMgr = Object.FindFirstObjectByType<UIManager>();
        if (uiMgr != null)
        {
            Wire(uiMgr, "hud", hud);
            Wire(uiMgr, "inventory", inventoryUI);
            Wire(uiMgr, "shop", shopUI);
            Wire(uiMgr, "crafting", craftingUI);
            Wire(uiMgr, "enhance", enhanceUI);
            Wire(uiMgr, "skillTree", skillTreeUI);
            Wire(uiMgr, "quest", questUI);
            Wire(uiMgr, "dialogue", dialogueUI);
            Wire(uiMgr, "npcProfile", npcProfilePanel);
            Wire(uiMgr, "npcQuest", npcQuestPanel);
            Wire(uiMgr, "pauseMenu", pauseMenuUI);
            EditorUtility.SetDirty(uiMgr);
        }
    }

    static Canvas FindCanvas(string name)
    {
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.gameObject.name == name) return c;
        }
        return null;
    }

    // ─── PREFABS ───

    static Dictionary<string, GameObject> CreatePrefabs()
    {
        var prefabs = new Dictionary<string, GameObject>();

        prefabs["slotPrefab"] = CreateSlotPrefab();
        prefabs["shopItemPrefab"] = CreateListEntryPrefab("ShopItem", true, true);
        prefabs["recipePrefab"] = CreateListEntryPrefab("RecipeItem", true, false);
        prefabs["enhanceSlotPrefab"] = CreateListEntryPrefab("EnhanceSlot", true, true);
        prefabs["skillRowPrefab"] = CreateSkillRowPrefab();
        prefabs["questEntryPrefab"] = CreateListEntryPrefab("QuestEntry", false, false);
        prefabs["historyEntryPrefab"] = CreateTextPrefab("HistoryEntry", 14);
        prefabs["logEntryPrefab"] = CreateTextPrefab("LogEntry", 16);
        prefabs["optionButtonPrefab"] = CreateButtonPrefab("OptionButton");
        prefabs["actionButtonPrefab"] = CreateButtonPrefab("ActionButton");
        prefabs["memoryEntryPrefab"] = CreateTextPrefab("MemoryEntry", 14);

        Debug.Log($"[UISetup] Created {prefabs.Count} UI prefabs");
        return prefabs;
    }

    static GameObject CreateSlotPrefab()
    {
        var obj = new GameObject("InventorySlot");
        var rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 64);
        var img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f);
        obj.AddComponent<Button>();

        var borderImg = AddChild<Image>(obj, "Border", Vector2.zero, new Vector2(64, 64));
        borderImg.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        borderImg.raycastTarget = false;

        var icon = AddChild<Image>(obj, "Icon", Vector2.zero, new Vector2(48, 48));
        icon.color = Color.white;
        icon.raycastTarget = false;

        var nameT = AddChild<TextMeshProUGUI>(obj, "NameText", new Vector2(0, -24), new Vector2(64, 16));
        nameT.fontSize = 10; nameT.alignment = TextAlignmentOptions.Bottom;

        var countT = AddChild<TextMeshProUGUI>(obj, "CountText", new Vector2(20, -20), new Vector2(24, 16));
        countT.fontSize = 10; countT.alignment = TextAlignmentOptions.BottomRight;

        var enhanceT = AddChild<TextMeshProUGUI>(obj, "EnhanceText", new Vector2(-20, 20), new Vector2(24, 16));
        enhanceT.fontSize = 10; enhanceT.color = Color.yellow;

        var slotUI = obj.AddComponent<InventorySlotUI>();
        Wire(slotUI, "iconImage", icon);
        Wire(slotUI, "nameText", nameT);
        Wire(slotUI, "countText", countT);
        Wire(slotUI, "enhanceText", enhanceT);
        Wire(slotUI, "borderImage", borderImg);

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + "InventorySlot.prefab");
        Object.DestroyImmediate(obj);
        return prefab;
    }

    static GameObject CreateSkillRowPrefab()
    {
        var obj = new GameObject("SkillRow");
        var rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 60);
        var bg = obj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);

        var nameT = AddChild<TextMeshProUGUI>(obj, "NameText", new Vector2(-60, 15), new Vector2(160, 20));
        nameT.fontSize = 14;
        var levelT = AddChild<TextMeshProUGUI>(obj, "LevelText", new Vector2(80, 15), new Vector2(60, 20));
        levelT.fontSize = 12;
        var costT = AddChild<TextMeshProUGUI>(obj, "CostText", new Vector2(80, -5), new Vector2(60, 16));
        costT.fontSize = 10;
        var descT = AddChild<TextMeshProUGUI>(obj, "DescText", new Vector2(-60, -10), new Vector2(160, 16));
        descT.fontSize = 10; descT.color = new Color(0.7f, 0.7f, 0.7f);

        var learnBtn = CreateChildButton(obj, "LearnButton", "Learn", new Vector2(100, -20), new Vector2(60, 24));
        var selectBtn = CreateChildButton(obj, "SelectButton", "Equip", new Vector2(100, 10), new Vector2(60, 24));

        var row = obj.AddComponent<SkillRowUI>();
        Wire(row, "nameText", nameT);
        Wire(row, "levelText", levelT);
        Wire(row, "costText", costT);
        Wire(row, "descText", descT);
        Wire(row, "learnButton", learnBtn);
        Wire(row, "selectButton", selectBtn);
        Wire(row, "backgroundImage", bg);

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + "SkillRow.prefab");
        Object.DestroyImmediate(obj);
        return prefab;
    }

    static GameObject CreateListEntryPrefab(string name, bool hasButton, bool hasPrice)
    {
        var obj = new GameObject(name);
        var rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 40);
        obj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

        AddChild<TextMeshProUGUI>(obj, "NameText", new Vector2(-60, 0), new Vector2(180, 30));
        if (hasPrice)
            AddChild<TextMeshProUGUI>(obj, "PriceText", new Vector2(80, 0), new Vector2(60, 30));
        if (hasButton)
            CreateChildButton(obj, "ActionButton", "Buy", new Vector2(120, 0), new Vector2(50, 28));

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + name + ".prefab");
        Object.DestroyImmediate(obj);
        return prefab;
    }

    static GameObject CreateTextPrefab(string name, int fontSize)
    {
        var obj = new GameObject(name);
        var rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 20);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = Color.white;

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + name + ".prefab");
        Object.DestroyImmediate(obj);
        return prefab;
    }

    static GameObject CreateButtonPrefab(string name)
    {
        var obj = new GameObject(name);
        var rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(280, 36);
        obj.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.4f);
        obj.AddComponent<Button>();
        var t = AddChild<TextMeshProUGUI>(obj, "Label", Vector2.zero, new Vector2(260, 30));
        t.fontSize = 14; t.alignment = TextAlignmentOptions.Center;

        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + name + ".prefab");
        Object.DestroyImmediate(obj);
        return prefab;
    }

    // ─── HUD ───

    static HUD SetupHUD(Transform parent, Dictionary<string, GameObject> prefabs)
    {
        var existing = parent.Find("HUD");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreateRTChild(parent, "HUD", Vector2.zero, Vector2.zero);
        Stretch(root);
        var hud = root.AddComponent<HUD>();

        // HP/MP/XP Bars — anchored bottom-left
        var hpBar = CreateBar(root.transform, "HPBar", new Vector2(120, 80), Color.red, "HP");
        AnchorBottomLeft(hpBar.root);
        var mpBar = CreateBar(root.transform, "MPBar", new Vector2(120, 50), Color.blue, "MP");
        AnchorBottomLeft(mpBar.root);
        var xpBar = CreateBar(root.transform, "XPBar", new Vector2(120, 20), Color.green, "XP");
        AnchorBottomLeft(xpBar.root);
        // Dodge bar — anchored bottom-right
        var dodgeBar = CreateBar(root.transform, "DodgeBar", new Vector2(-120, 80), Color.cyan, "");
        AnchorBottomRight(dodgeBar.root);

        Wire(hud, "hpFill", hpBar.fill);
        Wire(hud, "hpText", hpBar.text);
        Wire(hud, "mpFill", mpBar.fill);
        Wire(hud, "mpText", mpBar.text);
        Wire(hud, "xpFill", xpBar.fill);
        Wire(hud, "dodgeFill", dodgeBar.fill);

        var levelT = AddChild<TextMeshProUGUI>(root, "LevelText", new Vector2(10, 80), new Vector2(60, 24));
        levelT.fontSize = 16; levelT.text = "Lv.1";
        AnchorBottomLeft(levelT.gameObject);
        Wire(hud, "levelText", levelT);

        // Skill slots (6) — anchored bottom-center
        var skillArea = CreateRTChild(root.transform, "SkillBar", new Vector2(0, 30), new Vector2(360, 50));
        AnchorBottomCenter(skillArea);
        var icons = new List<Object>(); var overlays = new List<Object>();
        var keyLabels = new List<Object>(); var buffTexts = new List<Object>();
        for (int i = 0; i < 6; i++)
        {
            var slot = CreateRTChild(skillArea.transform, $"Skill_{i + 1}", new Vector2(-150 + i * 60, 0), new Vector2(50, 50));
            slot.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);
            var icon = AddChild<Image>(slot, "Icon", Vector2.zero, new Vector2(40, 40));
            icon.color = Color.clear;
            var overlay = AddChild<Image>(slot, "Cooldown", Vector2.zero, new Vector2(50, 50));
            overlay.color = new Color(0, 0, 0, 0.6f); overlay.fillAmount = 0;
            overlay.type = Image.Type.Filled; overlay.fillMethod = Image.FillMethod.Radial360;
            var key = AddChild<TextMeshProUGUI>(slot, "Key", new Vector2(0, -20), new Vector2(20, 14));
            key.fontSize = 10; key.text = (i + 1).ToString();
            var buff = AddChild<TextMeshProUGUI>(slot, "Buff", new Vector2(0, 20), new Vector2(50, 14));
            buff.fontSize = 10; buff.color = Color.yellow;

            icons.Add(icon); overlays.Add(overlay); keyLabels.Add(key); buffTexts.Add(buff);
        }
        WireArray(hud, "skillIcons", icons.ToArray());
        WireArray(hud, "skillCooldownOverlays", overlays.ToArray());
        WireArray(hud, "skillKeyLabels", keyLabels.ToArray());
        WireArray(hud, "skillBuffTexts", buffTexts.ToArray());

        // Info texts — bottom-left
        var hpPot = AddChild<TextMeshProUGUI>(root, "HpPotionCount", new Vector2(80, 10), new Vector2(40, 20));
        hpPot.fontSize = 12; hpPot.text = "0"; AnchorBottomLeft(hpPot.gameObject);
        var mpPot = AddChild<TextMeshProUGUI>(root, "MpPotionCount", new Vector2(130, 10), new Vector2(40, 20));
        mpPot.fontSize = 12; mpPot.text = "0"; AnchorBottomLeft(mpPot.gameObject);
        var goldT = AddChild<TextMeshProUGUI>(root, "GoldText", new Vector2(10, 10), new Vector2(100, 20));
        goldT.fontSize = 14; goldT.text = "0 G"; AnchorBottomLeft(goldT.gameObject);
        // Region name — top-center
        var regionT = AddChild<TextMeshProUGUI>(root, "RegionText", new Vector2(0, -30), new Vector2(200, 24));
        regionT.fontSize = 16; regionT.alignment = TextAlignmentOptions.Top; AnchorTopCenter(regionT.gameObject);
        var statPtsT = AddChild<TextMeshProUGUI>(root, "StatPointsText", new Vector2(200, 80), new Vector2(80, 20));
        statPtsT.fontSize = 12; AnchorBottomLeft(statPtsT.gameObject);

        Wire(hud, "hpPotionCount", hpPot);
        Wire(hud, "mpPotionCount", mpPot);
        Wire(hud, "goldText", goldT);
        Wire(hud, "regionText", regionT);
        Wire(hud, "statPointsText", statPtsT);

        // Boss bar — top-center
        var bossRoot = CreateRTChild(root.transform, "BossBar", new Vector2(0, -40), new Vector2(400, 30));
        AnchorTopCenter(bossRoot);
        bossRoot.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        bossRoot.SetActive(false);
        var bossFillObj = CreateRTChild(bossRoot.transform, "Fill", new Vector2(0, 0), new Vector2(390, 20));
        var bossFill = bossFillObj.AddComponent<Image>(); bossFill.color = Color.red;
        var bossName = AddChild<TextMeshProUGUI>(bossRoot, "BossName", new Vector2(-100, 0), new Vector2(180, 24));
        bossName.fontSize = 14;
        var bossHp = AddChild<TextMeshProUGUI>(bossRoot, "BossHP", new Vector2(100, 0), new Vector2(100, 24));
        bossHp.fontSize = 12;

        Wire(hud, "bossBarRoot", bossRoot);
        Wire(hud, "bossFill", bossFill);
        Wire(hud, "bossNameText", bossName);
        Wire(hud, "bossHpText", bossHp);

        // Minimap — top-right
        var minimap = CreateRTChild(root.transform, "Minimap", new Vector2(-10, -10), new Vector2(150, 150));
        AnchorTopRight(minimap);
        var minimapImg = minimap.AddComponent<RawImage>();
        minimapImg.color = new Color(0.3f, 0.3f, 0.3f);
        Wire(hud, "minimapImage", minimapImg);

        // History — bottom-right
        var historyRoot = CreateRTChild(root.transform, "History", new Vector2(-10, 100), new Vector2(250, 200));
        AnchorBottomRight(historyRoot);
        historyRoot.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        historyRoot.SetActive(false);
        var histScroll = historyRoot.AddComponent<ScrollRect>();
        var histContent = CreateRTChild(historyRoot.transform, "Content", Vector2.zero, new Vector2(240, 400));
        histContent.AddComponent<VerticalLayoutGroup>();
        histContent.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        histScroll.content = histContent.GetComponent<RectTransform>();
        histScroll.vertical = true; histScroll.horizontal = false;
        var histToggle = CreateChildButton(root, "HistoryToggle", "Log", new Vector2(-10, 10), new Vector2(60, 24));
        AnchorBottomRight(histToggle.gameObject);

        Wire(hud, "historyRoot", historyRoot);
        Wire(hud, "historyContent", histContent.transform);
        Wire(hud, "historyEntryPrefab", GetPrefabComponent<TextMeshProUGUI>(prefabs["historyEntryPrefab"]));
        Wire(hud, "historyToggleButton", histToggle);

        // Save indicator — top-right below minimap
        var saveInd = CreateRTChild(root.transform, "SaveIndicator", new Vector2(-10, -170), new Vector2(80, 24));
        AnchorTopRight(saveInd);
        var saveText = saveInd.AddComponent<TextMeshProUGUI>();
        saveText.text = "Saved"; saveText.fontSize = 14;
        var saveCG = saveInd.AddComponent<CanvasGroup>(); saveCG.alpha = 0;
        Wire(hud, "saveIndicator", saveCG);

        // Skill tooltip
        var ttPanel = CreateRTChild(root.transform, "SkillTooltip", new Vector2(0, -100), new Vector2(250, 100));
        ttPanel.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        ttPanel.SetActive(false);
        var ttName = AddChild<TextMeshProUGUI>(ttPanel, "Name", new Vector2(0, 30), new Vector2(230, 24));
        ttName.fontSize = 14;
        var ttDesc = AddChild<TextMeshProUGUI>(ttPanel, "Desc", new Vector2(0, 5), new Vector2(230, 20));
        ttDesc.fontSize = 11; ttDesc.color = new Color(0.8f, 0.8f, 0.8f);
        var ttStats = AddChild<TextMeshProUGUI>(ttPanel, "Stats", new Vector2(0, -20), new Vector2(230, 20));
        ttStats.fontSize = 11; ttStats.color = Color.yellow;

        Wire(hud, "skillTooltipPanel", ttPanel);
        Wire(hud, "skillTooltipName", ttName);
        Wire(hud, "skillTooltipDesc", ttDesc);
        Wire(hud, "skillTooltipStats", ttStats);

        EditorUtility.SetDirty(hud);
        return hud;
    }

    // ─── INVENTORY ───

    static InventoryUI SetupInventoryUI(Transform parent, Dictionary<string, GameObject> prefabs)
    {
        var existing = parent.Find("InventoryPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreatePanel(parent, "InventoryPanel", new Vector2(500, 500));
        var ui = root.AddComponent<InventoryUI>();

        var closeBtn = CreateChildButton(root, "CloseButton", "X", new Vector2(230, 230), new Vector2(40, 40));
        var sortBtn = CreateChildButton(root, "SortButton", "Sort", new Vector2(170, 230), new Vector2(60, 30));

        var gridObj = CreateRTChild(root.transform, "Grid", new Vector2(-80, 30), new Vector2(260, 260));
        var grid = gridObj.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(64, 64); grid.spacing = new Vector2(4, 4);

        // Equipment slots
        string[] equipNames = { "Weapon", "Helmet", "Armor", "Boots", "Accessory" };
        var equipSlots = new EquipSlotUI[5];
        for (int i = 0; i < 5; i++)
        {
            var eslot = CreateRTChild(root.transform, equipNames[i] + "Slot", new Vector2(190, 100 - i * 45), new Vector2(90, 40));
            eslot.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);
            var eNameT = AddChild<TextMeshProUGUI>(eslot, "Name", new Vector2(-10, 5), new Vector2(60, 16));
            eNameT.fontSize = 10; eNameT.text = equipNames[i];
            var eEnhT = AddChild<TextMeshProUGUI>(eslot, "Enhance", new Vector2(30, 5), new Vector2(30, 16));
            eEnhT.fontSize = 10; eEnhT.color = Color.yellow;
            var eBorder = eslot.GetComponent<Image>();
            var eUnequip = CreateChildButton(eslot, "Unequip", "-", new Vector2(35, -10), new Vector2(20, 16));
            var eComp = eslot.AddComponent<EquipSlotUI>();
            Wire(eComp, "nameText", eNameT);
            Wire(eComp, "enhanceText", eEnhT);
            Wire(eComp, "borderImage", eBorder);
            Wire(eComp, "unequipButton", eUnequip);
            equipSlots[i] = eComp;
        }

        // Stats texts
        string[] statNames = { "atk", "def", "spd", "crit", "hpStat", "mpStat" };
        string[] statLabels = { "ATK", "DEF", "SPD", "CRI", "HP", "MP" };
        var statTexts = new TextMeshProUGUI[6];
        for (int i = 0; i < 6; i++)
        {
            var st = AddChild<TextMeshProUGUI>(root, statLabels[i] + "Text", new Vector2(-200, -120 - i * 18), new Vector2(120, 16));
            st.fontSize = 11; st.text = statLabels[i] + ": 0";
            statTexts[i] = st;
        }

        // Bonus stat rows with add buttons
        string[] bonusNames = { "STR", "DEX", "WIS", "LUC" };
        var bonusTexts = new TextMeshProUGUI[4];
        var bonusBtns = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            var bt = AddChild<TextMeshProUGUI>(root, bonusNames[i] + "Text", new Vector2(-80, -120 - i * 18), new Vector2(60, 16));
            bt.fontSize = 11; bt.text = bonusNames[i] + ": 0";
            bonusTexts[i] = bt;
            bonusBtns[i] = CreateChildButton(root, bonusNames[i] + "Add", "+", new Vector2(-30, -120 - i * 18), new Vector2(20, 16));
        }

        var statPtsT = AddChild<TextMeshProUGUI>(root, "StatPointsText", new Vector2(-80, -195), new Vector2(80, 16));
        statPtsT.fontSize = 11;
        var lvlGoldT = AddChild<TextMeshProUGUI>(root, "LevelGoldText", new Vector2(-200, -195), new Vector2(100, 16));
        lvlGoldT.fontSize = 11;

        // Tooltip
        var tooltip = CreateRTChild(root.transform, "Tooltip", new Vector2(0, -220), new Vector2(300, 80));
        tooltip.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        tooltip.SetActive(false);
        var ttN = AddChild<TextMeshProUGUI>(tooltip, "Name", new Vector2(0, 25), new Vector2(280, 20));
        ttN.fontSize = 13;
        var ttD = AddChild<TextMeshProUGUI>(tooltip, "Desc", new Vector2(0, 5), new Vector2(280, 18));
        ttD.fontSize = 10;
        var ttS = AddChild<TextMeshProUGUI>(tooltip, "Stats", new Vector2(0, -15), new Vector2(280, 18));
        ttS.fontSize = 10; ttS.color = Color.yellow;

        // Drag icon
        var dragIcon = AddChild<Image>(root, "DragIcon", Vector2.zero, new Vector2(48, 48));
        dragIcon.raycastTarget = false; dragIcon.color = Color.clear;

        // Wire everything
        Wire(ui, "panel", root);
        Wire(ui, "closeButton", closeBtn);
        Wire(ui, "sortButton", sortBtn);
        Wire(ui, "gridContent", gridObj.transform);
        Wire(ui, "slotPrefab", prefabs["slotPrefab"]);
        Wire(ui, "weaponSlot", equipSlots[0]);
        Wire(ui, "helmetSlot", equipSlots[1]);
        Wire(ui, "armorSlot", equipSlots[2]);
        Wire(ui, "bootsSlot", equipSlots[3]);
        Wire(ui, "accessorySlot", equipSlots[4]);
        Wire(ui, "atkText", statTexts[0]);
        Wire(ui, "defText", statTexts[1]);
        Wire(ui, "spdText", statTexts[2]);
        Wire(ui, "critText", statTexts[3]);
        Wire(ui, "hpStatText", statTexts[4]);
        Wire(ui, "mpStatText", statTexts[5]);
        Wire(ui, "strText", bonusTexts[0]);
        Wire(ui, "dexText", bonusTexts[1]);
        Wire(ui, "wisText", bonusTexts[2]);
        Wire(ui, "lucText", bonusTexts[3]);
        Wire(ui, "strAddButton", bonusBtns[0]);
        Wire(ui, "dexAddButton", bonusBtns[1]);
        Wire(ui, "wisAddButton", bonusBtns[2]);
        Wire(ui, "lucAddButton", bonusBtns[3]);
        Wire(ui, "statPointsText", statPtsT);
        Wire(ui, "levelGoldText", lvlGoldT);
        Wire(ui, "tooltipPanel", tooltip);
        Wire(ui, "tooltipName", ttN);
        Wire(ui, "tooltipDesc", ttD);
        Wire(ui, "tooltipStats", ttS);
        Wire(ui, "dragIcon", dragIcon);

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ─── SHOP ───

    static ShopUI SetupShopUI(Transform parent, Dictionary<string, GameObject> prefabs)
    {
        var existing = parent.Find("ShopPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreatePanel(parent, "ShopPanel", new Vector2(350, 450));
        var ui = root.AddComponent<ShopUI>();

        var closeBtn = CreateChildButton(root, "CloseButton", "X", new Vector2(155, 205), new Vector2(40, 40));
        var titleT = AddChild<TextMeshProUGUI>(root, "TitleText", new Vector2(0, 200), new Vector2(200, 30));
        titleT.fontSize = 18; titleT.text = "Shop"; titleT.alignment = TextAlignmentOptions.Center;
        var goldT = AddChild<TextMeshProUGUI>(root, "GoldText", new Vector2(0, 175), new Vector2(100, 20));
        goldT.fontSize = 12;

        var scrollObj = CreateScrollArea(root.transform, "ScrollArea", new Vector2(0, -20), new Vector2(320, 350));

        Wire(ui, "panel", root);
        Wire(ui, "closeButton", closeBtn);
        Wire(ui, "titleText", titleT);
        Wire(ui, "goldText", goldT);
        Wire(ui, "scrollRect", scrollObj.scroll);
        Wire(ui, "itemListContent", scrollObj.content.transform);
        Wire(ui, "shopItemPrefab", prefabs["shopItemPrefab"]);

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ─── CRAFTING ───

    static CraftingUI SetupCraftingUI(Transform parent, Dictionary<string, GameObject> prefabs)
    {
        var existing = parent.Find("CraftingPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreatePanel(parent, "CraftingPanel", new Vector2(350, 400));
        var ui = root.AddComponent<CraftingUI>();
        var closeBtn = CreateChildButton(root, "CloseButton", "X", new Vector2(155, 180), new Vector2(40, 40));
        var scrollObj = CreateScrollArea(root.transform, "ScrollArea", new Vector2(0, -20), new Vector2(320, 330));

        Wire(ui, "panel", root);
        Wire(ui, "closeButton", closeBtn);
        Wire(ui, "scrollRect", scrollObj.scroll);
        Wire(ui, "recipeListContent", scrollObj.content.transform);
        Wire(ui, "recipePrefab", prefabs["recipePrefab"]);

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ─── ENHANCE ───

    static EnhanceUI SetupEnhanceUI(Transform parent, Dictionary<string, GameObject> prefabs)
    {
        var existing = parent.Find("EnhancePanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreatePanel(parent, "EnhancePanel", new Vector2(350, 400));
        var ui = root.AddComponent<EnhanceUI>();
        var closeBtn = CreateChildButton(root, "CloseButton", "X", new Vector2(155, 180), new Vector2(40, 40));
        var goldT = AddChild<TextMeshProUGUI>(root, "GoldText", new Vector2(0, 170), new Vector2(100, 20));
        goldT.fontSize = 12;
        var scrollObj = CreateScrollArea(root.transform, "ScrollArea", new Vector2(0, -20), new Vector2(320, 330));

        Wire(ui, "panel", root);
        Wire(ui, "closeButton", closeBtn);
        Wire(ui, "goldText", goldT);
        Wire(ui, "slotListContent", scrollObj.content.transform);
        Wire(ui, "enhanceSlotPrefab", prefabs["enhanceSlotPrefab"]);

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ─── SKILL TREE ───

    static SkillTreeUI SetupSkillTreeUI(Transform parent, Dictionary<string, GameObject> prefabs)
    {
        var existing = parent.Find("SkillTreePanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreatePanel(parent, "SkillTreePanel", new Vector2(700, 500));
        var ui = root.AddComponent<SkillTreeUI>();
        var closeBtn = CreateChildButton(root, "CloseButton", "X", new Vector2(330, 230), new Vector2(40, 40));
        var spText = AddChild<TextMeshProUGUI>(root, "SkillPointsText", new Vector2(-250, 225), new Vector2(120, 24));
        spText.fontSize = 14;
        var lvlText = AddChild<TextMeshProUGUI>(root, "PlayerLevelText", new Vector2(-120, 225), new Vector2(80, 24));
        lvlText.fontSize = 14;
        var resetBtn = CreateChildButton(root, "ResetButton", "Reset", new Vector2(250, 225), new Vector2(70, 30));

        // 3 columns
        var melee = CreateRTChild(root.transform, "MeleeColumn", new Vector2(-220, -20), new Vector2(200, 400));
        melee.AddComponent<VerticalLayoutGroup>().spacing = 4;
        melee.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var ranged = CreateRTChild(root.transform, "RangedColumn", new Vector2(0, -20), new Vector2(200, 400));
        ranged.AddComponent<VerticalLayoutGroup>().spacing = 4;
        ranged.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var magic = CreateRTChild(root.transform, "MagicColumn", new Vector2(220, -20), new Vector2(200, 400));
        magic.AddComponent<VerticalLayoutGroup>().spacing = 4;
        magic.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Equip slot buttons (6)
        var eqBtns = new List<Object>();
        var eqLabels = new List<Object>();
        for (int i = 0; i < 6; i++)
        {
            var btn = CreateChildButton(root, $"EquipSlot_{i + 1}", (i + 1).ToString(),
                new Vector2(-150 + i * 60, -230), new Vector2(50, 30));
            var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            eqBtns.Add(btn);
            eqLabels.Add(lbl);
        }

        Wire(ui, "panel", root);
        Wire(ui, "closeButton", closeBtn);
        Wire(ui, "skillPointsText", spText);
        Wire(ui, "playerLevelText", lvlText);
        Wire(ui, "resetButton", resetBtn);
        Wire(ui, "meleeColumn", melee.transform);
        Wire(ui, "rangedColumn", ranged.transform);
        Wire(ui, "magicColumn", magic.transform);
        Wire(ui, "skillRowPrefab", prefabs["skillRowPrefab"]);
        WireArray(ui, "equipSlotButtons", eqBtns.ToArray());
        WireArray(ui, "equipSlotLabels", eqLabels.ToArray());

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ─── QUEST ───

    static QuestUI SetupQuestUI(Transform parent, Dictionary<string, GameObject> prefabs)
    {
        var existing = parent.Find("QuestPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreatePanel(parent, "QuestPanel", new Vector2(400, 450));
        var ui = root.AddComponent<QuestUI>();
        var closeBtn = CreateChildButton(root, "CloseButton", "X", new Vector2(180, 205), new Vector2(40, 40));

        var activeTab = CreateChildButton(root, "ActiveTab", "Active", new Vector2(-80, 195), new Vector2(100, 30));
        var completedTab = CreateChildButton(root, "CompletedTab", "Done", new Vector2(40, 195), new Vector2(100, 30));

        var scrollObj = CreateScrollArea(root.transform, "ScrollArea", new Vector2(0, -20), new Vector2(370, 370));

        Wire(ui, "panel", root);
        Wire(ui, "closeButton", closeBtn);
        Wire(ui, "activeTab", activeTab);
        Wire(ui, "completedTab", completedTab);
        Wire(ui, "activeTabBg", activeTab.GetComponent<Image>());
        Wire(ui, "completedTabBg", completedTab.GetComponent<Image>());
        Wire(ui, "questListContent", scrollObj.content.transform);
        Wire(ui, "questEntryPrefab", prefabs["questEntryPrefab"]);

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ─── DIALOGUE ───

    static DialogueUI SetupDialogueUI(Transform parent, Dictionary<string, GameObject> prefabs)
    {
        var existing = parent.Find("DialoguePanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreatePanel(parent, "DialoguePanel", new Vector2(500, 500));
        var ui = root.AddComponent<DialogueUI>();
        var closeBtn = CreateChildButton(root, "CloseButton", "X", new Vector2(230, 230), new Vector2(40, 40));
        var npcName = AddChild<TextMeshProUGUI>(root, "NpcNameText", new Vector2(-100, 225), new Vector2(200, 24));
        npcName.fontSize = 16;
        var questTitle = AddChild<TextMeshProUGUI>(root, "QuestTitleText", new Vector2(100, 225), new Vector2(150, 24));
        questTitle.fontSize = 12; questTitle.color = Color.yellow;

        // Chat log
        var logScroll = CreateScrollArea(root.transform, "LogArea", new Vector2(0, 40), new Vector2(460, 280));
        // Options
        var optContainer = CreateRTChild(root.transform, "Options", new Vector2(0, -150), new Vector2(460, 80));
        optContainer.AddComponent<VerticalLayoutGroup>().spacing = 4;
        optContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        // Free input
        var inputObj = CreateRTChild(root.transform, "InputField", new Vector2(-40, -210), new Vector2(380, 36));
        inputObj.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);
        var inputField = inputObj.AddComponent<TMP_InputField>();
        var inputText = AddChild<TextMeshProUGUI>(inputObj, "Text", Vector2.zero, new Vector2(370, 30));
        inputText.fontSize = 14;
        var placeholder = AddChild<TextMeshProUGUI>(inputObj, "Placeholder", Vector2.zero, new Vector2(370, 30));
        placeholder.fontSize = 14; placeholder.text = "Type..."; placeholder.color = new Color(0.5f, 0.5f, 0.5f);
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;

        var sendBtn = CreateChildButton(root, "SendButton", ">", new Vector2(200, -210), new Vector2(40, 36));
        var freeToggle = CreateChildButton(root, "FreeToggle", "Free", new Vector2(-200, -180), new Vector2(50, 20));

        // Action buttons
        var actionContainer = CreateRTChild(root.transform, "ActionButtons", new Vector2(0, -180), new Vector2(300, 30));
        actionContainer.AddComponent<HorizontalLayoutGroup>().spacing = 4;

        // Loading
        var loadingObj = CreateRTChild(root.transform, "Loading", Vector2.zero, new Vector2(200, 40));
        loadingObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.7f);
        loadingObj.SetActive(false);
        var loadingT = AddChild<TextMeshProUGUI>(loadingObj, "Text", Vector2.zero, new Vector2(180, 30));
        loadingT.fontSize = 14; loadingT.text = "Thinking..."; loadingT.alignment = TextAlignmentOptions.Center;

        // Quest proposal
        var qpPanel = CreateRTChild(root.transform, "QuestProposal", new Vector2(0, 0), new Vector2(350, 250));
        qpPanel.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        qpPanel.SetActive(false);
        var qpTitle = AddChild<TextMeshProUGUI>(qpPanel, "Title", new Vector2(0, 100), new Vector2(320, 24));
        qpTitle.fontSize = 16;
        var qpDesc = AddChild<TextMeshProUGUI>(qpPanel, "Desc", new Vector2(0, 60), new Vector2(320, 40));
        qpDesc.fontSize = 12;
        var qpReqs = AddChild<TextMeshProUGUI>(qpPanel, "Requirements", new Vector2(0, 20), new Vector2(320, 30));
        qpReqs.fontSize = 11;
        var qpRewards = AddChild<TextMeshProUGUI>(qpPanel, "Rewards", new Vector2(0, -15), new Vector2(320, 30));
        qpRewards.fontSize = 11; qpRewards.color = Color.yellow;
        var qpAccept = CreateChildButton(qpPanel, "AcceptButton", "Accept", new Vector2(-60, -60), new Vector2(90, 30));
        var qpReject = CreateChildButton(qpPanel, "RejectButton", "Reject", new Vector2(60, -60), new Vector2(90, 30));

        Wire(ui, "panel", root);
        Wire(ui, "closeButton", closeBtn);
        Wire(ui, "npcNameText", npcName);
        Wire(ui, "questTitleText", questTitle);
        Wire(ui, "logScrollRect", logScroll.scroll);
        Wire(ui, "logContent", logScroll.content.transform);
        Wire(ui, "logEntryPrefab", GetPrefabComponent<TextMeshProUGUI>(prefabs["logEntryPrefab"]));
        Wire(ui, "optionsContainer", optContainer.transform);
        Wire(ui, "optionButtonPrefab", GetPrefabComponent<Button>(prefabs["optionButtonPrefab"]));
        Wire(ui, "freeInputField", inputField);
        Wire(ui, "sendButton", sendBtn);
        Wire(ui, "freeInputToggle", freeToggle);
        Wire(ui, "actionButtonsContainer", actionContainer.transform);
        Wire(ui, "actionButtonPrefab", GetPrefabComponent<Button>(prefabs["actionButtonPrefab"]));
        Wire(ui, "loadingPanel", loadingObj);
        Wire(ui, "loadingText", loadingT);
        Wire(ui, "questProposalPanel", qpPanel);
        Wire(ui, "questProposalTitle", qpTitle);
        Wire(ui, "questProposalDesc", qpDesc);
        Wire(ui, "questProposalRequirements", qpReqs);
        Wire(ui, "questProposalRewards", qpRewards);
        Wire(ui, "questAcceptButton", qpAccept);
        Wire(ui, "questRejectButton", qpReject);

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ─── NPC PROFILE ───

    static NpcProfilePanel SetupNpcProfilePanel(Transform parent, Dictionary<string, GameObject> prefabs)
    {
        var existing = parent.Find("NpcProfilePanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreatePanel(parent, "NpcProfilePanel", new Vector2(350, 400));
        var ui = root.AddComponent<NpcProfilePanel>();

        var npcName = AddChild<TextMeshProUGUI>(root, "NpcNameText", new Vector2(0, 175), new Vector2(200, 24));
        npcName.fontSize = 18; npcName.alignment = TextAlignmentOptions.Center;
        var relText = AddChild<TextMeshProUGUI>(root, "RelationshipText", new Vector2(0, 150), new Vector2(200, 20));
        relText.fontSize = 12;
        var moodText = AddChild<TextMeshProUGUI>(root, "MoodText", new Vector2(0, 130), new Vector2(200, 20));
        moodText.fontSize = 12;

        var scrollObj = CreateScrollArea(root.transform, "Memories", new Vector2(0, -30), new Vector2(320, 280));

        Wire(ui, "panel", root);
        Wire(ui, "npcNameText", npcName);
        Wire(ui, "relationshipText", relText);
        Wire(ui, "moodText", moodText);
        Wire(ui, "memoriesScrollRect", scrollObj.scroll);
        Wire(ui, "memoriesContent", scrollObj.content.transform);
        Wire(ui, "memoryEntryPrefab", GetPrefabComponent<TextMeshProUGUI>(prefabs["memoryEntryPrefab"]));

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ─── NPC QUEST ───

    static NpcQuestPanel SetupNpcQuestPanel(Transform parent)
    {
        var existing = parent.Find("NpcQuestPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreatePanel(parent, "NpcQuestPanel", new Vector2(400, 350));
        var ui = root.AddComponent<NpcQuestPanel>();

        var npcName = AddChild<TextMeshProUGUI>(root, "NpcName", new Vector2(0, 155), new Vector2(200, 24));
        npcName.fontSize = 16;
        var qTitle = AddChild<TextMeshProUGUI>(root, "QuestTitle", new Vector2(0, 130), new Vector2(350, 22));
        qTitle.fontSize = 14;
        var qDesc = AddChild<TextMeshProUGUI>(root, "QuestDesc", new Vector2(0, 90), new Vector2(350, 50));
        qDesc.fontSize = 12;
        var statusT = AddChild<TextMeshProUGUI>(root, "Status", new Vector2(0, 50), new Vector2(350, 20));
        statusT.fontSize = 12;
        var reqsT = AddChild<TextMeshProUGUI>(root, "Requirements", new Vector2(0, 15), new Vector2(350, 40));
        reqsT.fontSize = 11;
        var rewardsT = AddChild<TextMeshProUGUI>(root, "Rewards", new Vector2(0, -30), new Vector2(350, 40));
        rewardsT.fontSize = 11; rewardsT.color = Color.yellow;

        var acceptBtn = CreateChildButton(root, "AcceptButton", "Accept", new Vector2(-70, -100), new Vector2(120, 36));
        var completeBtn = CreateChildButton(root, "CompleteButton", "Complete", new Vector2(70, -100), new Vector2(120, 36));
        var acceptLbl = acceptBtn.GetComponentInChildren<TextMeshProUGUI>();
        var completeLbl = completeBtn.GetComponentInChildren<TextMeshProUGUI>();

        Wire(ui, "panel", root);
        Wire(ui, "npcNameText", npcName);
        Wire(ui, "questTitleText", qTitle);
        Wire(ui, "questDescText", qDesc);
        Wire(ui, "statusText", statusT);
        Wire(ui, "requirementsText", reqsT);
        Wire(ui, "rewardsText", rewardsT);
        Wire(ui, "acceptButton", acceptBtn);
        Wire(ui, "completeButton", completeBtn);
        Wire(ui, "acceptButtonText", acceptLbl);
        Wire(ui, "completeButtonText", completeLbl);

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ─── PAUSE MENU ───

    static PauseMenuUI SetupPauseMenuUI(Transform parent)
    {
        var existing = parent.Find("PauseMenuPanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var root = CreateRTChild(parent, "PauseMenuPanel", Vector2.zero, Vector2.zero);
        Stretch(root);
        var ui = root.AddComponent<PauseMenuUI>();

        var overlay = root.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.6f);

        var panel = CreateRTChild(root.transform, "Panel", Vector2.zero, new Vector2(300, 250));
        panel.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

        var resumeBtn = CreateChildButton(panel, "ResumeButton", "Resume", new Vector2(0, 70), new Vector2(200, 40));
        var saveBtn = CreateChildButton(panel, "SaveButton", "Save", new Vector2(0, 10), new Vector2(200, 40));
        var mainMenuBtn = CreateChildButton(panel, "MainMenuButton", "Main Menu", new Vector2(0, -50), new Vector2(200, 40));
        var saveConfirm = AddChild<TextMeshProUGUI>(panel, "SaveConfirm", new Vector2(0, -100), new Vector2(200, 20));
        saveConfirm.fontSize = 12; saveConfirm.color = Color.green; saveConfirm.text = "";

        Wire(ui, "panel", root);
        Wire(ui, "overlayImage", overlay);
        Wire(ui, "resumeButton", resumeBtn);
        Wire(ui, "saveButton", saveBtn);
        Wire(ui, "mainMenuButton", mainMenuBtn);
        Wire(ui, "saveConfirmText", saveConfirm);

        root.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    // ─── HELPERS ───

    struct BarResult { public GameObject root; public Image fill; public TextMeshProUGUI text; }

    static BarResult CreateBar(Transform parent, string name, Vector2 pos, Color fillColor, string label)
    {
        var bar = CreateRTChild(parent, name, pos, new Vector2(200, 20));
        bar.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
        var fillObj = CreateRTChild(bar.transform, "Fill", Vector2.zero, new Vector2(200, 20));
        var fill = fillObj.AddComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        var text = AddChild<TextMeshProUGUI>(bar, "Text", Vector2.zero, new Vector2(190, 18));
        text.fontSize = 11; text.text = label; text.alignment = TextAlignmentOptions.Center;
        return new BarResult { root = bar, fill = fill, text = text };
    }

    struct ScrollResult { public ScrollRect scroll; public GameObject content; }

    static ScrollResult CreateScrollArea(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var scrollObj = CreateRTChild(parent, name, pos, size);
        scrollObj.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.5f);
        scrollObj.AddComponent<RectMask2D>();
        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;

        var content = CreateRTChild(scrollObj.transform, "Content", Vector2.zero, new Vector2(size.x - 20, size.y));
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);

        scroll.content = contentRT;
        return new ScrollResult { scroll = scroll, content = content };
    }

    static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        var obj = CreateRTChild(parent, name, Vector2.zero, size);
        obj.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f, 0.95f);
        return obj;
    }

    static GameObject CreateRTChild(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return obj;
    }

    static void Stretch(GameObject obj)
    {
        var rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static T AddChild<T>(GameObject parent, string name, Vector2 pos, Vector2 size) where T : Component
    {
        var obj = CreateRTChild(parent.transform, name, pos, size);
        return obj.AddComponent<T>();
    }

    static Button CreateChildButton(GameObject parent, string name, string label, Vector2 pos, Vector2 size)
    {
        var obj = CreateRTChild(parent.transform, name, pos, size);
        obj.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.4f);
        var btn = obj.AddComponent<Button>();
        var t = AddChild<TextMeshProUGUI>(obj, "Label", Vector2.zero, size - new Vector2(4, 4));
        t.fontSize = Mathf.Min(14, (int)(size.y * 0.5f));
        t.text = label;
        t.alignment = TextAlignmentOptions.Center;
        return btn;
    }

    static T GetPrefabComponent<T>(GameObject prefab) where T : Component
    {
        return prefab.GetComponent<T>() ?? prefab.GetComponentInChildren<T>();
    }

    static void Wire(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void WireArray(Object target, string fieldName, Object[] values)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null && prop.isArray)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
        {
            var parts = path.TrimEnd('/').Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }

    // ─── ANCHOR HELPERS ───

    static void AnchorBottomLeft(GameObject obj)
    {
        var rt = obj.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
    }

    static void AnchorBottomRight(GameObject obj)
    {
        var rt = obj.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(1, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(1, 0);
    }

    static void AnchorBottomCenter(GameObject obj)
    {
        var rt = obj.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(0.5f, 0);
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
    }

    static void AnchorTopCenter(GameObject obj)
    {
        var rt = obj.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
    }
}
