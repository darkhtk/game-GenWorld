using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public void Init(PlayerController player, System.Func<Stats> getStats, System.Func<int> getLevel, System.Action<MonsterController> onMonsterDeath, System.Action onPlayerDeath, EffectHolder playerEffects) { }
    public void PerformAutoAttack(List<MonsterController> monsters) { }
    public void HandleMonsterAttacks(List<MonsterController> monsters, float now) { }
    public void ExecuteSkill(int slot) { }
    public void ShowDamageNumber(Vector2 pos, int amount, bool isCrit, Color? color = null) { }
    public void ShowFloatingText(Vector2 pos, string msg, Color? color = null) { }
}
