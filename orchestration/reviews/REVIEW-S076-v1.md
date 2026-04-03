# REVIEW-S076-v1: CombatManager 동시 공격 순차 처리

> **리뷰 일시:** 2026-04-03
> **태스크:** S-076 CombatManager 동시 공격 순차 처리
> **스펙:** SPEC-S-076
> **커밋:** `bd123e4` fix: S-076 CombatManager player death guard + auto-attack pendingKills unification
> **판정:** ✅ APPROVE

---

## 변경 요약

커밋 `bd123e4`에서 CombatManager.cs 변경 (1개 파일, +8 -4 라인):

1. **PerformAutoAttack()** (line 92-123) — `killed` 로컬 리스트 → `_pendingKills` 필드로 통일. 루프 후 일괄 death 콜백 호출.
2. **HandleMonsterAttacks()** (line 139) — `PlayerState.Hp <= 0` 시 `break` 추가. 사망 후 추가 공격 차단.
3. **ApplyDamageToPlayer()** (line 187) — HP <= 0 early return 추가. 이중 사망 피해 방지.

---

## 검증 1: 엔진 검증

- `GameManager.Update()` → `PerformAutoAttack()` → `HandleSkillInput()` → `HandleMonsterAttacks()` 순차 호출 구조 유지
- Unity는 단일 스레드 Update이므로 동시성 문제 없음. 콜백 체인의 간접 재호출만 주의.

---

## 검증 2: 코드 추적

### 2.1 PerformAutoAttack pendingKills 통일 (line 92-123)

**변경 전:** `List<MonsterController> killed = null;` → `killed ??= new(); killed.Add(m);` → 루프 후 즉시 `_onMonsterDeath?.Invoke(m)`
**변경 후:** `_pendingKills ??= new(); _pendingKills.Clear();` → `if (dead) _pendingKills.Add(m);` → 루프 후 for 루프로 일괄 콜백

- 루프 내에서 리스트 수정 없음 (Add는 별도 리스트) → 안전 ✅
- `_pendingKills.Clear()` 호출이 루프 전/후 2회 → 깔끔한 상태 보장 ✅
- `_onMonsterDeath?.Invoke()` 호출이 루프 밖으로 이동 → 콜백 체인이 몬스터 리스트를 수정해도 루프에 영향 없음 ✅
- 기존 스킬 실행(`ExecuteSkill`)도 동일 `_pendingKills` 패턴 사용 (line 262-280) → 일관성 ✅

### 2.2 HandleMonsterAttacks player death guard (line 139)

```csharp
if (PlayerState != null && PlayerState.Hp <= 0) break;
```

- 루프 역순 (i = Count-1 → 0) 순회 중 HP 체크 → 몬스터 A가 플레이어를 죽이면 B는 공격하지 않음 ✅
- `break`이므로 루프만 탈출, 이후 코드(line 159-160의 추가 로직 등) 정상 실행
- `PlayerState != null` null 체크 선행으로 NullRef 방지 ✅

### 2.3 ApplyDamageToPlayer death guard (line 187)

```csharp
if (PlayerState == null || PlayerState.Hp <= 0) return;
```

- 기존: `PlayerState == null` 만 체크
- 변경: `Hp <= 0`도 추가 → 이미 사망한 상태에서 추가 피해 적용 차단 ✅
- 근거 투사체(`FireMonsterProjectile` → `OnArrive` 콜백)에서도 `ApplyDamageToPlayer`를 호출 (line 180) → 이 경로도 death guard 적용됨 ✅

### 2.4 `_onPlayerDeath` 이중 호출 방지 확인

`ApplyDamageToPlayer` 내에서 `Hp <= 0` 시 `_onPlayerDeath?.Invoke()` 호출 (line 204 부근). HP가 0 이하로 내려간 후 같은 프레임에 다시 `ApplyDamageToPlayer` 호출 시:
1. line 187의 `Hp <= 0` guard → 조기 반환 → `_onPlayerDeath` 재호출 없음 ✅

---

## 검증 3: UI 추적

- DamageText 표시는 `ApplyDamageToPlayer` 내부에서 피해 적용 시에만 발생
- death guard로 추가 피해가 차단되면 추가 DamageText도 미표시 → 일관적 ✅

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 판정 |
|---------|-----------|------|
| auto-attack으로 몬스터 3마리 동시 처치 | pendingKills에 3마리 → 루프 후 일괄 death 콜백 | **PASS** |
| 동일 프레임 auto-attack + skill 동일 몬스터 | m.IsDead 체크로 중복 피해 방지 | **PASS** |
| 몬스터 A가 플레이어 처치 → 몬스터 B 공격 | HP <= 0 break → B 공격 무시 | **PASS** |
| 원거리 몬스터 투사체 → 이미 사망한 플레이어 | ApplyDamageToPlayer HP <= 0 가드 | **PASS** |
| 정상 전투 (사망 없음) | 기존과 동일 동작 | **PASS** |
| 스텔스 해제 동작 | 변경 없음, pendingKills 이후 실행 | **PASS** |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
죽었는데 데미지가 계속 뜨는 현상이 사라졌다. 사망 화면 전환이 깔끔해진 느낌.

### ⚔️ 코어 게이머
동시 공격 처리가 결정론적으로 바뀐 건 좋다. 보스+잡몹 혼합 전투에서 플레이어 사망 시 이후 몬스터 공격이 HP를 더 깎아 부활 시 음수 HP가 되는 버그가 없어졌다. pendingKills 통일로 kill 콜백(경험치/아이템 드롭) 타이밍도 일관성이 생겼다.

### 🎨 UX/UI 디자이너
사망 후 추가 데미지 숫자가 표시되지 않으므로 사망 화면 전환 UX가 개선됨.

### 🔍 QA 엔지니어
3개 변경 지점이 모두 방어적 가드 추가/패턴 통일이므로 회귀 위험 낮음. `_onMonsterDeath` 콜백 체인에서 `PerformAutoAttack`이 재호출되는 간접 재진입은 현재 구조상 불가능하지만, 스펙이 제안한 `_combatProcessing` 플래그(C3)는 미구현 — 현 시점에서는 불필요하다고 판단하며 동의함.

---

## 종합 판정

### ✅ APPROVE

| # | 스펙 요구사항 | 대응 | 판정 |
|---|-------------|------|------|
| 1 | 동일 몬스터 _onMonsterDeath 1회 호출 | ✅ m.IsDead + pendingKills | **PASS** |
| 2 | 플레이어 사망 후 추가 공격 차단 | ✅ HP <= 0 break + early return | **PASS** |
| 3 | _onPlayerDeath 프레임당 1회 | ✅ ApplyDamageToPlayer guard | **PASS** |
| 4 | pendingKills 패턴 통일 | ✅ auto-attack도 _pendingKills 사용 | **PASS** |
| 5 | 기존 동작 영향 없음 | ✅ 가드 추가만, 로직 변경 없음 | **PASS** |

모든 스펙 요구사항 충족. 변경이 방어적 가드와 패턴 통일에 한정되어 회귀 위험 극히 낮음.
