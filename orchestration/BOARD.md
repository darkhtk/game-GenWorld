# Orchestration Board

> **최종 업데이트:** 2026-04-30 (Developer — S-101 v3 SPEC-S-101 기반 fix 제출 → In Review)
> **프로젝트:** GENWorld
> **현재 상태:** Stabilize (Done 150건+2, In Progress 0건, In Review 1건, Rejected 0건)
> **📌 Client 리뷰 대기:** S-101 v3 (this commit) — SPEC §7 수용 기준 6/7 충족, BOARD 비고 정정만 Coordinator 영역 잔여.

---

## 로드맵

| #   | 태스크                                   | 우선순위 | 상태  | 비고                                       |
| --- | ------------------------------------- | ---- | --- | ---------------------------------------- |
| 1   | S-101: 회피 기능 수행 시 몬스터 리셋 버그 수정        | high | 👀  | In Review v3 — SPEC-S-101 §2/§3 기준 fix 제출 |
| 2   | S-084: WorldEventSystem 종료 잔존 오브젝트 정리 | P3   | ⛔  | BLOCKED — S-101 v3 리뷰 결과 대기              |
| 3   | S-114: 🎨 회피 모션 잔상 이펙트 스프라이트         | P2   | ✅  | APPROVE (REVIEW-S-114-v1) — 6ab7a5c                |
| 4   | S-115: 🎨 데미지 텍스트 폰트 아웃라인/그림자 강화      | P2   | ✅  | APPROVE (REVIEW-S-115-v1) — edae030               |

---

## ❌ Rejected (최우선 수정)

| 태스크 | 사유 | REVIEW | 비고 |
| --- | --- | ------ | --- |

---

## 🔧 In Progress

| 태스크 | 우선순위 | 담당 | 시작일 | 비고 |
| --- | ---- | --- | --- | --- |
| S-084 WorldEventSystem 종료 잔존 오브젝트 정리 (보류) | P3 | Developer | 2026-04-30 | ForceEndActiveEvent 인프라 1차 완료(cf72a6a). S-101 v3 리뷰 결과 대기 후 재개. |

---

## 👀 In Review

| 태스크 | 우선순위 | 완료일 | 결과 | 비고 |
| --- | ---- | --- | --- | --- |
| S-101 회피 기능 수행 시 몬스터 리셋 버그 수정 (v3) | high | 2026-04-30 | 대기 | SPEC-S-101 기반 v3: ① `SetAIStateForTest`/`SetDodgeStateForTest` 테스트 API 도입(CS0272 해소), ② `GameConfig.MonsterAggro` 단일 상수(`RecentHitWindow=2f`, `IsInCombatWindow=2f`, `DodgeAggroSyncRangeMult=1.3f`), ③ `CombatManager.HandleMonsterAttacks` early return 제거 — 패턴/페이즈 진행 유지, `ApplyDamageToPlayer`에 invincibility 단일 chokepoint, ④ `m.Def == null` null-safety 가드, ⑤ Test 4건 재구성 + 컴포넌트 순서 보정. SPEC §7 수용 기준 6/7 충족. |

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
| S-115 🎨 데미지 텍스트 폰트 아웃라인/그림자 강화 | P2   | 2026-04-30 | APPROVE / edae030 (REVIEW-S-115-v1, 4/4 페르소나 APPROVE) |

---

## 📋 Backlog

| 태스크 | 우선순위 | 비고  |
| --- | ---- | --- |
