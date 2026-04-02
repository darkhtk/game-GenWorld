using System.Collections.Generic;
using System.Linq;
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

    readonly Dictionary<string, int> _killCounts = new();
    int _totalKills;
    float _hpRegenAccum;
    float _lastAutoSaveTime;
    string _lastRegionId = "";

    void Awake()
    {
        Instance = this;
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
        _ = AI.Init();
        RegionTracker = new RegionTracker(Data.RegionList);

        PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
        PlayerState.FullHeal();
        player.SetSpeed(PlayerState.CurrentStats.spd);

        combatManager.Init(player, () => PlayerState.CurrentStats, () => PlayerState.Level,
            OnMonsterKilled, OnPlayerDeath, PlayerEffects);
        combatManager.Skills = Skills;
        combatManager.PlayerState = PlayerState;

        worldMap.Generate(Data.RegionList);
        SpawnInitialRegion();
        RegisterNpcs();
        SubscribeEvents();
        WirePotionCallbacks();

        if (SaveSystem.HasSave())
            LoadGame();

        PushInitialUiState();
        Debug.Log("[GameManager] Initialized");
    }

    void Update()
    {
        if (player.Frozen) return;

        float nowMs = Time.time * 1000f;
        var monsters = monsterSpawner.ActiveMonsters;

        combatManager.PerformAutoAttack(monsters);
        combatManager.HandleMonsterAttacks(monsters, nowMs);
        HandleSkillInput();

        PlayerEffects.Tick(nowMs);
        RegenHp();
        RefreshHud();

        RegionTracker.UpdatePlayerRegion(player.Position.x, player.Position.y);
        HandleRegionTransition();
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
        var hud = uiManager.Hud;
        var s = PlayerState.CurrentStats;
        hud.UpdateBars(PlayerState.Hp, s.maxHp, PlayerState.Mp, s.maxMp);
    }

    void PushInitialUiState()
    {
        if (uiManager == null || uiManager.Hud == null) return;
        var hud = uiManager.Hud;
        var s = PlayerState.CurrentStats;
        hud.UpdateBars(PlayerState.Hp, s.maxHp, PlayerState.Mp, s.maxMp);
        hud.UpdateGold(PlayerState.Gold);
        hud.UpdateLevel(PlayerState.Level, PlayerState.SkillPoints, PlayerState.StatPoints);
        hud.UpdateXpBar(PlayerState.Xp, GameConfig.XpForLevel(PlayerState.Level));
        hud.UpdateSkillBar(Skills.GetEquippedSkills(), new float[GameConfig.SkillSlotCount]);
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

    void SpawnInitialRegion()
    {
        RegionTracker.UpdatePlayerRegion(player.Position.x, player.Position.y);
        _lastRegionId = RegionTracker.CurrentRegionId;
        if (Data.Regions.TryGetValue(_lastRegionId, out var region))
            monsterSpawner.SpawnForRegion(region, Data.Monsters, worldMap);
    }

    void RegisterNpcs()
    {
        foreach (var npc in Data.NpcList)
        {
            string profile = Data.NpcProfiles.GetValueOrDefault(npc.id);
            AI.RegisterNpc(npc.id, npc.name, npc.personality, profile);
        }
    }

    void WirePotionCallbacks()
    {
        if (uiManager == null) return;
        uiManager.OnUseHpPotion = () => UsePotion("hp_potion");
        uiManager.OnUseMpPotion = () => UsePotion("mp_potion");
    }

    void UsePotion(string potionId)
    {
        if (!Inventory.RemoveItem(potionId, 1)) return;
        if (!Data.Items.TryGetValue(potionId, out var def)) return;
        var s = PlayerState.CurrentStats;
        if (def.healHp > 0)
            PlayerState.Hp = Mathf.Min(PlayerState.Hp + def.healHp, s.maxHp);
        if (def.healMp > 0)
            PlayerState.Mp = Mathf.Min(PlayerState.Mp + def.healMp, s.maxMp);
    }

    void SubscribeEvents()
    {
        var hud = uiManager != null ? uiManager.Hud : null;

        EventBus.On<LevelUpEvent>(e =>
        {
            ScreenFlash.LevelUp();
            CameraShake.Shake(this, 200f, 0.15f);
        });

        EventBus.On<EquipChangeEvent>(e =>
        {
            PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
            player.SetSpeed(PlayerState.CurrentStats.spd);
        });

        EventBus.On<GoldChangeEvent>(e =>
        {
            if (hud != null) hud.UpdateGold(e.gold);
        });

        EventBus.On<RegionVisitEvent>(e =>
        {
            if (hud != null) hud.UpdateRegion(e.regionName);

            float now = Time.time;
            if (now - _lastAutoSaveTime < 30f) return;
            _lastAutoSaveTime = now;
            EventBus.Emit(new SaveEvent());
            Debug.Log($"[AutoSave] Region changed to {e.regionName}");
        });

        EventBus.On<MonsterKillEvent>(e =>
        {
            if (hud != null)
                hud.AddHistoryEntry($"Defeated {e.monsterName} (x{e.killCount})", Color.white);
        });

        EventBus.On<PlayerDeathEvent>(e =>
        {
            if (hud != null) hud.AddHistoryEntry("You died!", Color.red);
        });

        EventBus.On<SaveEvent>(_ =>
        {
            SaveGame();
            if (hud != null) hud.ShowSaveIndicator();
        });
    }

    void OnMonsterKilled(MonsterController monster)
    {
        var def = monster.Def;
        _killCounts.TryGetValue(def.id, out int count);
        _killCounts[def.id] = ++count;
        _totalKills++;

        int prevLevel = PlayerState.Level;
        var state = new PlayerLevelState
        {
            level = PlayerState.Level, xp = PlayerState.Xp,
            skillPoints = PlayerState.SkillPoints, statPoints = PlayerState.StatPoints
        };
        StatsSystem.AddXp(ref state, def.xp);
        PlayerState.Level = state.level;
        PlayerState.Xp = state.xp;
        PlayerState.SkillPoints = state.skillPoints;
        PlayerState.StatPoints = state.statPoints;

        if (PlayerState.Level > prevLevel)
        {
            PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
            PlayerState.FullHeal();
            player.SetSpeed(PlayerState.CurrentStats.spd);
            var hud = uiManager != null ? uiManager.Hud : null;
            if (hud != null)
            {
                hud.UpdateLevel(PlayerState.Level, PlayerState.SkillPoints, PlayerState.StatPoints);
                hud.UpdateXpBar(PlayerState.Xp, GameConfig.XpForLevel(PlayerState.Level));
                hud.AddHistoryEntry($"Level Up! Lv.{PlayerState.Level}", Color.yellow);
            }
        }

        PlayerState.Gold += def.gold;
        EventBus.Emit(new GoldChangeEvent { gold = PlayerState.Gold });

        var drops = LootSystem.RollDrops(def.drops);
        foreach (var drop in drops)
        {
            bool stackable = Data.Items.TryGetValue(drop.itemId, out var itemDef) && itemDef.stackable;
            int maxStack = itemDef?.maxStack ?? 1;
            Inventory.AddItem(drop.itemId, drop.count, stackable, maxStack);
        }

        EventBus.Emit(new MonsterKillEvent
        {
            monsterId = def.id, monsterName = def.name,
            killCount = count, totalKills = _totalKills
        });

        monsterSpawner.RemoveMonster(monster);
    }

    void OnPlayerDeath()
    {
        int goldLoss = Mathf.FloorToInt(PlayerState.Gold * GameConfig.DeathGoldPenalty);
        PlayerState.Gold -= goldLoss;
        PlayerState.FullHeal();
        EventBus.Emit(new PlayerDeathEvent { deathX = player.Position.x, deathY = player.Position.y });
        EventBus.Emit(new GoldChangeEvent { gold = PlayerState.Gold });
    }

    void SaveGame()
    {
        var (active, completed) = Quests.Serialize();
        SaveSystem.Save(new SaveData
        {
            playerX = player.Position.x, playerY = player.Position.y,
            level = PlayerState.Level, xp = PlayerState.Xp,
            gold = PlayerState.Gold, skillPoints = PlayerState.SkillPoints,
            statPoints = PlayerState.StatPoints,
            hp = PlayerState.Hp, mp = PlayerState.Mp,
            inventory = Inventory.Slots,
            equipment = PlayerState.Equipment,
            learnedSkills = Skills.GetLearnedSkills(),
            equippedSkills = Skills.GetEquippedSkills(),
            npcBrains = AI.SerializeAllBrains(),
            questState = new QuestSaveData { active = active, completed = completed },
            killCounts = _killCounts, totalKills = _totalKills,
            bonusStats = PlayerState.BonusStats,
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
    }

    void LoadGame()
    {
        var save = SaveSystem.Load();
        if (save == null) return;

        player.transform.position = new Vector3(save.playerX, save.playerY, 0);
        PlayerState.Level = save.level;
        PlayerState.Xp = save.xp;
        PlayerState.Gold = save.gold;
        PlayerState.SkillPoints = save.skillPoints;
        PlayerState.StatPoints = save.statPoints;
        PlayerState.Hp = save.hp;
        PlayerState.Mp = save.mp;
        if (save.bonusStats != null) PlayerState.BonusStats = save.bonusStats;

        if (save.inventory != null)
        {
            for (int i = 0; i < save.inventory.Length && i < Inventory.MaxSlots; i++)
                Inventory.Slots[i] = save.inventory[i];
        }

        if (save.equipment != null) PlayerState.Equipment = save.equipment;
        if (save.learnedSkills != null) Skills.RestoreLearnedSkills(save.learnedSkills);
        if (save.equippedSkills != null) Skills.RestoreEquipped(save.equippedSkills);
        if (save.npcBrains != null) AI.RestoreAllBrains(save.npcBrains);
        if (save.questState != null) Quests.Restore((save.questState.active, save.questState.completed));
        if (save.killCounts != null) { foreach (var kv in save.killCounts) _killCounts[kv.Key] = kv.Value; }
        _totalKills = save.totalKills;

        PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
        player.SetSpeed(PlayerState.CurrentStats.spd);

        Debug.Log($"[GameManager] Loaded save — Level {PlayerState.Level}, Gold {PlayerState.Gold}");
    }
}
