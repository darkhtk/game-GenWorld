# SPEC-S-129 — GameManager 싱글톤 null 체크 강화 (Awake 순서 의존)

> **타입:** Stabilization (P2, stabilize 방향 1순위 — 안정성 > 기존 기능 개선)
> **연관 RESERVE:** S-129 "GameManager 싱글톤 null 체크 강화 (Awake 순서 의존) — 씬 전환 직후 NRE 산발"
> **선제 작성:** Coordinator 9회차 (2026-04-30)
> **상태:** Draft (미할당)

---

## 1. 목적

`GameManager.Instance`(`Assets/Scripts/Core/GameManager.cs`) 는 MonoBehaviour 싱글톤이며 씬 진입 시 `Awake()`에서 `Instance = this` 로 등록된다. 그러나 **Awake 순서는 Unity가 보장하지 않음** — 다른 컴포넌트의 `Awake/Start/OnEnable` 에서 `GameManager.Instance.SomeSystem` 호출 시 null이 들어오는 NRE(NullReferenceException) 산발.

목격 패턴(SUPERVISOR.md / DEVELOPER.md 과거 회차 메모):
- 씬 전환 직후 ~50~150ms 동안 `GameManager.Instance == null` 윈도우 존재
- 일부 UI 컴포넌트(`OnEnable` 에서 `GameManager.Instance.Inventory` 참조) → NRE
- BOARD `S-087 RegionManager 씬 전환 중 입력 차단`(Done) 이후에도 잔존

본 태스크는 **모든 호출처에 null 가드 + Lazy access 패턴**을 도입하여 NRE 0건 보장.

## 2. 비목표 (Out of Scope)

- GameManager 자체를 일반 클래스로 변환(현재 MonoBehaviour 싱글톤 — CLAUDE.md `MonoBehaviour 싱글톤은 GameManager만 허용` 명시).
- DI(Dependency Injection) 도입 — 본 프로젝트 규모 대비 과잉.
- `Instance` 자체에 lock 추가(스레드 안전 — Unity 메인 스레드 단일).
- 다른 싱글톤 패턴 적용(현재 GameManager 만 MonoBehaviour 싱글톤).

## 3. 호출 진입점

| 진입점 | 경로 | 위험도 |
|--------|------|--------|
| 컴포넌트 `Awake()` 에서 `GameManager.Instance.X` | 모든 MonoBehaviour | **HIGH** — 순서 보장 X |
| 컴포넌트 `OnEnable()` 에서 `GameManager.Instance.X` | UI 패널, 이펙트 | **HIGH** — 씬 재진입 시 |
| 컴포넌트 `Start()` 에서 `GameManager.Instance.X` | 일반 시스템 | **MEDIUM** — Awake 후 보장 |
| 이벤트 핸들러에서 `GameManager.Instance.X` | EventBus subscribe | **LOW** — Subscribe 시점에 GameManager 존재 |
| `Update()` 에서 `GameManager.Instance.X` | 매 프레임 | **LOW** — Awake 완료 후 |

**현재 grep 결과(예상):** `GameManager.Instance.` 직접 참조 ~30+곳. 본 SPEC은 **Awake/OnEnable** 호출처 우선 가드.

## 4. 변경 사항

### 4.1 GameManager 측 변경

`Assets/Scripts/Core/GameManager.cs` 에 다음 추가:

```csharp
// Existing:
public static GameManager Instance { get; private set; }

// Add:
public static bool IsReady => Instance != null && Instance._isReadyInternal;
private bool _isReadyInternal = false;

private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    // ... existing system initialization ...

    _isReadyInternal = true; // 모든 시스템 초기화 완료 후 마지막
}

private void OnDestroy()
{
    if (Instance == this)
    {
        _isReadyInternal = false;
        Instance = null;
    }
}

// Helper for safe access (optional):
public static T SafeGet<T>(System.Func<GameManager, T> accessor) where T : class
{
    if (!IsReady) return null;
    return accessor(Instance);
}
```

### 4.2 호출처 변경 패턴

#### Pattern A — Awake/OnEnable (HIGH 위험)
```csharp
// Before:
private void OnEnable()
{
    _inventory = GameManager.Instance.Inventory; // NRE 위험
    Refresh();
}

// After:
private void OnEnable()
{
    if (!GameManager.IsReady) return; // Defer to Start or external Refresh
    _inventory = GameManager.Instance.Inventory;
    Refresh();
}
```

#### Pattern B — Start (MEDIUM 위험)
```csharp
// Before:
private void Start()
{
    GameManager.Instance.Quests.OnQuestCompleted += HandleCompleted;
}

// After:
private void Start()
{
    if (GameManager.Instance?.Quests == null) return; // Null-conditional 가드
    GameManager.Instance.Quests.OnQuestCompleted += HandleCompleted;
}
```

#### Pattern C — Lazy Subscribe (재시도 패턴, 필요 시)
```csharp
// Awake/OnEnable 에서 직접 참조 못할 때:
private System.Collections.IEnumerator WaitForGameManager()
{
    while (!GameManager.IsReady) yield return null; // 1프레임씩 대기
    _inventory = GameManager.Instance.Inventory;
    Refresh();
}

private void OnEnable()
{
    StartCoroutine(WaitForGameManager());
}
```

## 5. 영향 범위 (예상 grep 결과)

`grep -n "GameManager.Instance" Assets/Scripts/` 호출처를 **위험도별 분류** 후 우선순위 적용:

| 위험도 | 호출처 패턴 | 가드 패턴 | 예상 건수 |
|--------|------------|----------|----------|
| HIGH | `Awake() { GameManager.Instance.* }` | Pattern A 또는 Pattern C | ~5~10 |
| HIGH | `OnEnable() { GameManager.Instance.* }` | Pattern A 또는 Pattern C | ~10~15 |
| MEDIUM | `Start() { GameManager.Instance.* }` | Pattern B (`?.`) | ~10~20 |
| LOW | `Update()`/이벤트 핸들러 | 무변경 (Awake 완료 후) | 변경 0 |

**우선순위:** HIGH 먼저, MEDIUM 다음. LOW는 본 SPEC 비목표(별도 후속).

## 6. 권장 분할 (옵션)

본 SPEC은 변경 범위가 클 수 있어 **단일 PR이 무겁다면 분할 권장**:

- **Phase 1:** `IsReady` 프로퍼티 + `_isReadyInternal` 도입 + Helper 메서드 → 인프라만
- **Phase 2:** HIGH 위험 호출처(Awake/OnEnable) 가드 적용 → 즉효성
- **Phase 3:** MEDIUM 호출처(Start) 가드 적용 → 회귀 안정화

각 phase마다 EditMode 회귀 0 확인 후 진행. 단일 PR로도 가능(영향 범위 그렙 후 판단).

## 7. EditMode 테스트 (필수)

`Assets/Tests/EditMode/GameManagerSafetyTests.cs` 신규:

1. **`IsReady_DefaultsFalse_BeforeAwake`** — `new GameObject()` 에 `GameManager` 컴포넌트 추가만 하고 `Awake` 미호출 시 `IsReady == false`.
2. **`IsReady_TrueAfterAwakeCompletes`** — `Awake()` 호출 시뮬레이션 후 `IsReady == true`.
3. **`IsReady_FalseAfterOnDestroy`** — `OnDestroy()` 호출 후 `IsReady == false` + `Instance == null`.
4. **`SafeGet_NullWhenNotReady`** — `IsReady == false` 상태에서 `SafeGet(gm => gm.Inventory)` → `null`.
5. **`SafeGet_ReturnsValueWhenReady`** — `IsReady == true` 후 `SafeGet(gm => gm.Inventory)` → 실제 인스턴스.

**Mocking 한계:** GameManager는 MonoBehaviour 싱글톤 + `DontDestroyOnLoad` 호출 — EditMode 직접 인스턴스화 어려움. 테스트는 **Unity Reflection 또는 SerializeReference 우회** 또는 **PlayMode 분리**(검토). EditMode 가능 한도까지만.

## 8. 회귀 위험

- ⚠️ `GameManager.Instance` 가 null 일 때 `OnEnable` 에서 `return` 하면 **그 컴포넌트의 초기화가 영영 안 될 수 있음** — 외부 Refresh 호출 또는 Pattern C(WaitForGameManager 코루틴) 필요.
- ⚠️ HIGH 위험 호출처를 모두 가드 적용 시 **초기 1~2프레임 동안 UI/이펙트 비활성** — 검수 필수.
- ⚠️ 호출처 30+ 곳 일괄 수정 시 **PR diff 비대** → Phase 분할 권장.

## 9. 비변경 영역

- GameManager 의 시스템 인스턴스(`Inventory`, `Quests`, `Audio` 등) 자체는 변경 X.
- `EventBus` 사용 패턴 변경 X.
- 다른 싱글톤 패턴(있을 시) 변경 X.

## 10. DoD (Definition of Done)

1. `GameManager.IsReady` 프로퍼티 + `_isReadyInternal` 필드 추가.
2. HIGH 위험 호출처(Awake/OnEnable) 가드 적용.
3. EditMode 테스트 5건 추가 + 모두 통과.
4. `grep -n "GameManager.Instance" Assets/Scripts/` 결과 변경 통계 로그에 기록(원본 N건 → 가드 적용 M건).
5. PlayMode 수동 검증: 메인 메뉴 → 게임 시작 → 마을 진입 → 던전 진입 → 회귀 → 메인 메뉴(이상 시퀀스 NRE 0건 / Console error 0건).
6. 컴파일 에러 0 / 경고 0(`error CS` / `warning CS` grep).

## 11. 우선순위 / 일정

- **우선순위:** P2 (stabilize 방향 1순위)
- **권장 일정:** Phase 1 단일 회차(인프라만), Phase 2/3 후속 회차(HIGH/MEDIUM 호출처 분할 적용).
- **단일 회차 가능:** Phase 1+2 함께 처리(영향 범위 30+ 호출처라도 단순 가드 패턴 일관 적용 시 단일 회차 충분).
- **블록:** 없음.

## 12. 후속 SPEC 후보

- **SPEC-S-129b:** MEDIUM 위험 호출처(Start) 일괄 가드 — 본 SPEC Phase 3 분할 시.
- **SPEC-S-129c:** GameManager 시스템 inner-null 가드(`Inventory != null` 등) — 본 SPEC은 Instance 자체만, 시스템 inner-null은 별도.
- **GameManager 시스템 초기화 순서 명문화** — `Awake()` 안 시스템 생성 순서가 함수 호출 순서로 결정되는데 의존 그래프 명시 필요.

---

**작성:** Coordinator 9회차 (2026-04-30)
**대조:** stabilize 우선순위 1순위 + 안정성 > 기존 기능 개선 > 신규 기능 + RESERVE 비고("씬 전환 직후 NRE 산발") 직접 매칭.
**호출 진입점 명시 §3.** 진입 위험도 분류 §3 + §5.
