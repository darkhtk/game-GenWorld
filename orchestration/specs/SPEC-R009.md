# SPEC-R009: 스킬 콤보 시스템

## 목적
특정 스킬을 연속으로 사용하면 보너스 효과가 발동되는 콤보 시스템을 추가한다.

## 현재 상태
- `SkillExecutor.cs` — behavior별 핸들러 등록 (self_buff, aoe_damage, single_target 등).
- `SkillSystem.cs` — 스킬 장착, 쿨다운, 레벨 관리.
- `ActionRunner.cs` — 스킬 actions 배열 순차 실행.
- **문제:** 스킬 간 연계 보너스 없음. 어떤 순서로 써도 동일 효과.

## 구현 명세

### 새 파일
- `Assets/Scripts/Systems/ComboSystem.cs` (순수 C# 클래스)

### 수정 파일
- `Assets/Scripts/Systems/CombatManager.cs` — ExecuteSkill에서 콤보 체크 호출

### 데이터 구조

1. **ComboSystem.cs:**
   ```csharp
   public class ComboSystem
   {
       public struct ComboEntry
       {
           public string[] sequence;    // ["slash", "thrust"] 순서
           public string bonusType;     // "damage_mult", "aoe_expand", "stun", "heal"
           public float bonusValue;     // 1.5 (50% 추가), 2.0 (범위 2배) 등
           public string name;          // "Blade Fury" (UI 표시용)
       }
       
       readonly List<ComboEntry> _combos;
       readonly Queue<string> _recentSkills = new(); // 최근 사용 스킬 ID
       const int MAX_HISTORY = 5;
       const float COMBO_WINDOW = 3f; // 3초 이내 연속 사용
       
       float _lastSkillTime;
       
       public ComboResult CheckCombo(string skillId, float now);
       public void RecordSkill(string skillId, float now);
   }
   ```

2. **ComboResult:**
   ```csharp
   public struct ComboResult
   {
       public bool triggered;
       public string comboName;
       public string bonusType;
       public float bonusValue;
   }
   ```

3. **콤보 정의 (하드코딩 → 추후 JSON):**
   ```csharp
   // 초기 콤보 3개
   new ComboEntry { sequence = new[]{"slash", "thrust"}, 
                     bonusType = "damage_mult", bonusValue = 1.5f, 
                     name = "Blade Fury" },
   new ComboEntry { sequence = new[]{"fireball", "ice_bolt"}, 
                     bonusType = "aoe_expand", bonusValue = 2f, 
                     name = "Elemental Burst" },
   new ComboEntry { sequence = new[]{"heal", "mana_shield"}, 
                     bonusType = "duration_extend", bonusValue = 1.5f, 
                     name = "Arcane Fortify" },
   ```

### 로직

1. **CombatManager.ExecuteSkill() 수정:**
   ```csharp
   // 스킬 실행 직전:
   _comboSystem.RecordSkill(skill.id, nowMs);
   var combo = _comboSystem.CheckCombo(skill.id, nowMs);
   if (combo.triggered)
   {
       ApplyComboBonus(combo, ref dmgMult, ref aoeBonus, ...);
       // HUD에 콤보 이름 표시
       EventBus.Emit(new ComboEvent { name = combo.comboName });
   }
   ```

2. **콤보 판정:**
   - `_recentSkills` 큐에서 최근 N개와 콤보 시퀀스 비교.
   - 시간 윈도우(3초) 초과 시 큐 초기화.
   - 매칭 시 큐 클리어 (같은 콤보 연속 방지).

3. **UI 피드백:**
   - `ComboEvent`를 HUD에서 구독 → 화면 중앙에 콤보 이름 1.5초 표시.

### 수치
| 파라미터 | 값 | 비고 |
|---------|-----|------|
| 콤보 윈도우 | 3초 | 스킬 간 최대 간격 |
| 히스토리 크기 | 5 | 최근 스킬 기록 |
| Blade Fury 보너스 | 데미지 x1.5 | slash → thrust |
| Elemental Burst 보너스 | AoE x2.0 | fireball → ice_bolt |
| Arcane Fortify 보너스 | 지속시간 x1.5 | heal → mana_shield |

### 세이브 연동
없음. 콤보 히스토리는 런타임 전용 (전투 세션 동안만 유지).

## 호출 진입점
- `CombatManager.ExecuteSkill()` 내부에서 자동 체크.
- UI: HUD에 콤보 텍스트 표시 (EventBus 구독).

## 테스트 항목
- [ ] 올바른 순서로 스킬 사용 시 콤보 발동
- [ ] 잘못된 순서에서는 콤보 미발동
- [ ] 3초 초과 시 콤보 윈도우 만료
- [ ] 콤보 보너스가 데미지/범위/지속시간에 정확히 적용되는지
- [ ] 콤보 발동 후 히스토리 초기화 (중복 방지)
- [ ] ComboEvent가 정상 발행되는지
