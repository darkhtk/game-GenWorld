using System.Collections.Generic;
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
        // Assign player sprite
        var playerSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Sprites/player.png");
        foreach (var asset in playerSprites)
        {
            if (asset is Sprite s && s.name.Contains("walk_down_0"))
            {
                playerSr.sprite = s;
                break;
            }
        }
        if (playerSr.sprite == null)
        {
            var fallback = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/player.png");
            if (fallback != null) playerSr.sprite = fallback;
        }
        var playerRb = playerObj.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 0;
        playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerObj.AddComponent<BoxCollider2D>();
        var playerCtrl = playerObj.AddComponent<PlayerController>();
        playerObj.AddComponent<PlayerAnimator>();

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

        // AudioManager (singleton, DontDestroyOnLoad in Awake)
        var audioObj = new GameObject("AudioManager");
        audioObj.AddComponent<AudioManager>();

        // DayNightCycle
        var dayNightObj = new GameObject("DayNightCycle");
        dayNightObj.AddComponent<DayNightCycle>();
        // Light2D — try to add via reflection (URP may not be available)
        var lightObj = new GameObject("GlobalLight2D");
        var light2dType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        if (light2dType != null)
        {
            var light2d = lightObj.AddComponent(light2dType);
            var ltProp = light2dType.GetProperty("lightType");
            if (ltProp != null) ltProp.SetValue(light2d, 3); // 3 = Global
            Wire(dayNightObj.GetComponent<DayNightCycle>(), "globalLight", light2d as Component);
        }
        else
        {
            Debug.LogWarning("[SceneSetup] URP Light2D not found — DayNightCycle will not have lighting control");
        }

        // UI Canvas + UIManager
        var canvas = CreateCanvas("UICanvas");
        var uiMgr = canvas.AddComponent<UIManager>();

        // ScreenFlash (full-screen overlay)
        var flashObj = new GameObject("ScreenFlash");
        flashObj.transform.SetParent(canvas.transform, false);
        var flashRt = flashObj.AddComponent<RectTransform>();
        flashRt.anchorMin = Vector2.zero;
        flashRt.anchorMax = Vector2.one;
        flashRt.offsetMin = Vector2.zero;
        flashRt.offsetMax = Vector2.zero;
        var flashImg = flashObj.AddComponent<Image>();
        flashImg.color = new Color(0, 0, 0, 0);
        flashImg.raycastTarget = false;
        var screenFlash = flashObj.AddComponent<ScreenFlash>();
        Wire(screenFlash, "flashImage", flashImg);

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
        canvas.AddComponent<DeathScreenUI>();

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
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
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

    // --- Tile Asset Creation & Wiring ---

    static readonly string[] TileNames = { "grass", "dirt", "water", "stone_floor", "tree", "bush", "wall" };
    const string TileAssetPath = "Assets/Art/Tiles/";
    const string TilesetPath = "Assets/Art/Tiles/tileset.png";

    [MenuItem("GenWorld/Setup Tiles")]
    public static void SetupTiles()
    {
        // Step 1: Slice tileset into 32x32 sprites
        var importer = AssetImporter.GetAtPath(TilesetPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[TileSetup] tileset.png not found at " + TilesetPath);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var spriteData = new SpriteMetaData[TileNames.Length];
        for (int i = 0; i < TileNames.Length; i++)
        {
            spriteData[i] = new SpriteMetaData
            {
                name = "tileset_" + TileNames[i],
                rect = new Rect(i * 32, 0, 32, 32),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
#pragma warning disable CS0618
        importer.spritesheet = spriteData;
#pragma warning restore CS0618
        importer.SaveAndReimport();

        // Step 2: Load sprites and create Tile assets
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(TilesetPath);
        var sprites = new Dictionary<string, Sprite>();
        foreach (var asset in allAssets)
        {
            if (asset is Sprite s)
                sprites[s.name] = s;
        }

        var tiles = new Dictionary<string, UnityEngine.Tilemaps.Tile>();
        foreach (string tileName in TileNames)
        {
            string spriteName = "tileset_" + tileName;
            if (!sprites.TryGetValue(spriteName, out var sprite))
            {
                Debug.LogWarning($"[TileSetup] Sprite '{spriteName}' not found");
                continue;
            }

            string tilePath = TileAssetPath + tileName + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<UnityEngine.Tilemaps.Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            tile.sprite = sprite;
            tile.colliderType = (tileName == "wall" || tileName == "tree")
                ? UnityEngine.Tilemaps.Tile.ColliderType.Grid
                : UnityEngine.Tilemaps.Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            tiles[tileName] = tile;
        }
        AssetDatabase.SaveAssets();

        Debug.Log($"[TileSetup] Created {tiles.Count} tile assets");

        // Step 3: Wire to WorldMapGenerator in open scene
        var wmg = Object.FindFirstObjectByType<WorldMapGenerator>();
        if (wmg != null)
        {
            if (tiles.TryGetValue("grass", out var t)) Wire(wmg, "grassTile", t);
            if (tiles.TryGetValue("dirt", out t)) Wire(wmg, "dirtTile", t);
            if (tiles.TryGetValue("water", out t)) Wire(wmg, "waterTile", t);
            if (tiles.TryGetValue("stone_floor", out t)) Wire(wmg, "stoneFloorTile", t);
            if (tiles.TryGetValue("tree", out t)) Wire(wmg, "treeTile", t);
            if (tiles.TryGetValue("bush", out t)) Wire(wmg, "bushTile", t);
            if (tiles.TryGetValue("wall", out t)) Wire(wmg, "wallTile", t);

            EditorUtility.SetDirty(wmg);
            EditorSceneManager.MarkSceneDirty(wmg.gameObject.scene);
            Debug.Log("[TileSetup] Wired tiles to WorldMapGenerator — save the scene!");
        }
        else
        {
            Debug.LogWarning("[TileSetup] WorldMapGenerator not found in scene. Open GameScene and run again.");
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
