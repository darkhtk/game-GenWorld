using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        Debug.Log("[SceneSetup] All 3 scenes created in Assets/Scenes/");
    }

    [MenuItem("GenWorld/Setup Scenes/BootScene")]
    public static void CreateBootScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera");
        var camera = cam.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 384;
        camera.backgroundColor = Color.black;
        cam.tag = "MainCamera";

        var canvas = CreateCanvas("BootCanvas");
        var splashText = CreateTMPText(canvas.transform, "SplashText", "GenWorld", 48);
        var rt = splashText.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;

        var loadingText = CreateTMPText(canvas.transform, "LoadingText", "Loading...", 24);
        var lrt = loadingText.GetComponent<RectTransform>();
        lrt.anchoredPosition = new Vector2(0, -80);

        EditorSceneManager.SaveScene(scene, ScenePath + "BootScene.unity");
        Debug.Log("[SceneSetup] BootScene created");
    }

    [MenuItem("GenWorld/Setup Scenes/MainMenuScene")]
    public static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cam = new GameObject("Main Camera");
        var camera = cam.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 384;
        camera.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cam.tag = "MainCamera";

        var canvas = CreateCanvas("MenuCanvas");

        var title = CreateTMPText(canvas.transform, "TitleText", "GenWorld", 64);
        title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 120);

        CreateButton(canvas.transform, "NewGameButton", "New Game", new Vector2(0, -20));
        CreateButton(canvas.transform, "ContinueButton", "Continue", new Vector2(0, -100));

        EditorSceneManager.SaveScene(scene, ScenePath + "MainMenuScene.unity");
        Debug.Log("[SceneSetup] MainMenuScene created");
    }

    [MenuItem("GenWorld/Setup Scenes/GameScene")]
    public static void CreateGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var cam = new GameObject("Main Camera");
        var camera = cam.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 384;
        camera.backgroundColor = new Color(0.15f, 0.2f, 0.15f);
        cam.tag = "MainCamera";

        // Grid + Tilemaps
        var grid = new GameObject("Grid");
        var gridComp = grid.AddComponent<Grid>();
        gridComp.cellSize = new Vector3(32, 32, 1);

        var ground = new GameObject("GroundTilemap");
        ground.transform.SetParent(grid.transform);
        ground.AddComponent<Tilemap>();
        ground.AddComponent<TilemapRenderer>();

        var collision = new GameObject("CollisionTilemap");
        collision.transform.SetParent(grid.transform);
        collision.AddComponent<Tilemap>();
        var collisionRenderer = collision.AddComponent<TilemapRenderer>();
        collisionRenderer.sortingOrder = 1;
        collision.AddComponent<TilemapCollider2D>();

        // Player placeholder
        var player = new GameObject("Player");
        player.transform.position = new Vector3(100 * 32, -100 * 32, 0);
        var sr = player.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10;
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        player.AddComponent<BoxCollider2D>();

        // Canvas (HUD)
        var canvas = CreateCanvas("HUDCanvas");

        EditorSceneManager.SaveScene(scene, ScenePath + "GameScene.unity");
        Debug.Log("[SceneSetup] GameScene created");
    }

    static GameObject CreateCanvas(string name)
    {
        var canvasObj = new GameObject(name);
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
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
        var btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        var rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 60);
        rt.anchoredPosition = pos;

        var img = btnObj.AddComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.4f);
        btnObj.AddComponent<Button>();

        var textObj = CreateTMPText(btnObj.transform, "Label", label, 28);
        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        return btnObj;
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
        Debug.Log("[PrefabSetup] All 4 prefabs created in Assets/Prefabs/");
    }

    [MenuItem("GenWorld/Setup Prefabs/Monster")]
    public static void CreateMonsterPrefab()
    {
        EnsureDirectory(PrefabPath);
        var obj = new GameObject("Monster");
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        var rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        obj.AddComponent<BoxCollider2D>();
        obj.AddComponent<MonsterController>();

        PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + "Monster.prefab");
        Object.DestroyImmediate(obj);
        Debug.Log("[PrefabSetup] Monster prefab created");
    }

    [MenuItem("GenWorld/Setup Prefabs/NPC")]
    public static void CreateNPCPrefab()
    {
        EnsureDirectory(PrefabPath);
        var obj = new GameObject("NPC");
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 5;
        var rb = obj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        obj.AddComponent<BoxCollider2D>();
        obj.AddComponent<VillageNPC>();

        PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + "NPC.prefab");
        Object.DestroyImmediate(obj);
        Debug.Log("[PrefabSetup] NPC prefab created");
    }

    [MenuItem("GenWorld/Setup Prefabs/Projectile")]
    public static void CreateProjectilePrefab()
    {
        EnsureDirectory(PrefabPath);
        var obj = new GameObject("Projectile");
        var sr = obj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 8;
        var col = obj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 4f;
        obj.AddComponent<Projectile>();

        PrefabUtility.SaveAsPrefabAsset(obj, PrefabPath + "Projectile.prefab");
        Object.DestroyImmediate(obj);
        Debug.Log("[PrefabSetup] Projectile prefab created");
    }

    [MenuItem("GenWorld/Setup Prefabs/DamageNumber")]
    public static void CreateDamageNumberPrefab()
    {
        EnsureDirectory(PrefabPath);
        var obj = new GameObject("DamageNumber");
        var canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        var rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);

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
        Debug.Log("[PrefabSetup] DamageNumber prefab created");
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
