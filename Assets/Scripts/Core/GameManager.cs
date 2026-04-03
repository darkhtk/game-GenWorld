using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] PlayerController player;
    [SerializeField] WorldMapGenerator worldMap;
    [SerializeField] MonsterSpawner monsterSpawner;
    [SerializeField] CombatManager combatManager;
    [SerializeField] UIManager uiManager;

    public DataManager Data { get; private set; }
    public InventorySystem Inventory { get; private set; }
    public SkillSystem Skills { get; private set; }
    public CraftingSystem Crafting { get; private set; }
    public QuestSystem Quests { get; private set; }
    public AIManager AI { get; private set; }
    public PlayerStats PlayerState { get; private set; }
    public EffectHolder PlayerEffects { get; private set; }
    public RegionTracker RegionTracker { get; private set; }
    public TimeSystem TimeSystem { get; private set; }
    public AchievementSystem Achievements { get; private set; }
    public WorldEventSystem WorldEvents { get; private set; }

    readonly Dictionary<string, int> _killCounts = new();
    float _hpRegenAccum;
    public bool AutoPotionEnabled { get; set; } = true;
    float _lastAutoPotionTime;
    float _lastAutoSaveTime;
    string _lastRegionId = "";
    int _lastHudHp = -1, _lastHudMp = -1;

    DialogueController _dialogue;
    SaveController _save;
    CombatRewardHandler _combatRewards;
    Camera _cachedCam;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        EventBus.Clear();
    }

    async System.Threading.Tasks.Task InitAISafe()
    {
        try { await AI.Init(); }
        catch (System.Exception ex) { Debug.LogError($"[GameManager] AI init failed: {ex.Message}"); }
    }

    void Start()
    {
        Data = new DataManager();
        Data.LoadAll();

        PlayerState = new PlayerStats();
        PlayerEffects = new EffectHolder();
        Inventory = new InventorySystem();
        Skills = new SkillSystem(Data.Skills);
        Crafting = new CraftingSystem(Data.Recipes, Data.Items);
        Quests = new QuestSystem(Data.QuestList);
        AI = new AIManager();
        _ = InitAISafe();
        RegionTracker = new RegionTracker(Data.RegionList);
        TimeSystem = new TimeSystem();
        Achievements = new AchievementSystem();
        Achievements.Init();
        WorldEvents = new WorldEventSystem();
        WorldEvents.Init();

        PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
        PlayerState.FullHeal();
        player.SetSpeed(PlayerState.CurrentStats.spd);

        _combatRewards = new CombatRewardHandler(this, player, PlayerState, Inventory,
            Data, uiManager, combatManager, monsterSpawner, _killCounts);

        combatManager.Init(player, () => PlayerState.CurrentStats, () => PlayerState.Level,
            _combatRewards.OnMonsterKilled, _combatRewards.OnPlayerDeath, PlayerEffects);
        combatManager.Skills = Skills;
        combatManager.PlayerState = PlayerState;

        worldMap.Generate(Data.RegionList);
        InitMinimap();
        SpawnInitialRegion();

        _dialogue = new DialogueController(this, player, uiManager, AI, Quests,
            Inventory, Data, Skills, PlayerState);
        _dialogue.RegisterNpcs();

        _save = new SaveController();

        Quests.SubscribeEvents();

        var uiWiring = new GameUIWiring(uiManager, Inventory, Skills, PlayerState,
            player, Data, Quests, combatManager, UsePotion, SaveGame);
        uiWiring.WireAll(this, RegionTracker,
            () => _lastAutoSaveTime, v => _lastAutoSaveTime = v, PlayRegionBGM);

        if (SaveSystem.HasSave())
            LoadGame();

        uiWiring.PushInitialState();
        PlayRegionBGM();
        Debug.Log("[GameManager] Initialized");
    }

    void Update()
    {
        if (player == null || PlayerState == null) return;
        if (player.Frozen) return;

        TimeSystem?.Update(Time.deltaTime);
        WorldEvents?.Update(TimeSystem?.GameHour ?? 8f);
        float nowMs = Time.time * 1000f;
        var monsters = monsterSpawner.ActiveMonsters;

        Vector2 playerPos = player.Position;
        float nowSec = Time.time;
        foreach (var m in monsters)
        {
            if (m != null && !m.IsDead)
                m.UpdateAI(playerPos, nowSec);
        }

        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            var m = monsters[i];
            if (m != null && m.IsDead && !m.DeathProcessed)
            {
                m.DeathProcessed = true;
                _combatRewards.OnMonsterKilled(m);
            }
        }

        bool inputBlocked = uiManager != null && uiManager.IsInputBlocked();

        if (!inputBlocked)
        {
            combatManager.PerformAutoAttack(monsters);
            HandleSkillInput();
            if (Input.GetKeyDown(KeyCode.F))
                _dialogue.TryInteract();
        }

        combatManager.HandleMonsterAttacks(monsters, nowMs);
        PlayerEffects.Tick(nowMs);
        RegenHp();
        RefreshHud();
        AutoUsePotion();

        RegionTracker.UpdatePlayerRegion(playerPos.x, playerPos.y);
        HandleRegionTransition();
    }

    const float MinZoom = 4f;
    const float MaxZoom = 20f;
    const float ZoomSpeed = 2f;

    void LateUpdate()
    {
        if (_cachedCam == null) _cachedCam = Camera.main;
        var cam = _cachedCam;
        if (cam == null) return;

        if (player != null)
        {
            var pos = player.transform.position;
            float z = cam.transform.position.z;
            if (z >= 0f) z = -10f;
            cam.transform.position = new Vector3(pos.x, pos.y, z);
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - scroll * ZoomSpeed, MinZoom, MaxZoom);
        }
    }

    void HandleSkillInput()
    {
        for (int i = 0; i < GameConfig.SkillSlotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                combatManager.ExecuteSkill(i);
        }
    }

    void RegenHp()
    {
        _hpRegenAccum += GameConfig.HpRegenPerSecond * Time.deltaTime;
        if (_hpRegenAccum >= 1f)
        {
            int heal = Mathf.FloorToInt(_hpRegenAccum);
            _hpRegenAccum -= heal;
            PlayerState.Hp = Mathf.Min(PlayerState.Hp + heal, PlayerState.CurrentStats.maxHp);
        }
    }

    void RefreshHud()
    {
        if (uiManager == null || uiManager.Hud == null) return;
        int hp = PlayerState.Hp, mp = PlayerState.Mp;
        if (hp == _lastHudHp && mp == _lastHudMp) return;
        _lastHudHp = hp;
        _lastHudMp = mp;
        var s = PlayerState.CurrentStats;
        uiManager.Hud.UpdateBars(hp, s.maxHp, mp, s.maxMp);
    }

    void HandleRegionTransition()
    {
        string regionId = RegionTracker.CurrentRegionId;
        if (regionId == _lastRegionId) return;
        _lastRegionId = regionId;
        if (string.IsNullOrEmpty(regionId)) return;

        if (Data.Regions.TryGetValue(regionId, out var region))
            monsterSpawner.SpawnForRegion(region, Data.Monsters, worldMap);
    }

    void InitMinimap()
    {
        var minimap = uiManager.Hud?.GetComponentInChildren<MinimapUI>(true);
        if (minimap == null)
            minimap = FindFirstObjectByType<MinimapUI>(FindObjectsInactive.Include);

        if (minimap == null)
        {
            Debug.LogWarning("[GameManager] MinimapUI not found in scene");
            return;
        }
        if (worldMap.Walkable == null)
        {
            Debug.LogWarning("[GameManager] worldMap.Walkable is null - minimap skipped");
            return;
        }
        minimap.Init(worldMap.Walkable, GameConfig.MapWidthTiles, GameConfig.MapHeightTiles);
        Debug.Log($"[GameManager] Minimap initialized ({GameConfig.MapWidthTiles}x{GameConfig.MapHeightTiles})");
    }

    void SpawnInitialRegion()
    {
        RegionTracker.UpdatePlayerRegion(player.Position.x, player.Position.y);
        _lastRegionId = RegionTracker.CurrentRegionId;
        if (Data.Regions.TryGetValue(_lastRegionId, out var region))
            monsterSpawner.SpawnForRegion(region, Data.Monsters, worldMap);
    }

    void PlayRegionBGM()
    {
        string region = RegionTracker?.CurrentRegionId ?? "";
        string bgm = region switch
        {
            "village" => "bgm_village",
            "forest" => "bgm_forest",
            "cave" or "deep_cave" => "bgm_cave",
            "swamp" or "dark_swamp" => "bgm_forest",
            "volcano" => "bgm_boss",
            "dragon_lair" => "bgm_boss",
            _ => "bgm_village"
        };
        string amb = region switch
        {
            "village" => "amb_village",
            "forest" or "swamp" or "dark_swamp" => "amb_forest",
            "cave" or "deep_cave" => "amb_cave",
            _ => "amb_village"
        };
        AudioManager.Instance?.PlayBGM(bgm);
        AudioManager.Instance?.PlayAmbient(amb);
    }

    void AutoUsePotion()
    {
        if (!AutoPotionEnabled || PlayerState == null) return;
        if (Time.time - _lastAutoPotionTime < 1f) return;
        float hpPct = (float)PlayerState.Hp / PlayerState.CurrentStats.maxHp;
        if (hpPct > 0.3f) return;
        if (Inventory.GetCount("hp_potion") <= 0) return;
        _lastAutoPotionTime = Time.time;
        UsePotion("hp_potion");
    }

    void UsePotion(string potionId)
    {
        if (!Inventory.RemoveItem(potionId, 1)) return;
        if (!Data.Items.TryGetValue(potionId, out var def)) return;
        AudioManager.Instance?.PlaySFX("sfx_potion");
        var s = PlayerState.CurrentStats;
        if (def.healHp > 0)
            PlayerState.Hp = Mathf.Min(PlayerState.Hp + def.healHp, s.maxHp);
        if (def.healMp > 0)
            PlayerState.Mp = Mathf.Min(PlayerState.Mp + def.healMp, s.maxMp);
    }

    void SaveGame()
    {
        _save.Save(player, PlayerState, Inventory, Skills, AI, Quests,
            _killCounts, _combatRewards.TotalKills);
    }

    void LoadGame()
    {
        _save.Load(player, PlayerState, Inventory, Skills, AI, Quests,
            _killCounts, v => _combatRewards.SetTotalKills(v), Data);
    }
}
