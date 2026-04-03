# REVIEW-S006-v1: GameManager 분할 리팩토링 [깊은 리뷰]

> **리뷰 일시:** 2026-04-03
> **태스크:** S-006 GameManager 분할 리팩토링
> **스펙:** 없음
> **판정:** ✅ APPROVE

---

## 변경 요약

**928줄 → 308줄 GameManager + 4개 클래스 분리:**

| 파일 | 줄 수 | 책임 |
|------|-------|------|
| GameManager.cs | 308 | 초기화, 게임 루프, 카메라, 리전 전환 |
| CombatRewardHandler.cs | 123 | 몬스터 처치 보상, XP/골드/드롭 처리 |
| DialogueController.cs | 278 | NPC 등록, 대화 생성, 액션 처리 |
| GameUIWiring.cs | 281 | UI 콜백 연결, 이벤트 구독 |
| SaveController.cs | 69 | 세이브/로드 직렬화 |

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 기존 SerializeField 유지, 새 클래스는 순수 C# |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적 [깊은 리뷰]

| 항목 | 결과 | 비고 |
|------|------|------|
| TASK 명세 부합 | ✅ | God class 분리 — 4개 SRP 클래스로 추출 |
| 기존 코드 호환 | ✅ | 동작 변경 없음 — 로직 1:1 이전 |
| 아키텍처 패턴 | ✅ | 순수 C# 클래스, 생성자 주입, MonoBehaviour 아님 |
| 테스트 커버리지 | ⚠️ | 리팩토링 전용 태스크로 미작성 |

### 코드 직접 읽기 분석

**GameManager.cs (308줄):**
- Start()에서 4개 클래스를 생성하고 조합. 오케스트레이터 역할만 남음.
- Update()는 몬스터 AI, 전투, 이펙트, 리전 전환 루프 유지.
- LateUpdate()는 카메라 추적 + 줌 유지.
- 308줄: 300줄 가이드라인 약간 초과하지만 928줄에서 대폭 개선.

**CombatRewardHandler.cs (123줄):**
- OnMonsterKilled: XP 부여 → 레벨업 체크 → 골드 → 플로팅 텍스트 → 드롭 → EventBus 방출 → 스포너 제거.
- OnPlayerDeath: 골드 패널티 → 힐 → 이벤트 방출.
- 발견: `monster.DeathProcessed = true` 이중 설정 (GameManager:135 + CombatRewardHandler:39). **무해하지만 중복.**

**DialogueController.cs (278줄):**
- RegisterNpcs(): NPC 프리팹 생성 + AI 브레인 등록.
- TryInteract(): 가장 가까운 NPC 찾기 → 대화 시작 → 콜백 설정.
- HandleDialogueResponse(): async AI 대화 생성 → 옵션 표시 → 퀘스트 제안.
- `_ = HandleDialogueResponse(...)` fire-and-forget: try-catch로 방어됨 (S-003 확인 완료).
- BuildLoreContext(): 로어 파일 lazy 캐시. 인스턴스 수명 동안 유지 — 적절.

**GameUIWiring.cs (281줄):**
- WireAll()에서 포션/인벤토리/스킬트리/대화/일시정지/오디오/이벤트 전부 연결.
- 주의점: WireDialogueCallbacks():143에서 dlg.OnClose 기본값 설정 → TryInteract():127에서 오버라이드. 의도된 패턴이나 암묵적 의존.
- SubscribeEvents(): EventBus 구독 일원화 — 가독성 우수.
- PushInitialState(): 초기 HUD 동기화.

**SaveController.cs (69줄):**
- Save/Load 메서드 각 1개. 깔끔한 추출.
- null 체크: save == null early return. inventory/equipment/skills 등 개별 null 가드.

### 의존성 분석

| 클래스 | 생성자 파라미터 수 | 평가 |
|--------|---------------------|------|
| CombatRewardHandler | 9 | God class에서 추출 — 파라미터 수는 원래 복잡도 반영 |
| DialogueController | 9 | 동일 |
| GameUIWiring | 10 | 동일 |
| SaveController | 0 (메서드 파라미터) | 가장 깔끔한 추출 |

파라미터 객체(DTO) 도입은 가능하나 현 단계에서 과도한 추상화. 현재로 충분.

### 잠재적 이슈

1. **DeathProcessed 이중 설정** (경미): GameManager:135 → CombatRewardHandler:39. 무해하지만 GameManager의 설정을 제거하면 더 명확.
2. **OnClose 오버라이드 패턴** (주의): GameUIWiring이 기본 OnClose를 설정하고 DialogueController가 대화 시 오버라이드. 동작 정상이나 암묵적.
3. **생성자 파라미터 수** (관찰): 9~10개는 많지만 God class 분리 첫 단계로 합리적.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 입력 → 이벤트 → UI 반응 | ✅ | GameUIWiring에서 일원 관리, EventBus 경로 유지 |
| 패널 열기/닫기 | ✅ | SetDialogueOpen, Frozen 플래그 양쪽 모두 처리 |
| 데이터 바인딩 | ✅ | PushInitialState + RefreshHud + 이벤트 콜백 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 게임 시작 | GameManager.Start → 4개 클래스 생성 → UI 연결 → 세이브 로드 |
| 몬스터 처치 | CombatRewardHandler.OnMonsterKilled → XP/골드/드롭 → 이벤트 |
| NPC 대화 | F키 → DialogueController.TryInteract → AI 대화 → 퀘스트 |
| 인벤토리 조작 | GameUIWiring 콜백 → 장비 장착/해제 → 스탯 재계산 |
| 세이브/로드 | SaveController.Save/Load → 기존과 동일한 직렬화 |
| 플레이어 사망 | CombatRewardHandler.OnPlayerDeath → 골드 패널티 → 풀힐 |
| 리전 전환 | GameManager.HandleRegionTransition → 몬스터 스폰 → BGM 변경 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
리팩토링이라 게임 동작이 달라지면 안 된다. 코드를 보면 기존 로직을 그대로 옮긴 거라 플레이어 입장에서 차이 없을 것 같다. 좋다.

### ⚔️ 코어 게이머
GameManager가 928줄짜리 God class였으면 버그 하나 잡으려 해도 스크롤이 끝없었을 거다. 전투 보상(CombatRewardHandler), 대화(DialogueController), UI 연결(GameUIWiring), 세이브(SaveController)로 분리한 건 논리적으로 맞다. 각 클래스가 자기 영역만 책임진다. 향후 전투 밸런스 조정 시 CombatRewardHandler만 보면 되니까 유지보수성이 크게 올랐다.

### 🎨 UX/UI 디자이너
GameUIWiring이 모든 UI 콜백을 한 곳에서 관리하는 건 좋다. 기존에 GameManager 여기저기 흩어져 있던 OnEquipCallback, OnLearnSkill 같은 콜백을 찾기가 훨씬 쉬워졌다. PushInitialState()로 초기 HUD 상태를 명확하게 밀어넣는 것도 깔끔하다. OnClose 오버라이드 패턴만 약간 주의가 필요하지만 기능적으로 문제 없다.

### 🔍 QA 엔지니어

| 체크 | 결과 |
|------|------|
| 동작 변경 없음 | ✅ 로직 1:1 이전 확인 |
| null 안전성 | ✅ 기존 null 가드 모두 유지 (uiManager, combatManager, dlg 등) |
| async 방어 | ✅ HandleDialogueResponse try-catch, InitAISafe try-catch |
| EventBus 구독 | ✅ OnDestroy에서 EventBus.Clear() 유지 |
| 메모리 누수 | ✅ 순수 C# 클래스 — GC 수집 대상 |
| 성능 영향 | ✅ 없음 — 함수 호출 1단계 추가뿐 |
| DeathProcessed 이중 설정 | ⚠️ 무해하지만 중복 (정리 가능) |
| 300줄 가이드라인 | ⚠️ GameManager 308줄 — 근소 초과 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 928줄 God class → 4개 SRP 클래스 + 308줄 오케스트레이터 |
| 기존 호환성 | ✅ 동작 변경 없음, 모든 기존 기능 보존 |
| 코드 품질 | ✅ 순수 C# 클래스, 생성자 주입, 300줄 이하(근소 초과) |
| 아키텍처 | ✅ CLAUDE.md Systems 패턴 준수 |

**결론:** ✅ **APPROVE** — GameManager God class(928줄)를 4개의 단일 책임 클래스로 깔끔하게 분리. 동작 변경 없이 유지보수성과 가독성을 크게 개선. DeathProcessed 이중 설정(경미)과 OnClose 오버라이드 패턴(주의)은 향후 정리 가능하나 차단 사유 아님.

**참고 사항:**
- DeathProcessed 이중 설정: GameManager:135의 `m.DeathProcessed = true`를 제거하면 더 명확 (CombatRewardHandler가 이미 설정).
- GameManager 308줄: 300줄 가이드라인 약간 초과. 향후 카메라/줌 로직 분리 시 해소 가능.
