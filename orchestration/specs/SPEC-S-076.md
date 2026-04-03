# SPEC-S-076: CombatManager Concurrent Attack Sequential Processing

## 목표
동일 프레임에서 복수의 공격 요청(auto-attack, skill, monster attack)이 동시에 발생할 때, 
큐잉/순차 처리를 통해 공격 순서의 결정론적 보장과 부작용 충돌(사망 후 중복 데미지 등)을 방지한다.

## 현재 상태
`CombatManager`는 `GameManager.Update()` 한 프레임 내에서 아래 순서로 호출된다:

1. `PerformAutoAttack(monsters)` — 마우스 좌클릭 시 arc 범위 내 몬스터에 즉시 피해
2. `HandleSkillInput()` → `ExecuteSkill(slot)` — 키 입력(1-6) 시 스킬 즉시 실행
3. `HandleMonsterAttacks(monsters, nowMs)` — 모든 몬스터의 플레이어 공격 처리

각 메서드는 **즉시 피해를 적용**하며, 대기열(queue)이 없다. 동일 프레임에서:
- 플레이어 auto-attack이 몬스터를 죽인 후, 같은 프레임의 skill이 이미 죽은 몬스터에 다시 피해를 입히려 시도할 수 있다.
  - `DealDamageToMonster()`의 `m.IsDead` 체크로 일부 방어되지만, `PerformAutoAttack` 내부 loop에서 killed 리스트 처리가 loop 종료 후에 발생하므로, 같은 loop 내 후속 iteration에서 동일 몬스터에 중복 피해가 가능하다.
- `HandleMonsterAttacks`에서 몬스터 A가 플레이어를 죽인 후, 같은 loop 내 몬스터 B가 다시 dead 플레이어를 공격하여 HP가 음수로 더 내려갈 수 있다.
  - `ApplyDamageToPlayer`는 HP <= 0 시 `_onPlayerDeath`를 호출하지만, HP가 이미 0 이하인지 재확인하지 않는다.
- 몬스터 death callback(`_onMonsterDeath`)이 `_pendingKills`로 지연 처리되는 것은 skill 실행 시에만 적용되고, auto-attack에서는 즉시 호출된다.

**핵심 문제 요약:**
1. Auto-attack loop 내 동일 몬스터 중복 피해 가능성
2. Player death 후 동일 프레임 추가 공격 미차단
3. Auto-attack과 Skill 간 kill callback 처리 방식 불일치

## 변경 사항

### C1: Player death guard 추가
`ApplyDamageToPlayer()` 진입부에 HP <= 0 early return 추가:

```csharp
void ApplyDamageToPlayer(int dmg)
{
    if (PlayerState == null || PlayerState.Hp <= 0) return;
    // ... existing logic
}
```

`HandleMonsterAttacks()` loop 내에도 player death 체크 추가:

```csharp
for (int i = monsters.Count - 1; i >= 0; i--)
{
    // ... existing checks ...
    if (PlayerState != null && PlayerState.Hp <= 0) break;  // NEW
    // ... rest of loop
}
```

### C2: Auto-attack killed 몬스터 즉시 skip
`PerformAutoAttack()` 내부 loop에서, 피해 적용 후 dead 판정된 몬스터를 `killed` 리스트에 추가하되, loop 자체는 이미 `m.IsDead` 체크가 있으므로 TakeDamage → IsDead 전환이 즉시 반영되는지 확인한다.

현재 `MonsterController.TakeDamage()`가 HP 감소 후 `IsDead`를 즉시 true로 설정하면 문제 없음. 단, auto-attack의 killed 처리를 `_pendingKills` 패턴으로 통일:

```csharp
// PerformAutoAttack 내부 — killed 즉시 콜백 대신 pendingKills 사용
_pendingKills.Clear();
// ... loop ...
if (dead) _pendingKills.Add(m);
// ... loop end ...
for (int i = 0; i < _pendingKills.Count; i++)
    _onMonsterDeath?.Invoke(_pendingKills[i]);
_pendingKills.Clear();
```

### C3: Frame-level combat phase flag (선택적)
`bool _combatProcessing` 플래그로 중첩 호출 방지. 현재 구조상 `GameManager.Update()`가 순차 호출하므로 re-entrancy 위험은 낮지만, callback 체인(`_onMonsterDeath` → 보상 → UI 갱신 → ...)에서 간접적으로 combat 메서드가 재호출될 가능성에 대비한다.

## 수치/상수

| 상수 | 값 | 비고 |
|------|-----|------|
| 추가 상수 없음 | — | 기존 `AutoAttackCooldown`(1000ms), 스킬 쿨다운은 변경 없음 |

## 연동 경로

| 파일 | 변경 |
|------|------|
| `Assets/Scripts/Systems/CombatManager.cs` | C1, C2 적용 |
| `Assets/Scripts/Core/GameManager.cs` | 변경 없음 (호출 순서 유지) |
| `Assets/Scripts/Entities/MonsterController.cs` | 읽기 전용 확인 — `TakeDamage()` 후 `IsDead` 즉시 반영 여부 |

## 호출 진입점

`GameManager.Update()` (line ~154-164):
```
PerformAutoAttack(monsters)  → 플레이어 auto-attack
HandleSkillInput()           → ExecuteSkill(slot)
HandleMonsterAttacks(...)    → 몬스터 → 플레이어 피해
```
세 메서드 모두 동일 프레임, 순차 실행. 콜백 체인에 의한 간접 재호출이 유일한 동시성 위험.

## 데이터 구조

기존 `_pendingKills` (`List<MonsterController>`) 재활용. 새로운 자료구조 추가 없음.

## 세이브 연동

영향 없음. 전투 처리는 런타임 전용이며 저장 대상이 아님.

## 검증 기준

- [ ] 동일 프레임에서 auto-attack과 skill이 같은 몬스터를 타겟해도 `_onMonsterDeath`가 1회만 호출됨
- [ ] 플레이어 HP가 0 이하로 떨어진 후 같은 프레임의 추가 몬스터 공격이 무시됨
- [ ] `_onPlayerDeath` callback이 프레임당 최대 1회 호출됨
- [ ] Auto-attack killed 처리가 `_pendingKills` 패턴으로 통일되어 loop 도중 리스트 수정 없음
- [ ] 기존 콤보 시스템, 스텔스 해제, 스킬 실행 동작에 영향 없음
- [ ] `MonsterController.TakeDamage()` → `IsDead == true` 즉시 반영 확인
