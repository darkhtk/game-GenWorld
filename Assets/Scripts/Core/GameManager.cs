using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    readonly List<VillageNPC> _npcs = new();
    int _totalKills;
    float _hpRegenAccum;
    public bool AutoPotionEnabled { get; set; } = true;
    float _lastAutoPotionTime;
    float _lastAutoSaveTime;
    string _lastRegionId = "";
    int _lastHudHp = -1, _lastHudMp = -1;

    // Dialogue state
    readonly List<DialogueEntry> _dialogueHistory = new();
    VillageNPC _dialogueNpc;
    bool _dialogueGenerating;
    Dictionary<string, string> _loreCache;

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
        TimeSystem = new TimeSystem();
        Achievements = new AchievementSystem();
        Achievements.Init();
        WorldEvents = new WorldEventSystem();
        WorldEvents.Init();

        PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
        PlayerState.FullHeal();
        player.SetSpeed(PlayerState.CurrentStats.spd);

        combatManager.Init(player, () => PlayerState.CurrentStats, () => PlayerState.Level,
            OnMonsterKilled, OnPlayerDeath, PlayerEffects);
        combatManager.Skills = Skills;
        combatManager.PlayerState = PlayerState;

        worldMap.Generate(Data.RegionList);
        InitMinimap();
        SpawnInitialRegion();
        RegisterNpcs();
        Quests.SubscribeEvents();
        SubscribeEvents();
        WirePotionCallbacks();
        WireUICallbacks();
        WireAudio();

        if (SaveSystem.HasSave())
            LoadGame();

        PushInitialUiState();
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

        combatManager.PerformAutoAttack(monsters);
        combatManager.HandleMonsterAttacks(monsters, nowMs);
        HandleSkillInput();

        PlayerEffects.Tick(nowMs);
        RegenHp();
        RefreshHud();
        AutoUsePotion();

        RegionTracker.UpdatePlayerRegion(playerPos.x, playerPos.y);
        HandleRegionTransition();

        if (Input.GetKeyDown(KeyCode.F))
            TryInteractNPC();
    }

    void TryInteractNPC()
    {
        Vector2 pp = player.Position;
        VillageNPC nearest = null;
        float bestDist = float.MaxValue;
        foreach (var npc in _npcs)
        {
            if (npc == null) continue;
            float d = (npc.Position - pp).sqrMagnitude;
            if (d < bestDist && npc.IsInInteractionRange(pp))
            {
                bestDist = d;
                nearest = npc;
            }
        }
        if (nearest == null) return;

        nearest.StopMoving();
        player.Frozen = true;
        uiManager.SetDialogueOpen(true);
        _dialogueNpc = nearest;
        _dialogueHistory.Clear();

        var conditional = nearest.EvaluateConditionalDialogue(Quests, Inventory);
        var dlg = uiManager.Dialogue;
        if (dlg == null) return;

        dlg.ClearLog();
        dlg.Show(nearest.Def, conditional);
        EventBus.Emit(new DialogueStartEvent { npcId = nearest.Def.id });

        // Set action buttons from NPC definition
        if (nearest.Def.actions != null && nearest.Def.actions.Length > 0)
            dlg.SetActionButtons(nearest.Def.actions);

        // Wire OnPlayerResponse — drives the AI dialogue loop
        var capturedNpc = nearest;
        dlg.OnPlayerResponse = playerText =>
        {
            if (_dialogueGenerating) return;
            dlg.AppendLog("You", playerText);
            _dialogueHistory.Add(new DialogueEntry { role = "player", text = playerText });
            _ = HandleDialogueResponse(capturedNpc, playerText);
        };

        // Wire OnAction — handle NPC-specific actions
        dlg.OnAction = action => HandleNpcAction(capturedNpc, action);

        // Wire OnClose
        dlg.OnClose = () =>
        {
            uiManager.SetDialogueOpen(false);
            player.Frozen = false;
            capturedNpc.ResumeMoving();
            int turns = _dialogueHistory.Count(e => e.role == "npc");
            EventBus.Emit(new DialogueEndEvent { npcId = capturedNpc.Def.id, turns = turns });
            _dialogueNpc = null;
            _dialogueGenerating = false;
        };

        // If no conditional greeting, generate initial AI greeting
        if (conditional == null)
            _ = HandleDialogueResponse(capturedNpc, "안녕하세요");
    }

    async System.Threading.Tasks.Task HandleDialogueResponse(VillageNPC npc, string playerInput)
    {
        _dialogueGenerating = true;
        var dlg = uiManager.Dialogue;
        dlg?.ShowLoading(true);

        string loreContext = BuildLoreContext(npc.Def);

        var response = await AI.GenerateDialogue(
            npc.Def.id, playerInput, _dialogueHistory,
            PlayerState.Level, PlayerState.Gold,
            Inventory, Data.Items, Quests,
            loreContext, npc.Def.actions, npc.Def.dialogueTraits);

        dlg?.ShowLoading(false);
        _dialogueGenerating = false;

        if (response == null || dlg == null) return;

        // Add NPC response to history
        _dialogueHistory.Add(new DialogueEntry { role = "npc", text = response.dialogue });

        // Display in UI
        dlg.AppendLog(npc.Def.name, response.dialogue, npc.Def.color);

        if (response.options != null && response.options.Length > 0)
            dlg.ShowOptions(response.options);
        else
        {
            // No options = farewell, auto-close after 1.5s
            StartCoroutine(AutoCloseDialogue(1.5f));
        }

        // Handle quest offer
        if (response.offerQuest)
        {
            var questInfo = Quests.GetQuestStatusForNpc(npc.Def.id, Inventory);
            if (questInfo.HasValue)
            {
                if (questInfo.Value.status == "available")
                {
                    var quest = questInfo.Value.quest;
                    var brain = AI.GetBrain(npc.Def.id);
                    int rel = brain?.GetRelationship("player") ?? 0;
                    float genMult = npc.Def.dialogueTraits?.generosity / 10f ?? 0.5f;
                    var rewards = Quests.GetScaledRewards(quest, 0, rel, genMult);
                    dlg.ShowQuestProposal(quest, rewards);
                }
                else if (questInfo.Value.status == "completable")
                {
                    dlg.OnCompleteQuest?.Invoke(questInfo.Value.quest.id);
                    dlg.AppendLog("System", "Quest completed!", "#00ff00");
                }
            }
        }

        // Handle AI-triggered action
        if (!string.IsNullOrEmpty(response.action))
            HandleNpcAction(npc, response.action);
    }

    System.Collections.IEnumerator AutoCloseDialogue(float delay)
    {
        yield return new WaitForSeconds(delay);
        var dlg = uiManager?.Dialogue;
        if (dlg != null)
        {
            dlg.Hide();
            dlg.OnClose?.Invoke();
        }
    }

    void HandleNpcAction(VillageNPC npc, string action)
    {
        switch (action)
        {
            case "heal_player":
                PlayerState.Hp = PlayerState.CurrentStats.maxHp;
                PlayerState.Mp = PlayerState.CurrentStats.maxMp;
                AudioManager.Instance?.PlaySFX("sfx_potion");
                uiManager.Hud?.AddHistoryEntry("HP/MP fully restored!", Color.green);
                break;
            case "reset_skills":
                int refunded = Skills.ResetAllSkills();
                PlayerState.SkillPoints += refunded;
                uiManager.Hud?.AddHistoryEntry($"Skills reset! (+{refunded} SP)", Color.cyan);
                break;
            case "reset_stats":
                int totalBonus = 0;
                foreach (var kv in PlayerState.BonusStats) totalBonus += kv.Value;
                foreach (var key in new List<string>(PlayerState.BonusStats.Keys))
                    PlayerState.BonusStats[key] = 0;
                PlayerState.StatPoints += totalBonus;
                PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
                uiManager.Hud?.AddHistoryEntry($"Stats reset! (+{totalBonus} points)", Color.cyan);
                break;
            case "open_shop":
                // TODO: open shop UI
                break;
            case "open_crafting":
                // TODO: open crafting UI
                break;
        }
    }

    string BuildLoreContext(NpcDef npcDef)
    {
        if (npcDef.loreDocs == null || npcDef.loreDocs.Length == 0) return null;

        if (_loreCache == null)
        {
            _loreCache = new Dictionary<string, string>();
            string loreDir = Path.Combine(Application.streamingAssetsPath, "Data", "lore");
            if (Directory.Exists(loreDir))
            {
                foreach (string file in Directory.GetFiles(loreDir, "*.md"))
                {
                    string key = Path.GetFileNameWithoutExtension(file);
                    _loreCache[key] = File.ReadAllText(file);
                }
            }
        }

        var sb = new StringBuilder();
        foreach (string doc in npcDef.loreDocs)
        {
            if (_loreCache.TryGetValue(doc, out string content))
            {
                sb.AppendLine(content);
                sb.AppendLine();
            }
        }
        return sb.Length > 0 ? sb.ToString() : null;
    }

    const float MinZoom = 4f;
    const float MaxZoom = 20f;
    const float ZoomSpeed = 2f;

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // Camera follow player
        if (player != null)
        {
            var pos = player.transform.position;
            float z = cam.transform.position.z;
            if (z >= 0f) z = -10f;
            cam.transform.position = new Vector3(pos.x, pos.y, z);
        }

        // Mouse wheel zoom
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
            Debug.LogWarning("[GameManager] worldMap.Walkable is null — minimap skipped");
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

    void RegisterNpcs()
    {
        var npcPrefab = Resources.Load<GameObject>("NPC");
        foreach (var npc in Data.NpcList)
        {
            string profile = Data.NpcProfiles.GetValueOrDefault(npc.id);
            AI.RegisterNpc(npc.id, npc.name, npc.personality, profile);

            // Spawn NPC in village region
            if (npc.patrol != null)
            {
                float x = (npc.patrol.cx + 0.5f) * GameConfig.TileSize;
                float y = -(npc.patrol.cy + 0.5f) * GameConfig.TileSize;
                Vector2 pos = new(x, y);

                GameObject go;
                if (npcPrefab != null)
                    go = Instantiate(npcPrefab, pos, Quaternion.identity);
                else
                {
                    go = new GameObject($"NPC_{npc.id}");
                    go.AddComponent<SpriteRenderer>().sortingOrder = 5;
                    var rb = go.AddComponent<Rigidbody2D>();
                    rb.gravityScale = 0;
                    rb.freezeRotation = true;
                }

                var villageNpc = go.GetComponent<VillageNPC>();
                if (villageNpc == null) villageNpc = go.AddComponent<VillageNPC>();
                villageNpc.Init(npc, pos);
                villageNpc.Brain = AI.GetBrain(npc.id);
                _npcs.Add(villageNpc);
            }
        }
    }

    void WireAudio()
    {
        EventBus.On<MonsterKillEvent>(_ =>
            AudioManager.Instance?.PlaySFX("sfx_monster_die"));
        EventBus.On<LevelUpEvent>(_ =>
            AudioManager.Instance?.PlaySFX("sfx_levelup"));
        EventBus.On<GoldChangeEvent>(_ =>
            AudioManager.Instance?.PlaySFX("sfx_coin"));
        EventBus.On<QuestCompleteEvent>(_ =>
            AudioManager.Instance?.PlaySFX("sfx_quest_complete"));
        EventBus.On<AchievementUnlockedEvent>(_ =>
            AudioManager.Instance?.PlaySFX("sfx_rank_up"));
        EventBus.On<PlayerDeathEvent>(_ =>
            AudioManager.Instance?.PlaySFX("sfx_defeat_jingle"));
        EventBus.On<SaveEvent>(_ =>
            AudioManager.Instance?.PlaySFX("sfx_confirm"));
        EventBus.On<RegionVisitEvent>(e => PlayRegionBGM());
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

    void WirePotionCallbacks()
    {
        if (uiManager == null) return;
        uiManager.OnUseHpPotion = () => UsePotion("hp_potion");
        uiManager.OnUseMpPotion = () => UsePotion("mp_potion");
    }

    void WireUICallbacks()
    {
        if (uiManager == null) return;

        // Inventory callbacks
        var inv = uiManager.Inventory;
        if (inv != null)
        {
            inv.OnEquipCallback = slotIdx =>
            {
                var item = Inventory.GetSlot(slotIdx);
                if (item == null || !Data.Items.TryGetValue(item.itemId, out var def)) return;
                string slot = ItemTypeUtil.GetEquipSlot(def.TypeEnum);
                if (string.IsNullOrEmpty(slot)) return;
                // Unequip current
                if (PlayerState.Equipment.TryGetValue(slot, out var old) && old != null)
                    Inventory.AddItem(old.itemId, 1, false, 1);
                PlayerState.Equipment[slot] = item;
                Inventory.RemoveAtSlot(slotIdx);
                PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
                player.SetSpeed(PlayerState.CurrentStats.spd);
                EventBus.Emit(new EquipChangeEvent());
                AudioManager.Instance?.PlaySFX("sfx_confirm");
                inv.Refresh();
            };
            inv.OnUnequipCallback = slot =>
            {
                if (!PlayerState.Equipment.TryGetValue(slot, out var item) || item == null) return;
                bool stackable = Data.Items.TryGetValue(item.itemId, out var def) && def.stackable;
                Inventory.AddItem(item.itemId, 1, stackable, def?.maxStack ?? 1);
                PlayerState.Equipment[slot] = null;
                PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
                player.SetSpeed(PlayerState.CurrentStats.spd);
                EventBus.Emit(new EquipChangeEvent());
                AudioManager.Instance?.PlaySFX("sfx_confirm");
                inv.Refresh();
            };
            inv.OnUseItemCallback = slotIdx =>
            {
                var item = Inventory.GetSlot(slotIdx);
                if (item == null || !Data.Items.TryGetValue(item.itemId, out var def)) return;
                if (def.healHp > 0 || def.healMp > 0)
                {
                    UsePotion(item.itemId);
                    Inventory.RemoveAtSlot(slotIdx);
                    inv.Refresh();
                }
            };
            inv.OnSortCallback = () =>
            {
                Inventory.SortItems(Data.Items);
                inv.Refresh();
            };
        }

        // SkillTree callbacks
        var st = uiManager.SkillTree;
        if (st != null)
        {
            st.OnLearnSkill = skillId =>
            {
                var result = Skills.LearnSkill(skillId, PlayerState.SkillPoints, PlayerState.Level);
                if (result.learned)
                {
                    PlayerState.SkillPoints = result.remainingPoints;
                    AudioManager.Instance?.PlaySFX("sfx_upgrade");
                    st.Refresh();
                    uiManager.Hud?.UpdateLevel(PlayerState.Level, PlayerState.SkillPoints, PlayerState.StatPoints);
                }
                else
                {
                    AudioManager.Instance?.PlaySFX("sfx_error");
                }
            };
            st.OnEquipSkill = (skillId, slot) =>
            {
                Skills.EquipSkill(skillId, slot);
                AudioManager.Instance?.PlaySFX("sfx_confirm");
                uiManager.Hud?.UpdateSkillBar(Skills.GetEquippedSkills(), new float[GameConfig.SkillSlotCount]);
                st.Refresh();
            };
        }

        // Dialogue callbacks
        var dlg = uiManager.Dialogue;
        if (dlg != null)
        {
            dlg.OnClose = () =>
            {
                uiManager.SetDialogueOpen(false);
                player.Frozen = false;
            };
            dlg.OnAcceptQuest = questId =>
            {
                Quests.AcceptQuest(questId);
                uiManager.Hud?.AddHistoryEntry($"Quest accepted: {questId}", Color.cyan);
            };
            dlg.OnCompleteQuest = questId =>
            {
                var reward = Quests.CompleteQuest(questId, Inventory);
                if (reward == null) return;

                if (reward.gold > 0)
                {
                    PlayerState.Gold += reward.gold;
                    EventBus.Emit(new GoldChangeEvent { gold = PlayerState.Gold });
                }
                if (reward.xp > 0)
                {
                    var state = new PlayerLevelState
                    {
                        level = PlayerState.Level, xp = PlayerState.Xp,
                        skillPoints = PlayerState.SkillPoints, statPoints = PlayerState.StatPoints
                    };
                    StatsSystem.AddXp(ref state, reward.xp);
                    PlayerState.Level = state.level; PlayerState.Xp = state.xp;
                    PlayerState.SkillPoints = state.skillPoints; PlayerState.StatPoints = state.statPoints;
                    PlayerState.RecalcStats(Data.Items, Data.SetBonuses);
                    player.SetSpeed(PlayerState.CurrentStats.spd);
                }
                if (reward.items != null)
                {
                    foreach (var ri in reward.items)
                    {
                        bool stackable = Data.Items.TryGetValue(ri.itemId, out var def) && def.stackable;
                        Inventory.AddItem(ri.itemId, ri.count, stackable, def?.maxStack ?? 1);
                    }
                }
                uiManager.Hud?.AddHistoryEntry(
                    $"Quest complete! +{reward.gold}G +{reward.xp}XP", Color.yellow);
                AudioManager.Instance?.PlaySFX("sfx_quest_complete");
            };
        }

        // PauseMenu callbacks
        var pm = uiManager.PauseMenu;
        if (pm != null)
        {
            pm.OnSaveRequested = () => EventBus.Emit(new SaveEvent());
            pm.OnMainMenuRequested = () => SceneManager.LoadScene("MainMenuScene");
        }
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
        // Death VFX at monster position
        SkillVFX.ShowAtPosition(this, "vfx_monster_death", monster.Position.x, monster.Position.y);

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

        // R-037/R-038: floating XP and gold text
        if (combatManager != null)
        {
            Vector2 textPos = monster.Position + Vector2.up * 1.2f;
            if (def.xp > 0)
                combatManager.ShowFloatingText(textPos, $"+{def.xp} XP", new Color(0.6f, 0.8f, 1f));
            if (def.gold > 0)
                combatManager.ShowFloatingText(textPos + Vector2.up * 0.4f, $"+{def.gold}G", new Color(1f, 0.85f, 0.3f));
        }

        var drops = LootSystem.RollDrops(def.drops);
        if (drops.Count > 0)
            AudioManager.Instance?.PlaySFX("sfx_item_acquire");
        float itemOffset = 0.8f;
        foreach (var drop in drops)
        {
            bool stackable = Data.Items.TryGetValue(drop.itemId, out var itemDef) && itemDef.stackable;
            int maxStack = itemDef?.maxStack ?? 1;
            Inventory.AddItem(drop.itemId, drop.count, stackable, maxStack);

            string itemName = itemDef?.name ?? drop.itemId;
            if (combatManager != null)
                combatManager.ShowFloatingText(
                    monster.Position + Vector2.up * itemOffset,
                    $"+{itemName} x{drop.count}", new Color(0.4f, 1f, 0.4f));
            itemOffset += 0.4f;
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
