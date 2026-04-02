using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;
using UnityEngine.UI;

public static class SceneSetupTool
{
    const string ScenePath = "Assets/Scenes/";
    const string PrefabPath = "Assets/Prefabs/";

    [MenuItem("GenWorld/Setup Scenes/All Scenes")]
    public static void SetupAllScenes()
    {
        CreateBootScene();
        CreateMainMenuScene();
        CreateGameScene();
        Debug.Log("[SceneSetup] All 3 scenes created");
    }

    [MenuItem("GenWorld/Setup Scenes/BootScene")]
    public static void CreateBootScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = CreateCamera("Main Camera", Color.black);

        var canvas = CreateCanvas("BootCanvas");
        var fadeGroup = canvas.AddComponent<CanvasGroup>();

        var splashText = CreateTMPText(canvas.transform, "SplashText", "GenWorld", 48);
        splashText.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        var loadingText = CreateTMPText(canvas.transform, "LoadingText", "Loading...", 24);
        loadingText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -80);

        var controller = canvas.AddComponent<BootSceneController>();
        Wire(controller, "splashText", splashText.GetComponent<TextMeshProUGUI>());
        Wire(controller, "fadeGroup", fadeGroup);

        EditorSceneManager.SaveScene(scene, ScenePath + "BootScene.unity");
        Debug.Log("[SceneSetup] BootScene created + wired");
    }

    [MenuItem("GenWorld/Setup Scenes/MainMenuScene")]
    public static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = CreateCamera("Main Camera", new Color(0.1f, 0.1f, 0.15f));

        var canvas = CreateCanvas("MenuCanvas");

        var title = CreateTMPText(canvas.transform, "TitleText", "GenWorld", 64);
        title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 120);

        var newGameBtn = CreateButton(canvas.transform, "NewGameButton", "New Game", new Vector2(0, -20));
        var continueBtn = CreateButton(canvas.transform, "ContinueButton", "Continue", new Vector2(0, -100));

        var controller = canvas.AddComponent<MainMenuController>();
        Wire(controller, "newGameButton", newGameBtn.GetComponent<Button>());
        Wire(controller, "continueButton", continueBtn.GetComponent<Button>());
        Wire(controller, "titleText", title.GetComponent<TextMeshProUGUI>());

        EditorSceneManager.SaveScene(scene, ScenePath + "MainMenuScene.unity");
        Debug.Log("[SceneSetup] MainMenuScene created + wired");
    }

    [MenuItem("GenWorld/Setup Scenes/GameScene")]
    public static void CreateGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var cam = CreateCamera("Main Camera", new Color(0.15f, 0.2f, 0.15f));

        // Grid + Tilemaps
        var grid = new GameObject("Grid");
        var gridComp = grid.AddComponent<Grid>();
        gridComp.cellSize = new Vector3(32, 32, 1);

        var groundObj = new GameObject("GroundTilemap");
        groundObj.transform.SetParent(grid.transform);
        var groundTilemap = groundObj.AddComponent<Tilemap>();
        groundObj.AddComponent<TilemapRenderer>();

        var collisionObj = new GameObject("CollisionTilemap");
        collisionObj.transform.SetParent(grid.transform);
        var collisionTilemap = collisionObj.AddComponent<Tilemap>();
        var collisionRenderer = collisionObj.AddComponent<TilemapRenderer>();
        collisionRenderer.sortingOrder = 1;
        collisionObj.AddComponent<TilemapCollider2D>();

        // Player
        var playerObj = new GameObject("Player");
        playerObj.transform.position = new Vector3(100 * 32, -100 * 32, 0);
        var playerSr = playerObj.AddComponent<SpriteRenderer>();
        playerSr.sortingOrder = 10;
        var playerRb = playerObj.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 0;
        playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerObj.AddComponent<BoxCollider2D>();
        var playerCtrl = playerObj.AddComponent<PlayerController>();

        // WorldMapGenerator
        var worldMapObj = new GameObject("WorldMapGenerator");
        var worldMap = worldMapObj.AddComponent<WorldMapGenerator>();
        Wire(worldMap, "groundTilemap", groundTilemap);
        Wire(worldMap, "collisionTilemap", collisionTilemap);

        // MonsterSpawner
        var spawnerObj = new GameObject("MonsterSpawner");
        var spawner = spawnerObj.AddComponent<MonsterSpawner>();
        var monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath + "Monster.prefab");
        if (monsterPrefab != null)
            Wire(spawner, "monsterPrefab", monsterPrefab);

        // CombatManager
        var combatObj = new GameObject("CombatManager");
        var combatMgr = combatObj.AddComponent<CombatManager>();

        // UI Canvas + UIManager
        var canvas = CreateCanvas("UICanvas");
        var uiMgr = canvas.AddComponent<UIManager>();

        // HUD
        var hudObj = new GameObject("HUD");
        hudObj.transform.SetParent(canvas.transform, false);
        var hud = hudObj.AddComponent<HUD>();
        Wire(uiMgr, "hud", hud);

        // UI panels (empty shells — Show/Hide will work, Refresh uses null guards)
        Wire(uiMgr, "inventory", CreatePanel<InventoryUI>(canvas.transform, "InventoryPanel"));
        Wire(uiMgr, "shop", CreatePanel<ShopUI>(canvas.transform, "ShopPanel"));
        Wire(uiMgr, "crafting", CreatePanel<CraftingUI>(canvas.transform, "CraftingPanel"));
        Wire(uiMgr, "enhance", CreatePanel<EnhanceUI>(canvas.transform, "EnhancePanel"));
        Wire(uiMgr, "skillTree", CreatePanel<SkillTreeUI>(canvas.transform, "SkillTreePanel"));
        Wire(uiMgr, "quest", CreatePanel<QuestUI>(canvas.transform, "QuestPanel"));
        Wire(uiMgr, "dialogue", CreatePanel<DialogueUI>(canvas.transform, "DialoguePanel"));
        Wire(uiMgr, "npcProfile", CreatePanel<NpcProfilePanel>(canvas.transform, "NpcProfilePanel"));
        Wire(uiMgr, "npcQuest", CreatePanel<NpcQuestPanel>(canvas.transform, "NpcQuestPanel"));
        Wire(uiMgr, "pauseMenu", CreatePanel<PauseMenuUI>(canvas.transform, "PauseMenuPanel"));

        // GameManager (central orchestrator)
        var gmObj = new GameObject("GameManager");
        var gm = gmObj.AddComponent<GameManager>();
        Wire(gm, "player", playerCtrl);
        Wire(gm, "worldMap", worldMap);
        Wire(gm, "monsterSpawner", spawner);
        Wire(gm, "combatManager", combatMgr);
        Wire(gm, "uiManager", uiMgr);

        EditorSceneManager.SaveScene(scene, ScenePath + "GameScene.unity");
        Debug.Log("[SceneSetup] GameScene created + fully wired");
    }

    // --- Prefab Creation ---

    [MenuItem("GenWorld/Setup Prefabs/All Prefabs")]
    public static void SetupAllPrefabs()
    {
        EnsureDirectory(PrefabPath);
        CreateMonsterPrefab();
        CreateNPCPrefab();
        CreateProjectilePrefab();
        CreateDamageNumberPrefab();
        Debug.Log("[PrefabSetup] All 4 prefabs created");
    }

    [MenuItem("GenWorld/Setup Prefabs/Monster")]
    public static void CreateMonsterPrefab()
    {
        EnsureDirectory(PrefabPath);
        var obj = new GameObject("Monster");
        obj.AddComponent<SpriteRenderer>().sortingOrder = 5;
        var rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        obj.AddComponent<BoxCollider2D>();
        obj.AddComponent<MonsterController>();
        PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + "Monster.prefab");
        Object.DestroyImmediate(obj);
    }

    [MenuItem("GenWorld/Setup Prefabs/NPC")]
    public static void CreateNPCPrefab()
    {
        EnsureDirectory(PrefabPath);
        var obj = new GameObject("NPC");
        obj.AddComponent<SpriteRenderer>().sortingOrder = 5;
        var rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        obj.AddComponent<BoxCollider2D>();
        obj.AddComponent<VillageNPC>();
        PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + "NPC.prefab");
        Object.DestroyImmediate(obj);
    }

    [MenuItem("GenWorld/Setup Prefabs/Projectile")]
    public static void CreateProjectilePrefab()
    {
        EnsureDirectory(PrefabPath);
        var obj = new GameObject("Projectile");
        obj.AddComponent<SpriteRenderer>().sortingOrder = 8;
        var col = obj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 4f;
        obj.AddComponent<Projectile>();
        PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + "Projectile.prefab");
        Object.DestroyImmediate(obj);
    }

    [MenuItem("GenWorld/Setup Prefabs/DamageNumber")]
    public static void CreateDamageNumberPrefab()
    {
        EnsureDirectory(PrefabPath);
        var obj = new GameObject("DamageNumber");
        var canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);

        var textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.enableWordWrapping = false;
        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + "DamageNumber.prefab");
        Object.DestroyImmediate(obj);
    }

    // --- Helpers ---

    static GameObject CreateCamera(string name, Color bg)
    {
        var cam = new GameObject(name);
        var c = cam.AddComponent<Camera>();
        c.orthographic = true;
        c.orthographicSize = 384;
        c.backgroundColor = bg;
        cam.tag = "MainCamera";
        return cam;
    }

    static GameObject CreateCanvas(string name)
    {
        var obj = new GameObject(name);
        var canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        obj.AddComponent<CanvasScaler>();
        obj.AddComponent<GraphicRaycaster>();
        return obj;
    }

    static GameObject CreateTMPText(Transform parent, string name, string text, int fontSize)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return obj;
    }

    static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 60);
        rt.anchoredPosition = pos;
        obj.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.4f);
        obj.AddComponent<Button>();

        var textObj = CreateTMPText(obj.transform, "Label", label, 28);
        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return obj;
    }

    static T CreatePanel<T>(Transform parent, string name) where T : MonoBehaviour
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        obj.SetActive(false);
        return obj.AddComponent<T>();
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
        else
        {
            Debug.LogWarning($"[SceneSetup] Field '{fieldName}' not found on {target.GetType().Name}");
        }
    }

    static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
        {
            string parent = System.IO.Path.GetDirectoryName(path.TrimEnd('/'));
            string folder = System.IO.Path.GetFileName(path.TrimEnd('/'));
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
