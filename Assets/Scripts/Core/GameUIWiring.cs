using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIWiring
{
    readonly UIManager _uiManager;
    readonly InventorySystem _inventory;
    readonly SkillSystem _skills;
    readonly PlayerStats _playerState;
    readonly PlayerController _player;
    readonly DataManager _data;
    readonly QuestSystem _quests;
    readonly CombatManager _combatManager;
    readonly Action<string> _usePotion;
    readonly Action _saveGame;

    public GameUIWiring(UIManager uiManager, InventorySystem inventory, SkillSystem skills,
        PlayerStats playerState, PlayerController player, DataManager data, QuestSystem quests,
        CombatManager combatManager, Action<string> usePotion, Action saveGame)
    {
        _uiManager = uiManager;
        _inventory = inventory;
        _skills = skills;
        _playerState = playerState;
        _player = player;
        _data = data;
        _quests = quests;
        _combatManager = combatManager;
        _usePotion = usePotion;
        _saveGame = saveGame;
    }

    public void WireAll(MonoBehaviour host, RegionTracker regionTracker,
        Func<float> getLastAutoSaveTime, Action<float> setLastAutoSaveTime, Action playRegionBGM)
    {
        WirePotionCallbacks();
        WireInventoryCallbacks();
        WireSkillTreeCallbacks();
        WireDialogueCallbacks();
        WirePauseMenuCallbacks();
        WireAudio(playRegionBGM);
        SubscribeEvents(host, regionTracker, getLastAutoSaveTime, setLastAutoSaveTime, playRegionBGM);
    }

    void WirePotionCallbacks()
    {
        if (_uiManager == null) return;
        _uiManager.OnUseHpPotion = () => _usePotion("hp_potion");
        _uiManager.OnUseMpPotion = () => _usePotion("mp_potion");
    }

    void WireInventoryCallbacks()
    {
        var inv = _uiManager?.Inventory;
        if (inv == null) return;

        inv.OnEquipCallback = slotIdx =>
        {
            var item = _inventory.GetSlot(slotIdx);
            if (item == null || !_data.Items.TryGetValue(item.itemId, out var def)) return;
            string slot = ItemTypeUtil.GetEquipSlot(def.TypeEnum);
            if (string.IsNullOrEmpty(slot)) return;
            if (_playerState.Equipment.TryGetValue(slot, out var old) && old != null)
                _inventory.AddItem(old.itemId, 1, false, 1);
            _playerState.Equipment[slot] = item;
            _inventory.RemoveAtSlot(slotIdx);
            _playerState.RecalcStats(_data.Items, _data.SetBonuses);
            _player.SetSpeed(_playerState.CurrentStats.spd);
            EventBus.Emit(new EquipChangeEvent());
            AudioManager.Instance?.PlaySFX("sfx_confirm");
            inv.Refresh();
        };

        inv.OnUnequipCallback = slot =>
        {
            if (!_playerState.Equipment.TryGetValue(slot, out var item) || item == null) return;
            bool stackable = _data.Items.TryGetValue(item.itemId, out var def) && def.stackable;
            _inventory.AddItem(item.itemId, 1, stackable, def?.maxStack ?? 1);
            _playerState.Equipment[slot] = null;
            _playerState.RecalcStats(_data.Items, _data.SetBonuses);
            _player.SetSpeed(_playerState.CurrentStats.spd);
            EventBus.Emit(new EquipChangeEvent());
            AudioManager.Instance?.PlaySFX("sfx_confirm");
            inv.Refresh();
        };

        inv.OnUseItemCallback = slotIdx =>
        {
            var item = _inventory.GetSlot(slotIdx);
            if (item == null || !_data.Items.TryGetValue(item.itemId, out var def)) return;
            if (def.healHp > 0 || def.healMp > 0)
            {
                _usePotion(item.itemId);
                _inventory.RemoveAtSlot(slotIdx);
                inv.Refresh();
            }
        };

        inv.OnSortCallback = () =>
        {
            _inventory.SortItems(_data.Items);
            inv.Refresh();
        };
    }

    void WireSkillTreeCallbacks()
    {
        var st = _uiManager?.SkillTree;
        if (st == null) return;

        st.OnLearnSkill = skillId =>
        {
            var result = _skills.LearnSkill(skillId, _playerState.SkillPoints, _playerState.Level);
            if (result.learned)
            {
                _playerState.SkillPoints = result.remainingPoints;
                AudioManager.Instance?.PlaySFX("sfx_upgrade");
                st.Refresh();
                _uiManager.Hud?.UpdateLevel(_playerState.Level, _playerState.SkillPoints, _playerState.StatPoints);
            }
            else
            {
                AudioManager.Instance?.PlaySFX("sfx_error");
            }
        };

        st.OnEquipSkill = (skillId, slot) =>
        {
            _skills.EquipSkill(skillId, slot);
            AudioManager.Instance?.PlaySFX("sfx_confirm");
            _uiManager.Hud?.UpdateSkillBar(_skills.GetEquippedSkills(), new float[GameConfig.SkillSlotCount]);
            st.Refresh();
        };
    }

    void WireDialogueCallbacks()
    {
        var dlg = _uiManager?.Dialogue;
        if (dlg == null) return;

        dlg.OnClose = () =>
        {
            _uiManager.SetDialogueOpen(false);
            _player.Frozen = false;
        };

        dlg.OnAcceptQuest = questId =>
        {
            _quests.AcceptQuest(questId);
            _uiManager.Hud?.AddHistoryEntry($"Quest accepted: {questId}", Color.cyan);
        };

        dlg.OnCompleteQuest = questId =>
        {
            var reward = _quests.CompleteQuest(questId, _inventory);
            if (reward == null) return;

            if (reward.gold > 0)
            {
                _playerState.Gold += reward.gold;
                EventBus.Emit(new GoldChangeEvent { gold = _playerState.Gold });
            }
            if (reward.xp > 0)
            {
                var state = new PlayerLevelState
                {
                    level = _playerState.Level, xp = _playerState.Xp,
                    skillPoints = _playerState.SkillPoints, statPoints = _playerState.StatPoints
                };
                StatsSystem.AddXp(ref state, reward.xp);
                _playerState.Level = state.level; _playerState.Xp = state.xp;
                _playerState.SkillPoints = state.skillPoints; _playerState.StatPoints = state.statPoints;
                _playerState.RecalcStats(_data.Items, _data.SetBonuses);
                _player.SetSpeed(_playerState.CurrentStats.spd);
            }
            if (reward.items != null)
            {
                foreach (var ri in reward.items)
                {
                    bool stackable = _data.Items.TryGetValue(ri.itemId, out var def) && def.stackable;
                    _inventory.AddItem(ri.itemId, ri.count, stackable, def?.maxStack ?? 1);
                }
            }
            _uiManager.Hud?.AddHistoryEntry(
                $"Quest complete! +{reward.gold}G +{reward.xp}XP", Color.yellow);
            AudioManager.Instance?.PlaySFX("sfx_quest_complete");
        };
    }

    void WirePauseMenuCallbacks()
    {
        var pm = _uiManager?.PauseMenu;
        if (pm == null) return;
        pm.OnSaveRequested = () => EventBus.Emit(new SaveEvent());
        pm.OnMainMenuRequested = () => SceneManager.LoadScene("MainMenuScene");
    }

    void WireAudio(Action playRegionBGM)
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
        EventBus.On<RegionVisitEvent>(_ => playRegionBGM());
    }

    void SubscribeEvents(MonoBehaviour host, RegionTracker regionTracker,
        Func<float> getLastAutoSaveTime, Action<float> setLastAutoSaveTime, Action playRegionBGM)
    {
        var hud = _uiManager != null ? _uiManager.Hud : null;

        EventBus.On<LevelUpEvent>(e =>
        {
            ScreenFlash.LevelUp();
            CameraShake.Shake(host, 200f, 0.15f);
        });

        EventBus.On<EquipChangeEvent>(e =>
        {
            _playerState.RecalcStats(_data.Items, _data.SetBonuses);
            _player.SetSpeed(_playerState.CurrentStats.spd);
        });

        EventBus.On<GoldChangeEvent>(e =>
        {
            if (hud != null) hud.UpdateGold(e.gold);
        });

        EventBus.On<RegionVisitEvent>(e =>
        {
            if (hud != null) hud.UpdateRegion(e.regionName);

            float now = Time.time;
            if (now - getLastAutoSaveTime() < 30f) return;
            setLastAutoSaveTime(now);
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
            _saveGame();
            if (hud != null) hud.ShowSaveIndicator();
        });
    }

    public void PushInitialState()
    {
        if (_uiManager == null || _uiManager.Hud == null) return;
        var hud = _uiManager.Hud;
        var s = _playerState.CurrentStats;
        hud.UpdateBars(_playerState.Hp, s.maxHp, _playerState.Mp, s.maxMp);
        hud.UpdateGold(_playerState.Gold);
        hud.UpdateLevel(_playerState.Level, _playerState.SkillPoints, _playerState.StatPoints);
        hud.UpdateXpBar(_playerState.Xp, GameConfig.XpForLevel(_playerState.Level));
        hud.UpdateSkillBar(_skills.GetEquippedSkills(), new float[GameConfig.SkillSlotCount]);
    }
}
