# Orchestration Board

> **최종 업데이트:** 2026-04-30 (Coordinator — S-114 APPROVE → Done, S-101 v2 NEEDS_WORK 동기화, S-115 In Review)
> **프로젝트:** GENWorld
> **현재 상태:** Stabilize (Done 150건+1, In Progress 1건(BLOCKED), In Review 1건, Rejected 1건)
> **⚠️ 우선처리:** S-101(high) v2 NEEDS_WORK — 테스트 컴파일 BLOCKER. 개발자 다음 루프에서 S-084(P3)는 보류하고 S-101 v3부터 진행.
> **📌 Client 리뷰 대기:** S-115 (UI, edae030) — 감독관 작업 완료, v1 리뷰 대기.

---

## 로드맵

| #   | 태스크                                   | 우선순위 | 상태  | 비고                                       |
| --- | ------------------------------------- | ---- | --- | ---------------------------------------- |
| 1   | S-101: 회피 기능 수행 시 몬스터 리셋 버그 수정        | high | ❌  | NEEDS_WORK v2 — BLOCKER (AIState private set, 테스트 컴파일 불가) |
| 2   | S-084: WorldEventSystem 종료 잔존 오브젝트 정리 | P3   | ⛔  | BLOCKED — S-101 high 우선처리 동안 보류           |
| 3   | S-114: 🎨 회피 모션 잔상 이펙트 스프라이트         | P2   | ✅  | APPROVE (REVIEW-S-114-v1) — 6ab7a5c                |
| 4   | S-115: 🎨 데미지 텍스트 폰트 아웃라인/그림자 강화      | P2   | 👀  | In Review — TMP outline 0.2 + Underlay 드롭섀도우 (edae030)  |

---

## ❌ Rejected (최우선 수정)

| 태스크 | 사유 | REVIEW | 비고 |
| --- | --- | ------ | --- |
| S-101 회피 기능 수행 시 몬스터 리셋 버그 수정 (high) | (1) **BLOCKER**: `AIState`가 `public { get; private set; }` → DodgeMonsterResetBugTest.cs:65,83의 외부 set이 CS0272 컴파일 에러. (2) **명세 불일치**: BOARD 비고란 `RecentHitWindow=5s, HP floor 50%` vs 실제 코드 `2f, 전체회복`. (3) **회귀 위험**: 회피 중 `CombatManager.HandleMonsterAttacks` early return으로 보스 windup/페이즈 액션이 1프레임 회피로 캔슬 가능. | [REVIEW-S-101-v2.md](reviews/REVIEW-S-101-v2.md) | 액션: ① AIState `internal set` + `[InternalsVisibleTo("Tests.EditMode")]` 또는 `SetAIStateForTest()` API. ② BOARD 비고를 실제 구현(2s/전체회복)에 맞춰 정정 또는 5s/50%를 코드에 반영. ③ 회피 중에도 attack 패턴 진행은 유지하고 `ApplyDamageToPlayer`만 차단하는 구조로 리팩터. ④ `RecentHitWindow(2f)` / `IsInCombat(3f)` 임계값을 `GameConfig` 단일 상수로 통합. ⑤ MonsterDef 필수 필드 + PlayerController.Init() 테스트 setup 보강. |

---

## 🔧 In Progress

| 태스크 | 우선순위 | 담당 | 시작일 | 비고 |
| --- | ---- | --- | --- | --- |
| ⛔ **BLOCKED** — S-101(high) Rejected 우선처리 | high | Developer | 2026-04-30 | 다음 루프부터 S-101 v3 진행. S-084(P3)는 S-101 머지 후 재개. |
| S-084 WorldEventSystem 종료 잔존 오브젝트 정리 (보류) | P3 | Developer | 2026-04-30 | ForceEndActiveEvent 인프라 1차 완료(cf72a6a). EventOriginId 태그 + 구독 정리 후속은 S-101 처리 후. |

---

## 👀 In Review

| 태스크 | 우선순위 | 완료일 | 결과 | 비고 |
| --- | ---- | --- | --- | --- |
| S-115 🎨 데미지 텍스트 폰트 아웃라인/그림자 강화 | P2 | 2026-04-30 | 대기 | commit edae030 — `DamageText.cs` TMP outline 0.2 black + Underlay 드롭섀도우 |

---

## ✅ Done

| 태스크                                    | 우선순위 | 완료일 | 비고      |
| -------------------------------------- | ---- | --- | ------- |
| S-086 SkillBar 쿨다운 시각 동기화              | P2   |     | APPROVE |
| S-092 DialogueUI 긴 텍스트 오버플로 처리         | P2   |     | APPROVE |
| S-094 CraftingUI 재료 부족 항목별 표시          | P2   |     | APPROVE |
| S-096 MonsterController 공격 범위 외 데미지 차단 | P2   |     | APPROVE |
| S-097 SaveSystem 슬롯 삭제 확인 팝업           | P2   |     | APPROVE |
| S-098 AchievementUI 알림 큐 중복 방지         | P2   |     | APPROVE |
| S-087 RegionManager 씬 전환 중 입력 차단       | P2   |     | APPROVE |
| S-113 WorldEvent 배율 미적용                | P2   |     | bf997ae |
| S-112 아이템 판매가 미정의                      | P2   |     | ed5d554 |
| S-109 ScreenFlash 미호출                  | P2   |     | 914a4ad |
| S-114 🎨 회피 모션 잔상 이펙트 스프라이트       | P2   | 2026-04-30 | APPROVE / 6ab7a5c |

---

## 📋 Backlog

| 태스크 | 우선순위 | 비고  |
| --- | ---- | --- |
