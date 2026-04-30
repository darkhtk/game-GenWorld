# Orchestration Board

> **최종 업데이트:** 2026-04-30 (Developer — S-124 인벤토리 드래그 ghost feedback 제출 → In Review)
> **프로젝트:** GENWorld
> **현재 상태:** Stabilize (Done 150건+4, In Progress 1건(P3 보류), In Review 2건, Rejected 0건)
> **📌 Client 리뷰 대기:** S-101 v3 + S-124.

---

## 로드맵

| #   | 태스크                                   | 우선순위 | 상태  | 비고                                       |
| --- | ------------------------------------- | ---- | --- | ---------------------------------------- |
| 1   | S-101: 회피 기능 수행 시 몬스터 리셋 버그 수정        | high | 👀  | In Review v3 — SPEC-S-101 §2/§3 기준 fix 제출 |
| 2   | S-084: WorldEventSystem 종료 잔존 오브젝트 정리 | P3   | ⛔  | BLOCKED — S-101 v3 리뷰 결과 대기              |
| 3   | S-114: 🎨 회피 모션 잔상 이펙트 스프라이트         | P2   | ✅  | APPROVE (REVIEW-S-114-v1) — 6ab7a5c                |
| 4   | S-115: 🎨 데미지 텍스트 폰트 아웃라인/그림자 강화      | P2   | ✅  | APPROVE (REVIEW-S-115-v1) — edae030               |
| 5   | S-116: 🎨 스킬 쿨다운 회복 SFX                     | P2   | ✅  | DONE — sfx_cooldown_ready.wav + HUD 트리거 (cfce018) |
| 6   | S-119: 🎨 레벨업 파티클 이펙트                     | P2   | ✅  | DONE — vfx_levelup_burst + LevelUpVFX 16 spark (3fd219c) |
| 7   | S-124: 인벤토리 드래그 앤 드롭 시각 피드백             | P2   | 👀  | In Review — 원본 슬롯 ghost(α=0.3) + dragIcon sprite 복사 |

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
| S-101 회피 기능 수행 시 몬스터 리셋 버그 수정 (v3) | high | 2026-04-30 | ✅ APPROVE | REVIEW-S-101-v3 (5373b76, [깊은 리뷰], 4/4 페르소나 APPROVE). v2 BLOCKER 5건 모두 해소. 다음 루프 Unity Editor 실 컴파일 + EditMode test 4건 실행 검증 권장 (SPEC §7 #1/#2/#7 마무리). 사전 존재 의심 1건: `MonsterAttackPatternSelector` 정의 위치 추적 — v3 책임 외, 별도 태스크 회부 권장. |
| S-124 인벤토리 드래그 앤 드롭 시각 피드백 | P2 | 2026-04-30 | 대기 | InventorySlotUI에 `IconSprite` getter / `SetDragGhost(bool)` 추가 (iconImage.color.a = 0.3). InventoryUI.OnSlotBeginDrag에서 원본 슬롯 ghost ON + dragIcon에 실제 sprite 복사. CancelDrag에서 ghost OFF. RefreshGrid의 SetItem이 color.white 강제 → drop 후 자동 복원. SPEC 없음 (BACKLOG_RESERVE 비고: "드래그 중 아이콘 반투명"). |

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
| S-116 🎨 스킬 쿨다운 회복 SFX 누락                  | P2   | 2026-04-30 | DONE / cfce018 (sfx_cooldown_ready.wav 220ms 1320Hz + HUD.UpdateCooldowns transition trigger) |
| S-119 🎨 레벨업 파티클 이펙트                       | P2   | 2026-04-30 | DONE / 3fd219c (vfx_levelup_burst 8f sprite + LevelUpVFX 16-spark 방사 1.0s) |

---

## 📋 Backlog

| 태스크 | 우선순위 | 비고  |
| --- | ---- | --- |
