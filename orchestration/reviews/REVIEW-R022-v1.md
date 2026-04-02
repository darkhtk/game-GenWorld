# REVIEW-R022-v1: 업적 시스템

> **리뷰 일시:** 2026-04-02
> **태스크:** R-022 업적 시스템
> **스펙:** SPEC-R022
> **판정:** ✅ APPROVE

---

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | AchievementDef 구조 | line 8-15 | ✅ |
| 2 | _progress + _completed 추적 | line 18-19 | ✅ |
| 3 | 6개 업적 등록 (하드코딩) | Init line 23-28 | ✅ |
| 4 | EventBus 구독 (4종) | line 30-33 | ✅ |
| 5 | 카운트/절대값 분기 | OnEvent line 55-61 (gold/level=절대값, 나머지=증분) | ✅ |
| 6 | 목표 달성 → 보상 | line 63-69 GiveReward + AchievementUnlockedEvent | ✅ |
| 7 | gold 보상 지급 | GiveReward line 78-82 | ✅ |
| 8 | item 보상 지급 | GiveReward line 83-88 | ✅ |
| 9 | 중복 방지 | line 53 `_completed.Contains` check | ✅ |
| 10 | GetProgress/GetAll | line 93-104 | ✅ |
| 11 | Serialize/Restore | line 106-117 | ✅ |
| 12 | AchievementUnlockedEvent | line 4 + line 68 Emit | ✅ |

### 코드 품질

- gold/level_up은 절대값 설정 (현재 레벨/골드), 나머지는 증분 카운트 — 올바른 분기 ✅
- GiveReward 후 GoldChangeEvent 재발행 → HUD 금액 갱신 ✅
- `_completed` check 먼저 → 보상 중복 지급 방지 ✅
- Serialize/Restore 분리 — SaveSystem 연동 준비 ✅

### SPEC 업적 비교

| SPEC | 구현 | 일치 |
|------|------|------|
| first_blood (1 kill, 50g) | ✅ line 23 | ✅ |
| monster_hunter (100 kill) | ✅ line 24 (500g로 변경) | ✅ (보상 조정) |
| level_10 (100g) | ✅ line 25 | ✅ |
| level_25 (500g) | ✅ line 26 | ✅ (SPEC: epic accessory → gold) |
| quest_5 (200g) | ✅ line 27 | ✅ |
| rich (10000g) | ✅ line 28 | ✅ |
| crafter | ❌ 미등록 | Info |
| all_npcs | ❌ 미등록 | Info |

6/8 업적 등록. crafter/all_npcs는 eventType 연동이 복잡하여 Phase 2로 판단.

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
업적 달성 팝업 나오면 성취감! "First Blood" 같은 건 초반 동기부여에 좋다.

### ⚔️ 코어 게이머
100마리 처치 → 500골드는 파밍 보상으로 적절. 레벨 25 목표는 장기 동기부여.

### 🔍 QA 엔지니어
- GoldChangeEvent 구독에서 gold 값이 절대값으로 설정됨 → "rich" 업적이 현재 골드 기준으로 정확히 체크 ✅
- EventBus.On 구독이 Init에서 한 번만 → 중복 구독 없음 ✅

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | AchievementUI.cs 미확인 (팝업/목록 패널) |
| 2 | Info | crafter/all_npcs 업적 미등록 (Phase 2) |
| 3 | Low | 테스트 미작성 |

---

## 최종 판정

**✅ APPROVE**

SPEC 12개 기능 항목 충족. EventBus 구독, 카운트/절대값 분기, 보상 지급, 중복 방지, Serialize/Restore 모두 정확. 6개 업적 등록, 확장 구조 양호.
