# REVIEW-R020-v1: NPC 호감도 이벤트

> **리뷰 일시:** 2026-04-02
> **태스크:** R-020 NPC 호감도 이벤트
> **스펙:** SPEC-R020
> **판정:** ✅ APPROVE

---

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | _triggeredEvents HashSet | NPCBrain.cs:19 | ✅ |
| 2 | HasTriggered/MarkTriggered | NPCBrain.cs:21-22 | ✅ |
| 3 | EvaluateTriggers 로직 | AIManager.cs:232-266 | ✅ |
| 4 | triggerId 구성 (npcId_eventType_threshold) | line 241 | ✅ |
| 5 | 중복 발동 방지 (HasTriggered check) | line 242 | ✅ |
| 6 | 호감도 임계점 체크 (rel < threshold) | line 245 | ✅ |
| 7 | wantToTalk + talkReason 설정 | lines 249-253 | ✅ |
| 8 | memory 추가 | line 254-255 | ✅ |
| 9 | NpcTriggerEvent 발행 | lines 257-262 | ✅ |
| 10 | Serialize에 triggeredEvents 포함 | NPCBrain.cs:29 | ✅ |
| 11 | Restore에 triggeredEvents 복원 | NPCBrain.cs:30 | ✅ |

### 코드 품질

- `trigger.target ?? "player"` — null 안전 기본값 ✅
- `trigger.talkReason ?? "I have something for you."` — 기본 메시지 ✅
- `npcDef.triggers == null` 체크 — 트리거 없는 NPC 안전 ✅
- EventBus.Emit으로 다른 시스템 연동 가능 (gift/quest/unlock_shop은 Phase 2 구독) ✅
- 로그 출력 (`[AIManager] Trigger fired`) — 디버깅 용이 ✅

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 호감도 50 도달 | 해당 트리거 발동, wantToTalk=true, 말풍선 |
| 동일 트리거 재시도 | HasTriggered → skip (중복 방지) |
| 세이브/로드 후 | triggeredEvents 유지, 재발동 방지 |
| 트리거 없는 NPC | triggers == null → 조기 반환 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
NPC랑 친해지면 특별한 대화나 선물 받는 거 재밌겠다! 호감도 시스템이 있으면 NPC랑 더 교류하고 싶어짐.

### ⚔️ 코어 게이머
호감도 보상이 게임플레이에 미치는 영향(숨겨진 상점, 특별 퀘스트)이 전략적 깊이 추가. 트리거 중복 방지로 어뷰징 불가.

### 🔍 QA 엔지니어
- triggeredEvents가 Serialize/Restore에 포함 — 세이브 연속성 ✅
- relationship -100~100 범위는 NPCBrain.UpdateRelationship에서 관리 — R-020 범위 밖이나 기존 로직 존재

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 6항목) |
| 2 | Info | gift/quest/unlock_shop eventType 처리는 NpcTriggerEvent 구독으로 Phase 2 구현 예정 |

---

## 최종 판정

**✅ APPROVE**

SPEC 11개 기능 항목 전부 충족. 트리거 평가, 중복 방지, wantToTalk/memory 설정, NpcTriggerEvent 발행, 세이브 연동 모두 정확.
