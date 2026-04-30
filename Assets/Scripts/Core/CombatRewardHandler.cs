using System.Collections.Generic;
using UnityEngine;

public class CombatRewardHandler
{
    readonly PlayerController _player;
    readonly PlayerStats _playerState;
    readonly InventorySystem _inventory;
    readonly DataManager _data;
    readonly UIManager _uiManager;
    readonly CombatManager _combatManager;
    readonly MonsterSpawner _monsterSpawner;
    readonly MonoBehaviour _host;
    readonly Dictionary<string, int> _killCounts;

    int _totalKills;
    public int TotalKills => _totalKills;

    public CombatRewardHandler(MonoBehaviour host, PlayerController player, PlayerStats playerState,
        InventorySystem inventory, DataManager data, UIManager uiManager,
        CombatManager combatManager, MonsterSpawner monsterSpawner,
        Dictionary<string, int> killCounts)
    {
        _host = host;
        _player = player;
        _playerState = playerState;
        _inventory = inventory;
        _data = data;
        _uiManager = uiManager;
        _combatManager = combatManager;
        _monsterSpawner = monsterSpawner;
        _killCounts = killCounts;
    }

    public void SetTotalKills(int value) => _totalKills = value;

    public void OnMonsterKilled(MonsterController monster)
    {
        if (monster.DeathProcessed) return;
        monster.DeathProcessed = true;
        SkillVFX.ShowAtPosition(_host, "vfx_monster_death", monster.Position.x, monster.Position.y);

        var def = monster.Def;
        _killCounts.TryGetValue(def.id, out int count);
        _killCounts[def.id] = ++count;
        _totalKills++;

        int prevLevel = _playerState.Level;
        var state = new PlayerLevelState
        {
            level = _playerState.Level, xp = _playerState.Xp,
            skillPoints = _playerState.SkillPoints, statPoints = _playerState.StatPoints
        };
        StatsSystem.AddXp(ref state, def.xp);
        _playerState.Level = state.level;
        _playerState.Xp = state.xp;
        _playerState.SkillPoints = state.skillPoints;
        _playerState.StatPoints = state.statPoints;

        var hud = _uiManager != null ? _uiManager.Hud : null;
        hud?.UpdateXpBar(_playerState.Xp, GameConfig.XpForLevel(_playerState.Level));

        if (_playerState.Level > prevLevel)
        {
            _playerState.RecalcStats(_data.Items, _data.SetBonuses);
            _playerState.FullHeal();
            _player.SetSpeed(_playerState.CurrentStats.spd);
            if (hud != null)
            {
                hud.UpdateLevel(_playerState.Level, _playerState.SkillPoints, _playerState.StatPoints);
                hud.UpdateXpBar(_playerState.Xp, GameConfig.XpForLevel(_playerState.Level));
                hud.AddHistoryEntry($"Level Up! Lv.{_playerState.Level}", new Color(1f, 0.867f, 0.267f)); // #ffdd44
            }
        }

        var worldEvents = GameManager.Instance?.WorldEvents;
        float goldMult = worldEvents?.GlobalGoldMultiplier ?? 1f;
        float dropMult = worldEvents?.GlobalDropMultiplier ?? 1f;

        int actualGold = Mathf.RoundToInt(def.gold * goldMult);
        _playerState.Gold += actualGold;
        EventBus.Emit(new GoldChangeEvent { gold = _playerState.Gold });

        if (actualGold > 0)
        {
            string coinSfx = def.rank switch
            {
                "boss"  => "sfx_coin_burst",
                "elite" => "sfx_coin_pile",
                _       => "sfx_coin_small",
            };
            AudioManager.Instance?.PlaySFX(coinSfx);
        }

        if (_combatManager != null)
        {
            Vector2 textPos = monster.Position + Vector2.up * 1.2f;
            if (def.xp > 0)
                _combatManager.ShowFloatingText(textPos, $"+{def.xp} XP", new Color(0.533f, 0.867f, 1f)); // #88ddff
            if (actualGold > 0)
            {
                string goldLabel = goldMult > 1f ? $"+{actualGold}G ×{goldMult:0.#}" : $"+{actualGold}G";
                _combatManager.ShowFloatingText(textPos + Vector2.up * 0.4f, goldLabel, new Color(1f, 0.851f, 0f)); // #ffd900
            }
        }

        var drops = LootSystem.RollDrops(def.drops, null, dropMult);
        if (drops.Count > 0)
        {
            AudioManager.Instance?.PlaySFX("sfx_item_acquire");
            // S-118: duck BGM so the acquire SFX is not buried.
            AudioManager.Instance?.DuckBGM(-6f, 0.4f);
        }
        float itemOffset = 0.8f;
        foreach (var drop in drops)
        {
            bool stackable = _data.Items.TryGetValue(drop.itemId, out var itemDef) && itemDef.stackable;
            int maxStack = itemDef?.maxStack ?? 1;
            int overflow = _inventory.AddItem(drop.itemId, drop.count, stackable, maxStack);

            string itemName = itemDef?.name ?? drop.itemId;
            if (overflow > 0)
                Debug.LogWarning($"[CombatRewardHandler] Inventory full: {overflow}x {itemName} lost");
            if (_combatManager != null)
                _combatManager.ShowFloatingText(
                    monster.Position + Vector2.up * itemOffset,
                    overflow > 0 ? $"+{itemName} x{drop.count - overflow} (FULL)" : $"+{itemName} x{drop.count}",
                    overflow > 0 ? new Color(1f, 0.6f, 0.267f) : new Color(0.4f, 1f, 0.533f)); // #ff9944 / #66ff88
            itemOffset += 0.4f;
        }

        EventBus.Emit(new MonsterKillEvent
        {
            monsterId = def.id, monsterName = def.name,
            killCount = count, totalKills = _totalKills
        });

        _monsterSpawner.RemoveMonster(monster);
    }

    public void OnPlayerDeath()
    {
        int goldLoss = Mathf.FloorToInt(_playerState.Gold * GameConfig.DeathGoldPenalty);
        _playerState.Gold -= goldLoss;
        _playerState.FullHeal();
        EventBus.Emit(new PlayerDeathEvent { deathX = _player.Position.x, deathY = _player.Position.y });
        EventBus.Emit(new GoldChangeEvent { gold = _playerState.Gold });
    }
}
