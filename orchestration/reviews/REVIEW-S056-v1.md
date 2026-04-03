# REVIEW-S056-v1: GameManager 초기화 순서 보장 [깊은 리뷰]

> **리뷰 일시:** 2026-04-03
> **태스크:** S-056 GameManager 초기화 순서 감사
> **스펙:** SPEC-S-056
> **판정:** ✅ APPROVE

---

## 변경 요약

**변경 파일 (1개 핵심):**

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Core/GameManager.cs` | `_initialized` 가드 추가 (line 39 선언, line 112 설정, line 118 Update 가드) |

**범위:** 초기화 순서 감사 + Update() 가드 추가. 초기화 순서 자체는 변경 없음 — 기존 순차 패턴이 이미 올바름을 검증.

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 기존 참조 유지 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적 [깊은 리뷰 — 코드 직접 읽기]

### 초기화 순서 감사 결과

GameManager.Start()의 초기화 순서를 의존 그래프로 검증:

```
Layer 0: DataManager.LoadAll()
  ├─ Items, Skills, Monsters, NPCs, Quests, Regions, NpcProfiles
  └─ ValidateData()

Layer 1: 플레이어 시스템 (Data 의존)
  ├─ PlayerState
  ├─ PlayerEffects
  ├─ Inventory
  ├─ Skills(Data.Skills)
  ├─ Crafting(Data.Recipes, Data.Items)
  └─ Quests(Data.QuestList)

Layer 2: 월드 시스템 (Data + Player 의존)
  ├─ AI (async, fire-and-forget)
  ├─ RegionTracker(Data.RegionList)
  ├─ TimeSystem, Achievements, WorldEvents

Layer 3: 전투/NPC 시스템 (Player + Data 의존)
  ├─ PlayerState.RecalcStats()
  ├─ CombatRewardHandler(PlayerState, Inventory, Data)
  ├─ combatManager.Init(PlayerState)
  └─ DialogueController(AI, Quests, Data, Skills, PlayerState)

Layer 4: 월드 생성
  ├─ worldMap.Generate()
  ├─ InitMinimap()
  └─ SpawnInitialRegion()

Layer 5: 저장/UI
  ├─ SaveController 생성
  ├─ Quests.SubscribeEvents()
  ├─ GameUIWiring.WireAll()
  ├─ LoadGame() (세이브 존재 시)
  └─ uiWiring.PushInitialState()

Layer 6: _initialized = true
```

**의존 관계 검증:**
- ✅ DataManager (Layer 0) → 모든 시스템이 Data 이후 초기화
- ✅ PlayerState (Layer 1) → CombatManager.Init(), CombatRewardHandler (Layer 3) 이후
- ✅ Skills (Layer 1) → DialogueController (Layer 3) 이후
- ✅ 모든 시스템 초기화 → LoadGame() → UI 반영 순서 준수
- ✅ SaveSystem.Load()는 DataManager + 모든 시스템 초기화 이후 호출

**결론:** 기존 Start() 순서가 이미 올바른 의존 관계를 준수. 경합 조건 없음.

### _initialized 가드 (GameManager.cs)

**선언 (line 39):**
```csharp
bool _initialized;
```
- default false — Start() 완료 전 false 보장. **PASS**

**설정 (line 112):**
```csharp
_initialized = true;
```
- Start() 최하단, `uiWiring.PushInitialState()` 및 `PlayRegionBGM()` 이후. **PASS**
- 모든 시스템 초기화 + 세이브 로드 + UI 반영 완료 후 설정. **PASS**

**가드 (line 118):**
```csharp
void Update()
{
    if (!_initialized) return;
    if (player == null || PlayerState == null) return;
    if (player.Frozen) return;
    // ...
}
```
- 3중 가드: _initialized → null 체크 → Frozen 체크. **PASS**
- Start() 실행 중 Update()가 호출되는 것을 방지 (Unity는 동일 프레임에서 보장하나, 안전 장치). **PASS**

### 분리 클래스 간 경합 분석

| 클래스 | 생성 시점 | 초기화 | 경합 위험 |
|--------|----------|--------|-----------|
| GameManager | Awake (Singleton) | Start (순차) | ✅ 안전 |
| GameUIWiring | Start 내부 new | WireAll() 직후 | ✅ 안전 — Start 내 순차 |
| SaveController | Start 내부 new | 생성자 | ✅ 안전 — Start 내 순차 |
| CombatRewardHandler | Start 내부 new | 생성자 | ✅ 안전 — Start 내 순차 |
| DialogueController | Start 내부 new | 생성자 | ✅ 안전 — Start 내 순차 |

- 모든 분리 클래스가 GameManager.Start() 내부에서 순차 생성 — Awake/Start 비결정 문제 없음
- SPEC의 "B) 단일 진입점" 패턴에 해당 — GameManager.Start()가 모든 하위 시스템 순서대로 Init()

### async AI 초기화 참고

```csharp
_ = InitAISafe();  // fire-and-forget
```
- AI.Init()은 비동기이나 DialogueController가 null AI를 방어적으로 처리
- 게임플레이 중에만 AI 사용 (Start 완료 이후)
- **위험 낮음** — 현재 아키텍처에서 허용

### SaveSystem.Load() 순서 검증

```
SaveController.Load() 호출 순서:
1. SaveSystem.Load() → 파일 읽기
2. Player position 설정
3. PlayerState 복원 (Level, Xp, Gold, Stats)
4. Inventory 복원
5. Skills 복원
6. AI brains 복원
7. Quests 복원 (killProgress 포함)
8. state.RecalcStats(Data.Items, Data.SetBonuses)
9. Player speed 갱신
```

- ✅ DataManager 이미 초기화됨 (Layer 0)
- ✅ PlayerState, Inventory, Skills, Quests 모두 초기화 후 Load
- ✅ RecalcStats()에 Data.Items, Data.SetBonuses 전달 — Data 유효

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 입력 → 이벤트 → UI 반응 | ✅ | _initialized 가드로 미초기화 입력 처리 방지 |
| 패널 열기/닫기 | N/A | UI 변경 없음 |
| 데이터 바인딩 | ✅ | PushInitialState() → _initialized 순서 보장 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 콜드 스타트 (신규 게임) | 전 시스템 순차 초기화 → null 에러 0건 |
| 콜드 스타트 (세이브 로드) | 시스템 초기화 → LoadGame() → UI 반영 |
| 첫 프레임 Update() | _initialized false → 즉시 return → 크래시 없음 |
| 씬 전환 후 재초기화 | OnDestroy() → 새 씬 Start() → 순차 재초기화 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
"게임 켤 때 가끔 이상한 에러가 떴었는데, 이런 초기화 문제 때문이었구나. 이제 순서가 확실히 잡혔다니 안심."

### ⚔️ 코어 게이머
"단일 진입점(GameManager.Start)에서 모든 시스템을 순차 초기화하는 패턴이 가장 안전하다. ScriptExecutionOrder보다 명시적이고 유지보수가 쉽다. _initialized 가드가 Update()를 보호해서 초기화 도중 게임 루프가 돌지 않는다."

### 🎨 UX/UI 디자이너
"UI 관점에서 변화 없음. PushInitialState()가 _initialized 설정 직전에 호출되므로 초기 UI 상태가 확실히 반영된 후 게임 루프가 시작된다."

### 🔍 QA 엔지니어

| # | 체크 항목 | 결과 | 비고 |
|---|----------|------|------|
| 1 | _initialized 선언 | ✅ | bool, default false |
| 2 | _initialized 설정 시점 | ✅ | Start() 최하단 |
| 3 | Update() 가드 | ✅ | if (!_initialized) return |
| 4 | 의존 순서: Data → Systems | ✅ | DataManager.LoadAll() 최선두 |
| 5 | 의존 순서: Systems → SaveLoad | ✅ | LoadGame() Layer 5 |
| 6 | 의존 순서: SaveLoad → UI | ✅ | PushInitialState() 최후미 |
| 7 | 분리 클래스 경합 | ✅ | 전부 Start() 내 순차 생성 |
| 8 | AI async 안전 | ✅ | DialogueController null 방어 |
| 9 | Start() 재진입 방지 | ⚠️ | 미적용 — 현재 아키텍처에서 불필요 |
| 10 | 초기화 순서 테스트 | ⚠️ | 없음 — 통합 테스트 권장 (후속) |

**참고:** Start() 재진입 가드(`if (_initialized) return`)는 현재 GameManager가 DontDestroyOnLoad + Singleton 패턴이므로 Start()가 재호출되지 않음. 추가 불필요.

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 감사 완료 + _initialized 가드 추가 |
| 기존 호환성 | ✅ 초기화 순서 변경 없음, 가드만 추가 |
| 코드 품질 | ✅ 최소 변경 (3줄), 명확한 의도 |
| 아키텍처 | ✅ 단일 진입점 패턴 확인 (SPEC 권장 B안) |
| 테스트 커버리지 | ⚠️ 초기화 순서 통합 테스트 권장 (후속) |

**결론:** ✅ **APPROVE** — GameManager 초기화 순서 감사가 완료되어 기존 순차 패턴이 올바름을 확인. `_initialized` 가드가 Update()에 추가되어 초기화 미완료 시 게임 루프 실행을 방지. DataManager → 시스템 → 전투/NPC → 월드 생성 → 저장/UI 순서로 의존 관계가 정확하게 유지됨. 분리 클래스 간 경합 조건 없음. 코드 변경 최소(3줄)이며 안전 장치 역할만 수행. 승인.
