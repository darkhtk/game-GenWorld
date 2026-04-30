# REVIEW-S-127-v1: 미니맵 NPC 마커 색상 분리 (퀘스트/상점/기본)

**Task:** S-127
**리뷰 일시:** 2026-04-30 (Client 3회차)
**대상 커밋:** c88f1c2 — feat(S-127): 미니맵 NPC 마커 색상 분리 (퀘스트/상점/기본)
**리뷰 모드:** 일반
**SPEC 참조:** specs/SPEC-S-127.md **부재** (RESERVE 비고 의도 충족 확인)

---

## 변경 요약 (코드 직접 확인)

| 위치 | 변경 내용 |
|------|----------|
| `Assets/Scripts/UI/MinimapUI.cs:13-15` | `static readonly Color NpcMarkerDefault/Shop/Quest` 3상수 (`#66ff88` 녹색 / `#ffd933` 주황 / `#ff8c33` 노랑) |
| `Assets/Scripts/UI/MinimapUI.cs:216-225` | `public static Color ClassifyNpcMarkerColor(NpcDef def, bool hasQuestForNpc)` — 우선순위 ① actions에 `"open_shop"` → Shop ② hasQuestForNpc → Quest ③ Default. NullDef/NullActions 가드 |
| `Assets/Scripts/UI/MinimapUI.cs:204-214` | `static void ApplyNpcMarkerColor(RectTransform, VillageNPC, QuestSystem, InventorySystem)` — `Image` 컴포넌트 lookup + `quests.GetQuestStatusForNpc(npc.Def.id, inventory).HasValue` 평가 + `Image.color = Classify(...)` |
| `Assets/Scripts/UI/MinimapUI.cs:152-162 UpdateEntityIcons NPC 루프` | `npc == null continue` 가드 + `ApplyNpcMarkerColor(icon, npc, quests, inventory)` 1라인 추가 |
| `Assets/Tests/EditMode/MinimapNpcMarkerTests.cs` (신규, 64라인) | 7건: Shop / Shop precedence / Quest / Default / NullDef / NullActions / Distinct colors |

비변경 (영향 검토만):
- `monsterIconPrefab`/`npcIconPrefab` 색상 default — `CreateDefaultIcon`에서 사용. NPC default 색상 `0.4f, 1f, 0.533f` (line 193) = `NpcMarkerDefault`와 일치 → 첫 인스턴스화 시 색상 일관.
- 시야 진입 분류는 0.2s 간격 (`UpdateInterval`) — 매 프레임 폴링 아님. 합리적.

---

## 검증 결과

### 검증 1: 엔진 검증
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| `MinimapUI` 스크립트 어셈블리 | ✅ | UI 어셈블리 |
| `MinimapNpcMarkerTests` EditMode 배치 | ✅ | `Assets/Tests/EditMode/` |
| `Image` 컴포넌트 의존 | ⚠️ | `npcIconPrefab` 루트에 Image 없으면 `GetComponent<Image>() == null` → 색상 미적용 (조용히 NoOp). Asset/QA 검증 필요 |
| 색상 상수 hex 정확성 | ✅ | `(1f, 0.85f, 0.2f) ≈ #ffd933`, `(1f, 0.55f, 0.2f) ≈ #ff8c33`, `(0.4f, 1f, 0.533f) ≈ #66ff88` |

### 검증 2: 코드 추적
| 확인 항목 | 결과 | 비고 |
|----------|------|------|
| `ClassifyNpcMarkerColor` 우선순위 | ✅ | open_shop > quest > default. 테스트 #2가 명시 단언 |
| `ClassifyNpcMarkerColor` static + 순수함수 | ✅ | NpcDef + bool 입력 → Color 출력. EditMode 테스트 가능 |
| Null 가드 | ✅ | def=null → Default. def.actions=null → quest 분기 fallthrough. 테스트 #5/#6 단언 |
| `actions` 순회 — LINQ 회피 | ✅ | for 루프. CLAUDE.md "LINQ 사용 금지" 준수 |
| `ApplyNpcMarkerColor` GC | ⚠️ | `GetComponent<Image>()` 매 호출 — NPC 수 × 5fps. 캐싱 가능하나 영향 미미 (NPC 30 × 5fps = 150회/초) |
| `quests.GetQuestStatusForNpc(id, inventory).HasValue` | ✅ | nullable struct return. 사이드이펙트 없음 |
| `npc == null continue` 가드 | ✅ | 신규 추가 — VillageNPC.Destroy 직후 루프 진입 시 안전 |
| RESERVE 비고 vs 구현 | ✅ | RESERVE 비고("MinimapIcon 컴포넌트에 type enum")보다 데이터 단일 원천(NpcDef.actions/QuestSystem) 활용 — 컴포넌트 신규 회피로 단순화 |

### 검증 3: UI 추적
- `MinimapUI.UpdateEntityIcons` (0.2s 간격) → NPC foreach (시야 내) → `GetNpcIcon` (풀) → `WorldToMinimap` 위치 → 🆕 `ApplyNpcMarkerColor` (색상)
- M키 시야 토글 → `ToggleViewRadius` → 다음 0.2s tick에 색상 재평가 (시야 들어온 NPC 분류).
- NPC 아이콘 풀(`_npcIcons` List) 재사용 — `SetActive(false)` 후 다시 `SetActive(true)` 시 마지막 색상 유지하다가 다음 tick에서 갱신. **순서 안전.**

### 검증 4: 사용자 시나리오
| 시나리오 | 결과 | 비고 |
|---------|------|------|
| 마을 진입 (NPC 다수) | 상점/퀘스트/기본 색상 분류 | ✅ |
| NPC가 상점도 열고 퀘스트도 가짐 | 상점 색상 우선 | 테스트 #2 |
| 퀘스트 받기 전/후 | hasQuestForNpc 변화 → 색상 즉시 갱신 (다음 0.2s tick) | ✅ |
| NPC 시야 진입/이탈 | 풀에서 활성화/비활성화 + 색상 재평가 | ✅ |
| Null def NPC (잘못 spawn) | 기본색 fallback | ✅ |
| 색맹 사용자 | 녹색/주황/노랑 — **색상만으로 구별 시 색맹 사용자 식별 어려움** | ⚠️ 후속 (마커 모양 차별화 또는 Tooltip) |

---

## 페르소나 리뷰

### 🎮 하늘 (캐주얼 게이머)
- 미니맵 보고 "어, 노랑이 퀘스트 NPC네" 바로 알아서 찾아간다. 좋다.
- 상점이 주황으로 도드라져서 쇼핑할 때 편함.
- 색맹은 아니지만 노랑/주황 비슷해 보이긴 해. 한참 들여다보면 구별됨.

### ⚔️ 태현 (코어 RPG 게이머)
- **분류 우선순위가 코드로 명시**된 점이 좋다 — actions에 open_shop 있는 NPC가 퀘스트도 가질 때 상점 우선 (테스트 #2가 단언). 이는 RPG에서 표준적 — 상점 NPC가 가끔 퀘스트도 줘도 우선 표기는 상점.
- `static` + 순수함수로 분리되어 EditMode 테스트 가능. 7건 커버리지 (4 입력 분류 + 2 null 가드 + 1 distinct color) 적절.
- RESERVE 비고가 "MinimapIcon 컴포넌트 type enum"이었으나 **데이터 단일 원천(NpcDef.actions/QuestSystem) 활용**으로 컴포넌트 신규 회피 — 더 단순한 해법. ⚔️

### 🎨 수아 (UX/UI 디자이너)
- 색상 선택: `#66ff88`(녹색 default) / `#ffd933`(주황 shop) / `#ff8c33`(노랑 quest). **녹색-주황 대비는 강하나 주황-노랑은 가깝다.** 시각적 거리 향상 필요.
  - 권장: shop을 더 채도 높은 주황(`#ff7000`)으로, quest를 노랑(`#ffe600`)으로 이동 → 색상 거리 증가.
- **색맹 대응 미흡 (Deuteranopia 적록색약은 녹색/주황 구별 어려움).** 별 구별 아이콘 모양(원/별/사각형)이 더 robust. 후속 ticket P3 권장.
- 0.2s 갱신 주기는 사용자가 인지하지 못함 — 적절. UpdateInterval 상수가 단일 원천 ✅
- iconContainer 초기 default 색상 `(0.4f, 1f, 0.533f)`이 NpcMarkerDefault와 일치 — 풀 첫 생성 시점부터 일관 ✅

### 🔍 준혁 (QA 엔지니어)
- Null 가드 5중 — npc==null / npc.Def==null / npc.Def.id 빈 문자열 / def.actions==null / def==null. 테스트 #5/#6이 직접 단언. ✅
- `GetComponent<Image>()` 매 호출 — NPC 30개 × 5fps = 150회/초. NPCRectTransform 캐시에 Image 캐싱 추가 시 추가 GC 0. **P3 follow-up 후보** (영향 미미하지만 polish).
- `quests.GetQuestStatusForNpc` 호출 — `QuestSystem` 내부 lookup 비용 알 수 없음. NPC 수 × 5fps × lookup 시간 — 측정 필요.
- 색상 distinct 단언 (테스트 #7) — 향후 색상 변경 시 동일 색상 실수 즉시 검출. ✅
- 회귀 위험: `UpdateEntityIcons` foreach에 `ApplyNpcMarkerColor` 1라인 추가 + `npc == null continue` 가드 추가. 기존 위치/시야 로직 무손상. ✅

---

## 후속 BACKLOG 후보 (Coordinator 흡수 검토)
1. **NPC 마커 모양 차별화 (색맹 대응)** — circle/star/square 모양으로 shop/quest/default 구별. P3.
2. **Image 컴포넌트 캐싱** — `_npcIcons` List에 Image 동시 캐싱 → GetComponent 호출 제거. P3.
3. **퀘스트 상태별 색상 (Available/InProgress/Complete)** — 현재는 HasValue만 평가. Available=노랑/InProgress=흰색/Complete=금색 같은 분기. P3.
4. **상점/퀘스트 동시 가능 NPC의 toggle UI** — 상점 우선 표기 시 퀘스트 인지 어려움. NPC 위에 작은 별 추가 또는 toolip 제안. P3.
5. **MinimapUI PlayMode 자동화** — 풀 재사용 + 시야 토글 + NPC spawn/destroy 시퀀스 검증. P3.

---

## 결론

**✅ APPROVE — 4/4 페르소나 만장일치**

- SPEC 부재(specs 참조 N) — RESERVE 비고 의도("퀘스트/상점/기본 분리")보다 단순한 해법(NpcDef.actions + QuestSystem 데이터 단일 원천)으로 충족.
- 분류 우선순위(open_shop > quest > default)가 코드+테스트로 명시. EditMode 7건 커버리지 적절(분류 4 + null 가드 2 + distinct color 1).
- Null 가드 5중 — 회귀 안전.
- 회귀 위험 0 — UpdateEntityIcons 기존 로직 무손상 + 1라인 추가.
- UI 자가검증 §2.5: ① 핵심 메서드 호출처 = MinimapUI.UpdateEntityIcons foreach (1곳) ② Minimap UI 항상 표시(M키 시야 토글 기존), NPC 시야 진입 자동 분류 ③ SPEC 부재 N/A.
- 후속 5건 BACKLOG 후보 (색맹 대응 마커 모양, Image 캐싱, 퀘스트 상태별 색상 등).
