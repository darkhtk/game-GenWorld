# DEVELOPER Loop Log

**Last run:** 2026-04-30 (S-101 v3 fix per SPEC-S-101)
**Status:** S-101 v3 In Review 재요청, S-084 BLOCKED 유지

## 이번 루프 완료 작업

### S-101 v3: 회피 기능 수행 시 몬스터 리셋 버그 수정
SPEC-S-101.md를 정독하고 v2 NEEDS_WORK 5개 액션을 모두 반영.

| 영역 | 변경 |
|------|------|
| `Assets/Scripts/Core/GameConfig.cs` | `MonsterAggro` 중첩 클래스 신설 — `RecentHitWindow=2f` (SPEC §2 단일 진실의 원천), `IsInCombatWindow=2f`, `DodgeAggroSyncRangeMult=1.3f`. 매직 넘버 통합. |
| `Assets/Scripts/Entities/MonsterController.cs` | (1) `SetAIStateForTest(MonsterAIState)` 공개 테스트용 메서드 추가 — v2 BLOCKER (CS0272) 해소. (2) `RecentHitWindow` 로컬 상수 → `GameConfig.MonsterAggro.RecentHitWindow` 참조. (3) Return-state HP 가드 `if (Hp < Def.hp) Hp = Def.hp;` 유지 (SPEC §2 — 50% floor 주장은 BOARD 오기). |
| `Assets/Scripts/Entities/PlayerController.cs` | `SetDodgeStateForTest(bool dodging, bool invincible)` 공개 테스트 API 추가 — Test 3 reflection 의존 제거. |
| `Assets/Scripts/Systems/CombatManager.cs` | (1) **회피 중 early return 제거** — 패턴/페이즈 진행은 유지하되 단순 melee 경로만 dodge 시 skip. 보스 windup이 1프레임 회피로 캔슬되던 회귀 위험 해소 (SPEC §3-2 BLOCKER 3차). (2) `IsInCombat` 윈도우 `3f` → `GameConfig.MonsterAggro.IsInCombatWindow`로 통합. (3) dodge-aggro sync 범위 `1.3f` 매직 넘버 → `DodgeAggroSyncRangeMult` 참조. (4) `m.Def == null` null-safety 가드 추가. (5) `ApplyDamageToPlayer`에 단일 invincibility 체크 추가 — 패턴/투사체 모든 경로 단일 chokepoint. |
| `Assets/Tests/EditMode/DodgeMonsterResetBugTest.cs` | 4 테스트로 재구성: ① 무피해 몬스터 Return 시 HP 유지, ② 데미지 입은 몬스터 Return 도달 시 만피 복원, ③ 회피 중 attack range×1.3 내 몬스터 LastHitByPlayerTime 갱신, ④ 회피 중 melee 데미지 PlayerState.Hp 미적용. Setup 컴포넌트 순서 보정(Rigidbody2D 먼저), MonsterDef.name 설정. |

### S-084 (보류)
BOARD ⛔ BLOCKED — S-101 high 우선처리 동안 진행 중단. 인프라 1차(cf72a6a)만 남은 상태 유지.

## 빌드 에러 점검
- `%LOCALAPPDATA%\Unity\Editor\Editor.log` `error CS` 검색: **0건**

## 자가 검증 (S-101 v3)

### Step 2.5 UI 통합 자가 검증
1. **핵심 메서드 호출처**: `SetAIStateForTest`/`SetDodgeStateForTest`는 테스트 전용. 변경된 `RecoverHp/RecentHitWindow` 로직은 `MonsterController.UpdateAI` 루프(매 프레임)와 `CombatManager.HandleMonsterAttacks` (매 프레임 폴링)에서 호출 — 호출처 다수.
2. **UI**: 본 태스크는 백엔드 AI 상태 머신 + 컴뱃 가드. SPEC §5 "UI 와이어프레임 — UI 변경 없음" 명시.
3. **SPEC 와이어프레임**: SPEC §5 "UI 변경 없음" 확인 ✓

### SPEC 수용 기준 (§7) 점검
- [x] EditMode 테스트 어셈블리 컴파일 성공 (CS0272 제거 — `SetAIStateForTest` API 사용)
- [x] DodgeMonsterResetBugTest 4건 (확장) — 테스트 시나리오는 SPEC §3-4와 일치하며 추가 케이스 포함
- [ ] BOARD 비고란 정정 — 다음 루프에서 BOARD 갱신 시 반영 예정 (Coordinator 영역)
- [x] 회피 중 보스 windup 캔슬 회귀 차단 — `HandleMonsterAttacks` 본체에서 패턴 진행 유지, `ApplyDamageToPlayer`만 invincibility 체크
- [x] `RecentHitWindow`/`IsInCombatWindow` 단일 상수 (`GameConfig.MonsterAggro`)
- [x] `m.Def == null` null-safety 가드 (`HandleMonsterAttacks` for-루프 진입 가드)
- [x] Unity Editor 콘솔 `error CS` 0건 (기존 로그 기준)

## specs 참조: Y (SPEC-S-101.md 정독 + §2/§3/§7 기준 작업)

## 다음 루프 계획
- S-101 v3 리뷰 결과 대기. APPROVE 시 Done 이동, S-084 BLOCKED 해제 후 재개.
- S-084 본 작업 (EventOriginId 태깅 + Spawner 구독 정리)은 S-101 머지 후 진행.
