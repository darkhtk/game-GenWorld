# REVIEW-R024-v1: 월드 이벤트 시스템

> **리뷰 일시:** 2026-04-02
> **태스크:** R-024 월드 이벤트 시스템
> **스펙:** SPEC-R024
> **판정:** ✅ APPROVE

---

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | WorldEventDef 구조 | line 9-13 | ✅ |
| 2 | 4개 이벤트 등록 | Init line 31-34 | ✅ |
| 3 | 매 게임 1시간 체크 | line 39 `gameHour - _lastCheckHour < 1f` | ✅ |
| 4 | 최소 간격 체크 | line 59 | ✅ |
| 5 | 확률 체크 | line 60 `Random.value > def.chance` | ✅ |
| 6 | 셔플 순서 | line 50-54 Fisher-Yates shuffle | ✅ |
| 7 | bonus_drops: 드롭 2x, 골드 1.5x | line 75-76 | ✅ |
| 8 | 이벤트 종료 시 리셋 | EndEvent line 93-94 | ✅ |
| 9 | WorldEventStart/EndEvent 발행 | line 80-84, 96 | ✅ |
| 10 | EventTimeRemaining 계산 | line 23-24 | ✅ |
| 11 | Serialize/Restore | line 102-122 | ✅ |
| 12 | 로드 시 만료 판단 | Restore line 117 `currentHour - startHour < duration` | ✅ |

### SPEC 이벤트 검증

| 이벤트 | SPEC | 코드 | 일치 |
|--------|------|------|------|
| Blood Moon | elite_spawn, 2h, 8h, 30% | ✅ | ✅ |
| Golden Hour | bonus_drops, 1h, 6h, 40% | ✅ | ✅ |
| Goblin Raid | invasion, 1.5h, 10h, 20% | ✅ | ✅ |
| Wandering Merchant | merchant, 3h, 12h, 25% | ✅ | ✅ |

### 코드 품질

- Fisher-Yates shuffle로 공정한 이벤트 선택 순서 ✅
- `_activeEvent` nullable struct — 단일 활성 이벤트 보장 ✅
- `_lastOccurrence` Dictionary로 이벤트별 쿨다운 추적 ✅
- EndEvent에서 multiplier 리셋 — 누적 방지 ✅
- Restore에서 만료된 이벤트는 미복원 — 올바른 동작 ✅

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
Blood Moon이나 Golden Hour 같은 이벤트가 갑자기 나오면 흥미진진! 방랑 상인도 특별한 느낌.

### ⚔️ 코어 게이머
Golden Hour(드롭 2x) 타이밍에 파밍 몰아치기 전략. Blood Moon 엘리트는 고위험 고보상. 이벤트 간격이 충분해서 스팸 안 됨.

### 🔍 QA 엔지니어
- 활성 이벤트 중에는 새 이벤트 체크 안 함 (line 42-47) — 이벤트 충돌 방지 ✅
- elite_spawn/invasion은 StartEvent에서 MonsterSpawner 호출 미구현 (EventBus 발행으로 대체) — 구독 측에서 처리 예정
- Serialize tuple 반환 — SaveData 통합 필요

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | elite_spawn/invasion/merchant 실행 로직이 EventBus 발행만 (구독 측 Phase 2) |
| 2 | Low | 테스트 미작성 |

---

## 최종 판정

**✅ APPROVE**

SPEC 12개 기능 항목 전부 충족. 4개 이벤트, 확률/간격/지속 체계, Fisher-Yates 셔플, bonus 멀티플라이어, Serialize/Restore, 만료 판단 모두 정확.
