# REVIEW-S-101-v2: 회피 기능 수행 시 몬스터 리셋 버그 수정 [깊은 리뷰]

**Task:** S-101
**리뷰 일시:** 2026-04-30
**대상 커밋:** 0643971 — fix: 회피 기능 수행 시 몬스터 리셋 버그 수정
**리뷰 모드:** [깊은 리뷰] — 코드 직접 분석 + 테스트 컴파일 가능성 검증
**SPEC 참조:** specs/SPEC-S-101.md 미존재 (비공식 수용기준 — BOARD 비고란만 존재)

---

## 변경 요약 (코드 직접 확인)

| 위치 | 변경 내용 |
|------|----------|
| `MonsterController.cs:154-176` | Return 상태 HP 복구 시 `if (Hp < Def.hp) Hp = Def.hp;` 가드 추가 (force-teleport / proximity 두 경로 모두) |
| `CombatManager.cs:156-173` | 플레이어 회피 중 `_player.Invincible || _player.IsDodging` 시 attack range × 1.3 내 몬스터 `LastHitByPlayerTime` 갱신 후 early return |
| `CombatManager.cs:232-235` | 원거리 투사체 `OnArrive` 콜백에서 dodge invincible 체크 **이전에** `LastHitByPlayerTime` 갱신 |
| `Tests/EditMode/DodgeMonsterResetBugTest.cs` | 신규 테스트 3건 추가 |

---

## 검증 결과

### 검증 1: 엔진 검증
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| MonsterAIState enum / Return 분기 존재 | ✅ | MonsterController.cs:13, 154 |
| CombatManager 회피 가드 분기 | ✅ | CombatManager.cs:156-173 |
| Projectile.OnArrive 콜백 경로 | ✅ | CombatManager.cs:225-240 |
| 신규 테스트 파일 어셈블리 배치 | ⚠️ | EditMode/ 위치는 정상이나 컴파일 가능성 의심 (검증 2 참조) |

### 검증 2: 코드 추적 ⚠️ 문제 발견
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| Return 상태 HP 가드 로직 | ✅ | 두 분기(force-teleport line 159, proximity line 168) 모두 `Hp < Def.hp` 가드 적용 |
| 회피 중 LastHitByPlayerTime 유지 | ✅ | CombatManager.cs:169 — attack range × 1.3 내 몬스터 갱신 |
| 원거리 공격 도착 시 engagement 추적 | ✅ | CombatManager.cs:233 — invincible 체크 전에 갱신 |
| **테스트 컴파일 가능성** | ❌ **BLOCKER** | `_monster.AIState = MonsterAIState.Return;` (테스트 line 65, 83) — `AIState` 프로퍼티는 `public ... { get; private set; }` (MonsterController.cs:13). EditMode 테스트 어셈블리에서 외부 set 불가 → **컴파일 에러** |
| `RecentHitWindow` 값 | ⚠️ | 코드 = `2f` (MonsterController.cs:34), BOARD 비고란 주장 = `5s` — 불일치 |
| "HP recover floor 50%" 적용 여부 | ❌ | BOARD 비고란 주장과 달리 **floor 50% 로직 없음**. 단순 `Hp = Def.hp` (전체 회복). |
| `IsInCombat` 윈도우 | ⚠️ | CombatManager.cs:31 = `3f`, MonsterController `RecentHitWindow` = `2f` — 두 시스템이 다른 임계값 사용 (의도적이라면 주석 권장) |
| Null-safety: `m.Def.attackRange × 1.3f` | ⚠️ | CombatManager.cs:165 — `m.Def` null 가능성 미체크 (다른 분기는 `m == null` 만 확인) |

### 검증 3: UI 추적
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| HP 바 표시 정합성 | ✅ | TakeDamage 경로에서 `_hpBar.UpdateHP` 호출 유지. Return 시 HP 복구가 발생하지 않으면 HP 바 갱신도 발생 X — 정상 동작 |
| 회피 시각 피드백 | N/A | 본 태스크 범위 외 |

### 검증 4: 사용자 시나리오
| 시나리오 | 결과 | 비고 |
|---------|------|------|
| 몬스터 공격 직전 회피 → 몬스터가 Return 상태로 가지 않음 | ✅ (코드 의도 일치) | CombatManager dodge 분기에서 LastHitByPlayerTime 갱신 → ChaseRangeMult/MaxSpawnDistance 조건 만족해도 `Time.time - LastHitByPlayerTime > RecentHitWindow` 가 false → Return 진입 차단 |
| 회피 후 몬스터가 도주 → 자연스러운 spawn 복귀 시 HP가 만피로 회복되지 않음 | ✅ (실제 데미지 입었을 때만) | line 159, 168 가드로 만피 상태 변하지 않음 |
| 회피 도중 몬스터 windup 공격 진행 | ❌ **회귀 가능성** | CombatManager.cs:172 — 회피 중 early return 이 모든 몬스터 공격 로직(`TryGetAttackToExecute`, `HandleMonsterAttacks` 본체) 차단. windup 중인 보스 패턴이 회피 1프레임으로 인해 **무효화** 될 수 있음 |
| 원거리 투사체가 회피 중 도착 | ✅ | OnArrive에서 LastHitByPlayerTime 갱신 → 데미지는 무적으로 차단 (정상) |

---

## 페르소나 리뷰

### 🎮 미나 (캐주얼 게이머)
**인상**: "어, 회피하니까 몬스터가 도망 안 가네. 좋다. 근데 회피하면서 몬스터 패턴이 끊기는 게 약간 어색해."
**문제점**:
- 회피 한 번에 보스 윈드업 공격이 사라지는 느낌 — 회피가 너무 강력해 보일 수도
- 몬스터가 어쨌든 화면 안에 있으면 계속 따라오니까 도망 안 됨 (의도라면 OK)
**제안**: 회피로 모든 공격을 무력화하지는 않게 — windup은 그대로 진행되되 데미지만 무효화

### ⚔️ 타쿠야 (코어 게이머)
**인상**: "수정 방향 자체는 맞다. 하지만 디테일이 엉성하다."
**문제점**:
- BOARD 비고란이 "RecentHitWindow=5s"라고 주장하는데 실제 코드는 `2f`. 명세-구현 불일치는 신뢰 저하
- "HP recover floor 50%" 도 코드에 존재하지 않음 — 비고란이 거짓이거나 구현 누락
- 회피 중 `early return`으로 몬스터 공격 로직 전체 정지 → 보스 페이즈 패턴/소환 액션이 회피 한 번에 캔슬될 수 있음
- `IsInCombat`(3s) vs `RecentHitWindow`(2s) 임계값 두 곳이 다름 — 한쪽 갱신 시 다른 쪽 깨질 수 있음
**제안**:
1. BOARD 비고란을 실제 구현에 맞춰 정정하거나, 명시한 값(5s/50%)을 실제 적용
2. 회피 중에도 `TryGetAttackToExecute` 흐름은 유지하되 데미지만 차단
3. `RecentHitWindow` / `IsInCombat` 임계값을 단일 상수(예: `GameConfig`)로 통합

### 🎨 하루카 (UX/UI 디자이너)
**인상**: "사용자 시각에서는 의도한 대로 동작 — 회피해도 몬스터가 갑자기 만피로 돌아가는 그 어색함이 사라졌다."
**문제점**:
- 회피 시 몬스터가 윈드업을 멈추고 그대로 멈춰있는 시각적 어색함 가능성 (검증 4 시나리오 3)
- HP 바는 변화 없음 — 좋음
**제안**: 회피 성공 시 몬스터의 attack windup VFX 가 자연스럽게 fade out 되도록 별도 처리

### 🔍 켄지 (QA 엔지니어) — **CRITICAL**
**인상**: "테스트가 컴파일 안 됨. 통합 회귀 테스트 어셈블리 빌드 실패 위험."
**문제점**:
1. **컴파일 블로커**: `DodgeMonsterResetBugTest.cs:65, 83` — `_monster.AIState = MonsterAIState.Return;` 시도. `AIState`는 `private set` 프로퍼티이므로 EditMode 테스트 어셈블리에서 외부 set 불가능 → CS0272 컴파일 에러
2. **테스트 setup 불완전**:
   - `monsterDef.name`, `monsterDef.sprite`, `monsterDef.animationDef` 미설정 — `Init()` 내부 `Resources.LoadAll`, `MonsterHPBar.Create`, `NameLabel.Create` 호출 시 NRE/Warning 가능
   - `PlayerController` 에 `Init()`/`PlayerStats` 주입 없이 `_player.Position`, `AimDirection`, `Invincible`, `IsDodging` 직접 접근 — 기본값 0 으로 작동할 가능성 있으나 미정의 동작
3. **Test 3 (LastHitByPlayerTimeShouldUpdateDuringDodge)**:
   - `typeof(PlayerController).GetField("IsDodging", reflection)` — IsDodging 가 프로퍼티이면 `GetField` 는 null 반환, `?.SetValue` 는 silent skip → **테스트가 실제로 dodge 상태를 만들지 못한 채 통과/실패할 수 있음**
   - `Invincible` 도 동일하게 `GetProperty` 로 시도하지만 setter 가 public 인지 미검증
4. **CombatManager.HandleMonsterAttacks**:
   - `_player == null` 가드는 있지만 `_player.Invincible`, `_player.IsDodging` 접근 시 `Init()` 미호출 상태이면 의존 컴포넌트 NRE 가능
**제안**:
1. `AIState` 에 internal setter + `[InternalsVisibleTo("Tests.EditMode")]` 또는 테스트 전용 `SetAIStateForTest(MonsterAIState)` API 추가
2. 테스트 setup 에 `MonsterDef` 의 모든 필수 필드, `PlayerController.Init()` 호출 추가
3. Test 3 의 reflection 접근을 실제 멤버 시그니처와 맞춰 재작성 (또는 PlayerController 에 `SetDodgeStateForTest` API 추가)
4. CI 에 `dotnet test` 또는 Unity test runner 통과 확인 후 In Review 제출

---

## 종합 판정

| 페르소나 | 판정 | 핵심 사유 |
|---------|------|----------|
| 미나 | ⚠️ APPROVE w/ minor | 의도한 UX 달성, 부수효과 약간 |
| 타쿠야 | ❌ NEEDS_WORK | 명세-구현 불일치, 임계값 분산 |
| 하루카 | ✅ APPROVE | UX 측면 정상 |
| 켄지 | ❌ **NEEDS_WORK (BLOCKER)** | 테스트 컴파일 불가, setup 불완전 |

**최종 권고: ❌ NEEDS_WORK**

---

## 크리티컬 이슈

1. **[BLOCKER] 테스트 컴파일 불가**
   - `DodgeMonsterResetBugTest.cs:65, 83` 에서 `_monster.AIState = MonsterAIState.Return;` 시도
   - `MonsterController.AIState` 는 `public ... { get; private set; }` (line 13) → 외부 어셈블리에서 set 불가능
   - **이 상태로 머지하면 EditMode 테스트 어셈블리 전체 빌드 실패**
   - 즉시 수정 필요: `AIState` 에 internal setter + InternalsVisibleTo 또는 테스트용 setter API 추가

2. **[명세 불일치] BOARD 비고란 vs 실제 구현**
   - BOARD 비고란: `RecentHitWindow=5s, HP recover floor 50%`
   - 실제 코드: `RecentHitWindow = 2f`, HP 는 단순 `= Def.hp` (전체 회복, floor 없음)
   - 비고란을 정정하거나, 명시한 값을 실제 적용 — 둘 중 하나 필요

3. **[회귀 가능성] 회피 중 몬스터 공격 로직 전면 정지**
   - `CombatManager.HandleMonsterAttacks` line 172 의 early return 으로 인해 회피 한 프레임에 모든 몬스터의 `TryGetAttackToExecute` / `ConsumePhaseEnterActions` / pattern 실행이 스킵됨
   - 보스 windup / 페이즈 진입 액션 / 다단 패턴이 회피 1프레임으로 무효화될 위험
   - 회피 중에도 attack 진행은 유지하되 `ApplyDamageToPlayer` 만 차단하는 구조 권장

4. **[일관성] 임계값 분산**
   - `MonsterController.RecentHitWindow = 2f` vs `CombatManager.IsInCombat` 의 `3f` 이중 정의
   - 한 곳만 수정 시 다른 쪽이 깨질 위험 — `GameConfig` 단일 상수로 통합 권장

---

## 마이너 이슈

5. CombatManager.cs:165 — `m.Def.attackRange` 접근 시 `m.Def` null 체크 부재
6. 테스트의 reflection 기반 멤버 접근(`GetField` / `GetProperty`) — 실제 멤버 종류와 일치하는지 검증 필요
7. `MonsterDef` ScriptableObject 의 `name/sprite/animationDef` 필드 미설정으로 `Init()` 내부 경고 발생 가능

---

## 다음 단계 (Developer 액션)

1. **즉시 (블로커)**: `AIState` 의 테스트 가능성 확보 — internal setter 또는 `SetAIStateForTest` API
2. **즉시 (명세 정합)**: BOARD 비고란을 실제 구현(2s, 전체회복)에 맞춰 정정 — 또는 합의된 값을 코드에 반영
3. **권장**: 회피 중 monster pattern progression 유지하면서 데미지만 차단하는 구조로 리팩터
4. **권장**: `RecentHitWindow` / `IsInCombat` 임계값을 `GameConfig` 로 통합
5. EditMode 테스트 setup 보강 (PlayerController.Init, MonsterDef 필수 필드)
6. 수정 후 EditMode 테스트가 실제 빌드/통과되는지 로컬에서 확인 후 v3 제출
