using System.Collections.Generic;

public class SkillSystem
{
    static readonly SkillScaling DefaultScaling = new()
        { damage = 0.2f, aoe = 0.1f, duration = 0.15f, buff = 0.1f };

    readonly Dictionary<string, SkillDef> _defs;
    readonly Dictionary<string, int> _learned = new();
    readonly Dictionary<string, float> _cooldowns = new();
    readonly string[] _equipped;
    readonly float[] _cooldownBuffer;

    public SkillSystem(Dictionary<string, SkillDef> defs)
    {
        _defs = defs;
        _equipped = new string[GameConfig.SkillSlotCount];
        _cooldownBuffer = new float[GameConfig.SkillSlotCount];
    }

    public int GetSkillLevel(string id) =>
        _learned.TryGetValue(id, out int lv) ? lv : 0;

    public bool IsLearned(string id) => _learned.ContainsKey(id);

    public LearnResult LearnSkill(string skillId, int availablePoints, int playerLevel)
    {
        if (!_defs.TryGetValue(skillId, out var def))
            return new LearnResult { learned = false, remainingPoints = availablePoints };

        int currentLevel = GetSkillLevel(skillId);
        if (currentLevel >= GameConfig.SkillMaxLevel)
            return new LearnResult { learned = false, remainingPoints = availablePoints };

        if (currentLevel == 0)
        {
            if (availablePoints < def.requiredPoints || playerLevel < def.requiredLevel)
                return new LearnResult { learned = false, remainingPoints = availablePoints };
            _learned[skillId] = 1;
            return new LearnResult { learned = true, remainingPoints = availablePoints - def.requiredPoints };
        }
        else
        {
            if (availablePoints < 1)
                return new LearnResult { learned = false, remainingPoints = availablePoints };
            _learned[skillId] = currentLevel + 1;
            return new LearnResult { learned = true, remainingPoints = availablePoints - 1 };
        }
    }

    public int ResetAllSkills()
    {
        int refund = 0;
        foreach (var kvp in _learned)
        {
            if (_defs.TryGetValue(kvp.Key, out var def))
                refund += def.requiredPoints + (kvp.Value - 1);
        }
        _learned.Clear();
        for (int i = 0; i < _equipped.Length; i++) _equipped[i] = null;
        return refund;
    }

    public float GetDamageMultiplier(string id)
    {
        if (!_defs.TryGetValue(id, out var def)) return 1f;
        int lv = GetSkillLevel(id);
        if (lv <= 0) return def.damage;
        var sc = def.scaling ?? DefaultScaling;
        return def.damage + (lv - 1) * sc.damage;
    }

    public float GetAoeBonus(string id)
    {
        int lv = GetSkillLevel(id);
        var sc = _defs.TryGetValue(id, out var def) ? (def.scaling ?? DefaultScaling) : DefaultScaling;
        return 1f + System.Math.Max(0, lv - 1) * sc.aoe;
    }

    public float GetDurationBonus(string id)
    {
        int lv = GetSkillLevel(id);
        var sc = _defs.TryGetValue(id, out var def) ? (def.scaling ?? DefaultScaling) : DefaultScaling;
        return 1f + System.Math.Max(0, lv - 1) * sc.duration;
    }

    public float GetBuffBonus(string id)
    {
        int lv = GetSkillLevel(id);
        var sc = _defs.TryGetValue(id, out var def) ? (def.scaling ?? DefaultScaling) : DefaultScaling;
        return 1f + System.Math.Max(0, lv - 1) * sc.buff;
    }

    public bool EquipSkill(string skillId, int slot)
    {
        if (slot < 0 || slot >= _equipped.Length) return false;
        if (!IsLearned(skillId)) return false;
        _equipped[slot] = skillId;
        return true;
    }

    public string GetEquippedSkill(int slot) =>
        slot >= 0 && slot < _equipped.Length ? _equipped[slot] : null;

    public string[] GetEquippedSkills() => (string[])_equipped.Clone();

    public UseSkillResult UseSkill(int slot, int currentMp, float now)
    {
        if (slot < 0 || slot >= _equipped.Length || _equipped[slot] == null)
            return new UseSkillResult { success = false, reason = "not_equipped" };

        string id = _equipped[slot];
        if (!_defs.TryGetValue(id, out var def))
            return new UseSkillResult { success = false, reason = "no_skill" };

        if (_cooldowns.TryGetValue(id, out float readyAt) && now < readyAt)
            return new UseSkillResult { success = false, reason = "cooldown" };

        if (currentMp < def.mpCost)
            return new UseSkillResult { success = false, reason = "no_mp" };

        _cooldowns[id] = now + def.cooldown;
        return new UseSkillResult
        {
            success = true,
            skill = def,
            skillLevel = GetSkillLevel(id),
            mpCost = def.mpCost
        };
    }

    public float GetCooldownRemaining(string skillId, float now)
    {
        if (!_cooldowns.TryGetValue(skillId, out float readyAt)) return 0f;
        return System.Math.Max(0f, readyAt - now);
    }

    public float[] GetCooldowns(float now)
    {
        for (int i = 0; i < _cooldownBuffer.Length; i++)
        {
            _cooldownBuffer[i] = 0f;
            if (_equipped[i] == null) continue;
            if (!_defs.TryGetValue(_equipped[i], out var def)) continue;
            float remaining = GetCooldownRemaining(_equipped[i], now);
            _cooldownBuffer[i] = def.cooldown > 0 ? remaining / def.cooldown : 0f;
        }
        return _cooldownBuffer;
    }

    public Dictionary<string, int> GetLearnedSkills() => new(_learned);

    public void RestoreLearnedSkills(Dictionary<string, int> data)
    {
        _learned.Clear();
        foreach (var kvp in data) _learned[kvp.Key] = kvp.Value;
    }

    public void RestoreEquipped(string[] data)
    {
        for (int i = 0; i < _equipped.Length && i < data.Length; i++)
            _equipped[i] = data[i];
    }
}
