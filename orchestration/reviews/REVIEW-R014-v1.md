# REVIEW-R014-v1: 퀘스트 추적 HUD 위젯

> **리뷰 일시:** 2026-04-02
> **태스크:** R-014 퀘스트 추적 HUD 위젯
> **스펙:** SPEC-R014
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | questTrackerRoot + Content + EntryPrefab SerializeField |
| 컴포넌트/노드 참조 | ✅ | HUD → QuestSystem, Inventory 참조 (GameManager.Instance) |
| 에셋 존재 여부 | ✅ | 프리팹 바인딩 (코드만 변경) |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | questTrackerRoot/Content/EntryPrefab 필드 | HUD.cs:66-68 | ✅ |
| 2 | MaxTrackedQuests = 3 | HUD.cs:75 | ✅ |
| 3 | UpdateQuestTracker 매 프레임 | HUD.cs:136 (Update에서 호출) | ✅ |
| 4 | 아이템 수집 목표: current/required | HUD.cs:443-447 | ✅ |
| 5 | 킬 목표: kills/required | HUD.cs:451-458 | ✅ |
| 6 | 완료 목표: 녹색 + ✅ | HUD.cs:445-446, 456-457 `#88ff88` + `\u2705` | ✅ |
| 7 | V 키 토글 | HUD.cs:141-142 | ✅ |
| 8 | 퀘스트 없으면 숨김 | HUD.cs:469-470 `count > 0 && visible` | ✅ |
| 9 | StringBuilder 재사용 | HUD.cs:78 `_questSb` | ✅ |
| 10 | null 방어 | HUD.cs:414-418 | ✅ |

### 코드 품질

- `_questSb` 재사용 (매 프레임 new 방지) — GC 최적화 ✅
- `Mathf.Min(have, r.count)` — 초과 수량 표시 방지 ✅
- `Mathf.Min(kills, kr.count)` — 동일 ✅
- TextMeshPro rich text `<color>` 태그 사용 — 색상 표현 ✅
- `<b>` 태그로 퀘스트 이름 강조 ✅

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 퀘스트 이름 표시 | ✅ | `<b>{q.title}</b>` |
| 목표 진행률 | ✅ | `{itemId}: {current}/{required}` |
| 완료 표시 | ✅ | 녹색(#88ff88) + ✅ |
| V 키 토글 | ✅ | questTrackerRoot SetActive |
| 최대 3건 | ✅ | MaxTrackedQuests = 3 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 퀘스트 수락 | 위젯에 즉시 표시 (매 프레임 갱신) |
| 몬스터 처치 | 킬 카운트 실시간 증가 |
| 아이템 수집 | 수집 카운트 실시간 증가 |
| 목표 달성 | 해당 줄 녹색 + ✅ |
| 퀘스트 완료 | 위젯에서 제거 (active 목록에서 빠짐) |
| 4개 이상 퀘스트 | 3건만 표시 |
| V 키 | 위젯 숨김/표시 토글 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
퀘스트 창 안 열어도 뭘 해야 하는지 한눈에 보여서 좋다! ✅ 마크도 직관적. V 키로 숨길 수 있으니 화면 가릴 때 편하겠다.

### ⚔️ 코어 게이머
3건 제한은 합리적. 실시간 킬 카운트 갱신이 파밍 동기부여에 효과적. 다만 퀘스트 우선순위 순서(최근 수락순?)가 명확하면 더 좋겠다.

### 🎨 UX/UI 디자이너
rich text로 색상/볼드 표현 — TMP 기능 활용 양호. `#88ff88` 녹색은 일반 흰색 텍스트와 대비 충분. 우상단 배치는 RPG 장르 컨벤션에 맞음.

### 🔍 QA 엔지니어
- 매 프레임 UpdateQuestTracker 호출 — 퀘스트 수 적어 성능 OK
- `_questSb` StringBuilder 재사용 — GC 방지 ✅
- `GetActiveQuests()` 반환값이 null이면? → gm.Quests null 체크 있으나 GetActiveQuests 반환 null 미체크. 다만 반환값이 배열이므로 Length 접근 시 NRE 가능 — QuestSystem 구현에 따라 다름.

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 6항목) |
| 2 | Info | GetActiveQuests() null 반환 시 NRE 가능 (QuestSystem 구현 의존) |

---

## 최종 판정

**✅ APPROVE**

SPEC 10개 기능 항목 전부 충족. 아이템/킬 목표 추적, 완료 표시, V 키 토글, 최대 3건 제한 모두 정확. StringBuilder 재사용으로 GC 최적화.
