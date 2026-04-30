# SPEC-S-101: 회피 기능 수행 시 몬스터 리셋 버그 수정

> **작성:** 2026-04-30 (Coordinator)
> **출처:** REVIEW-S-101-v1.md, REVIEW-S-101-v2.md (BLOCKER 종합)
> **우선순위:** high
> **상태:** Rejected v2 → v3 재작업 대기
> **호출 진입점:** PlayerController 회피 입력 (스페이스 / 마우스 우클릭) → DodgeController.OnDodgeStart → 시스템 전반 (CombatManager, MonsterController) 영향

---

## 1. 문제 정의

플레이어 회피 직후 몬스터가 다음 조건에서 비정상 동작:
1. 몬스터가 추적을 중단하고 Spawn 지점으로 Return — 플레이어가 도망친 것이 아님
2. Return 도착 시 HP가 만피로 회복 — 플레이어가 입힌 누적 데미지 무효
3. 위 두 동작이 회피 1회로 발생해, 보스/엘리트 전에서 진행 무효화

근본 원인:
- 회피 중 `Invincible == true` 상태에서 몬스터의 `LastHitByPlayerTime`이 갱신되지 않음
- `MonsterController.UpdateAI`의 Return 진입 조건(`ChaseRangeMult` 초과 OR `MaxSpawnDistance` 초과 AND `Time.time - LastHitByPlayerTime > RecentHitWindow`) 중 마지막 항만 만족시키면 Return으로 빠짐
- Return 도착 시 HP를 무조건 `Def.hp`로 세팅 (전체 회복)

---

## 2. 수치/상수 (단일 소스)

> v2 리뷰에서 BOARD 비고-실제 구현 불일치가 BLOCKER 사유 중 하나. v3에서는 **하나의 진실의 원천**을 정한다.

| 상수                              | 값        | 위치 (제안)               | 비고                                    |
| ------------------------------- | -------- | -------------------- | ------------------------------------- |
| `RecentHitWindow`               | **2.0s** | `GameConfig` 단일 상수    | 현재 코드값 유지. BOARD 비고란 5s 주장은 **오기로 정정** |
| `IsInCombat`                    | **2.0s** | `GameConfig` 단일 상수    | 현재 `CombatManager` 3f → 2f로 통합          |
| `DodgeAggroSyncRangeMult`       | **1.3** | `CombatManager`     | attackRange × 1.3 내 몬스터 LastHitByPlayerTime 갱신 |
| HP Return 회복 정책                  | **전체회복 유지** | `MonsterController`  | 단, **데미지를 입은 적 없는 몬스터만**: `if (Hp < Def.hp) Hp = Def.hp;` 가드 (이미 v2에 적용됨, 유지) |

> ※ BOARD 비고란의 "HP recover floor 50%"는 **요구사항이 아닌 오기로 결정**. 현재 가드 로직(만피 미만일 때만 만피로 복원)이 의도. v3 작업 시 BOARD 비고란 정정 의무.

---

## 3. 연동 경로 (변경 범위)

### 3-1. `MonsterController.cs`
1. **AIState 테스트 가능성 확보** (BLOCKER 1차 해결)
   - 옵션 A: `AIState`를 `public ... { get; internal set; }`로 변경 + `Tests.EditMode` 어셈블리에 `[InternalsVisibleTo]` 부여 (`Assembly-CSharp`의 AssemblyInfo.cs 또는 asmdef 권한)
   - 옵션 B (보다 안전): `AIState` private set 유지 + `internal void SetAIStateForTest(MonsterAIState s)` 메서드 신설 + `[InternalsVisibleTo("Tests.EditMode")]`
   - **권장: 옵션 B** — 프로덕션 setter 노출을 막는다.
2. Return 진입 가드 (현행 유지)
   - `Time.time - LastHitByPlayerTime > RecentHitWindow` 체크
   - HP 가드: `if (Hp < Def.hp) Hp = Def.hp;` (force-teleport 분기 line 159, proximity 분기 line 168)
3. `RecentHitWindow` 상수를 `GameConfig.MonsterAggro.RecentHitWindow`로 이동(또는 참조). 매직 넘버 제거.

### 3-2. `CombatManager.cs`
1. **회피 중 패턴 캔슬 회귀 수정** (BLOCKER 3차 해결)
   - 현행: `_player.Invincible || _player.IsDodging` 시 `LastHitByPlayerTime` 갱신 후 **early return** → 모든 몬스터 공격 흐름 정지
   - 변경: 회피 중에도 `TryGetAttackToExecute` / `ConsumePhaseEnterActions` / `HandleMonsterAttacks` 본체는 진행. 단 `ApplyDamageToPlayer`만 무적으로 차단(이미 다른 경로에서 차단됨).
   - 구체 패치:
     ```csharp
     foreach (var m in _monsters) {
         if (m == null || m.Def == null) continue;
         if (_player.Invincible || _player.IsDodging) {
             if (Vector2.Distance(_player.Position, m.transform.position) < m.Def.attackRange * DodgeAggroSyncRangeMult) {
                 m.LastHitByPlayerTime = Time.time;  // aggro만 유지
             }
             // early return 제거 — 패턴 진행은 계속
         }
         // 이후 기존 attack 로직 그대로
     }
     ```
2. 원거리 투사체 `OnArrive` 콜백 (현행 유지)
   - dodge invincible 체크 **이전**에 `LastHitByPlayerTime` 갱신 → 정확. 변경 없음.
3. `m.Def.attackRange × 1.3` 접근 시 `m.Def == null` null-safety 가드 추가 (마이너 이슈 5).

### 3-3. `GameConfig` (신규 또는 기존 확장)
- `static class GameConfig.MonsterAggro { public const float RecentHitWindow = 2f; public const float IsInCombatWindow = 2f; public const float DodgeAggroSyncRangeMult = 1.3f; }`
- `MonsterController.RecentHitWindow`, `CombatManager.IsInCombatWindow` 두 곳이 같은 상수를 참조하도록 통합.

### 3-4. `Tests/EditMode/DodgeMonsterResetBugTest.cs`
1. **컴파일 BLOCKER 수정**
   - line 65, 83의 `_monster.AIState = MonsterAIState.Return;` → `_monster.SetAIStateForTest(MonsterAIState.Return);` (또는 internal set 활용)
2. **Setup 보강**
   - `MonsterDef`의 `name`, `sprite` (32x32 placeholder), `animationDef` 필수 필드 채우기
   - `PlayerController.Init(playerStats)` 호출 추가 — 의존 컴포넌트 NRE 방지
3. **Test 3 reflection 재작성**
   - `IsDodging`이 프로퍼티이므로 `GetField` 대신 `GetProperty(...).SetValue(...)` 사용
   - 또는 `PlayerController.SetDodgeStateForTest(bool)` API 추가 후 직접 호출 (권장)
4. **케이스 유지**
   - Test 1: 회피 중 몬스터 Return 진입 차단
   - Test 2: Return 도착 시 데미지 입은 적 없는 몬스터는 HP 변화 없음
   - Test 3: 회피 중 attack range × 1.3 내 몬스터 `LastHitByPlayerTime` 갱신

---

## 4. 데이터 구조 / 세이브 연동
- 본 태스크는 **데이터/세이브 구조 변경 없음**. 모든 상수는 코드 상수(`GameConfig`).
- `MonsterController.AIState` 직렬화 정책 변경 없음 (런타임 상태, 세이브 비대상).

---

## 5. UI 와이어프레임
- **UI 변경 없음.** 본 태스크는 백엔드 AI 상태 머신 + 컴뱃 가드 수정.
- HP 바: 기존 `_hpBar.UpdateHP` 호출 유지 — Return 회복이 발생하지 않으면 HP 바 갱신도 발생 X (정상).

---

## 6. 호출 진입점 (시나리오)

| 진입점                                    | 트리거                  | 영향 경로                                                       |
| -------------------------------------- | -------------------- | ------------------------------------------------------------ |
| `PlayerController.Update()` 회피 입력      | Space / 마우스 우클릭      | → `DodgeController.OnDodgeStart` → `_player.IsDodging = true`, `Invincible = true` |
| `CombatManager.HandleMonsterAttacks()` | 매 프레임 폴링             | 회피 중: aggro만 유지, 패턴 진행 / 회피 외: 정상 |
| `MonsterController.UpdateAI()`          | 매 프레임 폴링             | `Time.time - LastHitByPlayerTime > RecentHitWindow` 시에만 Return 허용 |
| `Projectile.OnArrive()`                 | 투사체 도달 콜백           | dodge 무적 체크 전에 `LastHitByPlayerTime` 갱신 (현행 유지)               |

---

## 7. 수용 기준 (Acceptance)

- [ ] EditMode 테스트 어셈블리가 컴파일 성공 (CS0272 제거)
- [ ] DodgeMonsterResetBugTest 3건 모두 PASS (실제 dodge 상태 + AIState 조작 가능)
- [ ] BOARD 비고란이 실제 구현값과 일치 (`RecentHitWindow=2s`, HP 가드 정책 명시)
- [ ] 회피 중 보스 windup 패턴이 1프레임 회피로 캔슬되지 않음 (수동 검증 + 가능하면 PlayMode 테스트)
- [ ] `RecentHitWindow` / `IsInCombatWindow` 가 `GameConfig` 단일 상수에서만 정의됨 (grep으로 매직 넘버 부재 확인)
- [ ] `m.Def == null` null-safety 가드가 회피 분기에 존재
- [ ] Unity Editor 콘솔 `error CS` 0건

---

## 8. v3 제출 체크리스트 (개발자용)

1. `GameConfig.MonsterAggro` 상수 도입
2. `MonsterController.SetAIStateForTest` (internal) 또는 internal setter + InternalsVisibleTo
3. `CombatManager.HandleMonsterAttacks` 회피 분기에서 early return 제거, aggro 갱신만 유지
4. `m.Def` null-safety 가드 추가
5. 테스트 setup 보강: `PlayerController.Init`, `MonsterDef` 필수 필드, reflection → API 호출
6. 로컬에서 EditMode test 빌드/실행 PASS 확인
7. BOARD 비고란을 실제 구현으로 정정한 후 v3 제출
