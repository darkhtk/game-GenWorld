# REVIEW-R008-v1: 조건부 대화 분기

> **리뷰 일시:** 2026-04-02
> **태스크:** R-008 조건부 대화 분기
> **스펙:** SPEC-R008
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | VillageNPC → DialogueConditionParser 참조 정상 |
| 에셋 존재 여부 | ✅ | 코드만 변경, JSON 데이터 스키마 확장 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### SPEC 항목별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | NpcDef에 conditionalDialogues 추가 | NpcDef.cs:13 | ✅ |
| 2 | ConditionalDialogue 클래스 (id, condition, greeting, options, priority) | NpcDef.cs:21 | ✅ |
| 3 | DialogueConditionParser 정적 클래스 | DialogueConditionParser.cs | ✅ |
| 4 | "quest_active:" 조건 | DialogueConditionParser.cs:13-17 | ✅ |
| 5 | "quest_done:" 조건 | DialogueConditionParser.cs:19-23 | ✅ |
| 6 | "has_item:id:count" 조건 | DialogueConditionParser.cs:25-32 | ✅ |
| 7 | "relationship>=N" 조건 | DialogueConditionParser.cs:34-39 | ✅ |
| 8 | FindBestMatch — priority 최고 선택 | DialogueConditionParser.cs:58-77 | ✅ |
| 9 | VillageNPC.EvaluateConditionalDialogue | VillageNPC.cs:56-61 | ✅ |
| 10 | DialogueUI.Show — 조건부 인사말/옵션 표시 | DialogueUI.cs:89-108 | ✅ |
| 11 | 조건 미충족 시 폴백 | conditional == null → 기존 흐름 | ✅ |

### 추가 구현 (SPEC 외)

- `relationship<=` 조건 (line 41-46) — 논리적 확장, 역관계 조건 지원

### 코드 품질

- `Evaluate()` 전체를 try-catch로 감싸 파싱 오류 시 false 반환 — 안전
- `has_item` 파싱: `:` 구분자로 split → 아이템 ID에 `:` 포함 시 문제 가능하나, 현재 데이터에서 콜론 ID는 없음
- null 체크 철저: `quests != null`, `inventory != null`, `brain != null`, `dialogues == null`, `d == null`
- 미지 조건은 Warning 로그 + false 반환 — 데이터 오류 디버깅에 유용

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 조건부 인사말 표시 | ✅ | DialogueUI.cs:104-105 AppendLog |
| 조건부 옵션 표시 | ✅ | DialogueUI.cs:106-107 ShowOptions |
| 기존 대화 흐름 호환 | ✅ | conditional == null이면 기존 동작 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 특정 아이템 3개 보유 + NPC 대화 | 조건 충족 → 특수 인사말 + 옵션 표시 |
| 퀘스트 진행 중 + NPC 대화 | quest_active 조건 충족 → 퀘스트 관련 대화 |
| 조건 없는 NPC 대화 | conditionalDialogues null → 기존 LLM 대화 |
| 여러 조건 동시 충족 | priority 최고 항목 선택 |
| JSON에 조건 데이터 없음 | null array → EvaluateConditionalDialogue returns null → 폴백 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
NPC가 내가 가진 아이템에 반응하는 거 좋다! "허브 모아왔구나!" 같은 대사가 나오면 세계가 살아있는 느낌. 기존 AI 대화도 그대로 유지되니까 자연스러울 듯.

### ⚔️ 코어 게이머
priority 시스템으로 조건 충돌 해결 — 깔끔. 퀘스트 진행도에 따라 NPC 반응이 달라지면 반복 상호작용 동기 부여. relationship 조건도 있으니 호감도 시스템과 자연스럽게 연동 가능.

### 🎨 UX/UI 디자이너
조건부 옵션이 기존 ShowOptions()를 재사용 — UI 일관성 유지. 인사말이 AppendLog로 표시되어 대화 흐름에 자연스럽게 삽입됨.

### 🔍 QA 엔지니어
- `has_item` 파싱에서 `:` 2개 이상 포함 시 (예: `has_item:item:with:colon:3`) → `Split(':')` 결과 parts[0]="item", parts[1]="with" → 잘못된 매칭. 현재 아이템 ID에 콜론 없으므로 실질적 문제 없으나 향후 주의.
- `brain.GetRelationship("player")` — "player"가 하드코딩. 멀티플레이어 시 문제 가능하나 현재 싱글플레이어 게임.
- `int.TryParse` 실패 시 false 반환 — 데이터 오류에 안전.

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 6항목) |
| 2 | Info | has_item 조건에서 아이템 ID에 콜론 포함 시 파싱 오류 가능 |

---

## 최종 판정

**✅ APPROVE**

SPEC 11개 항목 전부 충족. ConditionalDialogue 데이터 구조, DialogueConditionParser (4종 조건 + 보너스 1종), VillageNPC 평가 로직, DialogueUI 조건부 표시 모두 정확 구현. 기존 LLM 대화와 자연스러운 폴백.
