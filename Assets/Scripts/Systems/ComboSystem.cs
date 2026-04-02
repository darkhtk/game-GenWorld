using System.Collections.Generic;

public struct ComboResult
{
    public bool triggered;
    public string comboName;
    public string bonusType;
    public float bonusValue;
}

public class ComboSystem
{
    public struct ComboEntry
    {
        public string[] sequence;
        public string bonusType;
        public float bonusValue;
        public string name;
    }

    readonly List<ComboEntry> _combos = new();
    readonly List<(string skillId, float time)> _history = new();
    const int MaxHistory = 5;
    const float ComboWindow = 3000f; // ms

    public ComboSystem()
    {
        _combos.Add(new ComboEntry
        {
            sequence = new[] { "slash", "thrust" },
            bonusType = "damage_mult", bonusValue = 1.5f,
            name = "Blade Fury"
        });
        _combos.Add(new ComboEntry
        {
            sequence = new[] { "fireball", "ice_bolt" },
            bonusType = "aoe_expand", bonusValue = 2f,
            name = "Elemental Burst"
        });
        _combos.Add(new ComboEntry
        {
            sequence = new[] { "heal", "mana_shield" },
            bonusType = "duration_extend", bonusValue = 1.5f,
            name = "Arcane Fortify"
        });
    }

    public void RecordSkill(string skillId, float nowMs)
    {
        // Clear expired entries
        while (_history.Count > 0 && nowMs - _history[0].time > ComboWindow)
            _history.RemoveAt(0);

        _history.Add((skillId, nowMs));

        while (_history.Count > MaxHistory)
            _history.RemoveAt(0);
    }

    public ComboResult CheckCombo(string skillId, float nowMs)
    {
        foreach (var combo in _combos)
        {
            if (combo.sequence == null || combo.sequence.Length < 2) continue;
            if (combo.sequence[combo.sequence.Length - 1] != skillId) continue;

            if (MatchSequence(combo.sequence, nowMs))
            {
                _history.Clear();
                return new ComboResult
                {
                    triggered = true,
                    comboName = combo.name,
                    bonusType = combo.bonusType,
                    bonusValue = combo.bonusValue
                };
            }
        }
        return default;
    }

    bool MatchSequence(string[] sequence, float nowMs)
    {
        if (_history.Count < sequence.Length) return false;

        int historyStart = _history.Count - sequence.Length;
        for (int i = 0; i < sequence.Length; i++)
        {
            var entry = _history[historyStart + i];
            if (entry.skillId != sequence[i]) return false;
            if (nowMs - entry.time > ComboWindow) return false;
        }
        return true;
    }
}
