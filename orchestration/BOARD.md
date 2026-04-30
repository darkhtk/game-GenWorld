# Orchestration Board

> **최종 업데이트:** 2026-04-30 (Coordinator)
> **프로젝트:** GENWorld
> **현재 상태:** Stabilize (Done 150건+, In Progress 0건, Rejected 2건)

> ⚠️ **개발자 다음 루프 우선처리:** Rejected 재작업 → S-101 (high) → S-084 (P3). 픽업 시 In Progress로 이동 + REVIEW 사유 확인.

---

## 로드맵

| #   | 태스크                                   | 우선순위 | 상태  | 비고               |
| --- | ------------------------------------- | ---- | --- | ---------------- |
| 1   | S-101: 회피 기능 수행 시 몬스터 리셋 버그 수정        | high | ❌   | Rejected — 수정 필요 |
| 2   | S-084: WorldEventSystem 종료 잔존 오브젝트 정리 | P3   | ❌   | Rejected — 수정 필요 |

---

## ❌ Rejected (최우선 수정)

| 태스크                                  | 사유                                | REVIEW                            | 비고   |
| ------------------------------------ | --------------------------------- | --------------------------------- | ---- |
| S-101 회피 기능 수행 시 몬스터 리셋 버그 수정        | 회피 중 몬스터 AI 상태 초기화 — 원인 파악 필요    | reviews/REVIEW-S-101-v1.md       | high |
| S-084 WorldEventSystem 종료 잔존 오브젝트 정리 | OnDisable 풀 반환 누락                | reviews/REVIEW-S-084-v1.md       | P3   |

---

## 🔧 In Progress

| 태스크 | 우선순위 | 담당  | 시작일 | 비고  |
| --- | ---- | --- | --- | --- |

---

## 👀 In Review

| 태스크 | 우선순위 | 완료일 | 결과  | 비고  |
| --- | ---- | --- | --- | --- |

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

---

## 📋 Backlog

| 태스크 | 우선순위 | 비고  |
| --- | ---- | --- |
