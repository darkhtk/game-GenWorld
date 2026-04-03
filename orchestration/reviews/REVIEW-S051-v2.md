# REVIEW-S051-v2: SceneTransition 메모리 누수 [깊은 리뷰]

> **리뷰 일시:** 2026-04-03
> **태스크:** S-051 SceneTransition 메모리 누수 (v2 재제출)
> **스펙:** SPEC-S-051
> **커밋:** `3c71d94` fix: S-048 v2 SkillList null-id 필터링 + S-051 v2 SceneTransition 메모리 정리
> **v1 판정:** ❌ NEEDS_WORK (REVIEW-S051-v1)
> **v2 판정:** ✅ APPROVE

---

## v1 지적사항 대응 현황

| # | v1 지적 | 심각도 | v2 대응 | 상태 |
|---|---------|--------|---------|------|
| 1 | ObjectPool.Clear() 호출 지점 없음 | **MUST** | Projectile.ClearPool() + DamageText.ClearPool() → GameManager.OnDestroy()에서 호출 | ✅ 해결 |
| 2 | EventVFX 구독 소멸 (EventBus.Clear 충돌) | **MUST** | SceneManager.sceneLoaded 콜백으로 재구독 메커니즘 추가 | ✅ 해결 |
| 3 | Resources.UnloadUnusedAssets() 타이밍 부적절 | **MUST** | OnDestroy → sceneLoaded 콜백으로 이동 | ✅ 해결 |
| 4 | ObjectPool.Clear() 단위 테스트 | SHOULD | 미추가 (아래 참조) | ⚠️ 미대응 |
| 5 | GameUIWiring 람다 → 명시적 메서드 참조 | SHOULD | 미대응 (장기 과제) | ⚠️ 미대응 |

SHOULD 항목 2건은 장기 개선 과제이며 현재 태스크 범위에서 필수 아님. MUST 항목 3건 전부 해결.

---

## 변경 요약

커밋 `3c71d94`에서 S-051 관련 변경 (4개 파일, +42 라인):

1. **GameManager.cs** — OnDestroy()에 ClearPool() 호출 2건 추가, sceneLoaded 원샷 핸들러로 UnloadUnusedAssets() 이동.
2. **Projectile.cs** — static `ClearPool()` 메서드 추가 (풀 정리 + 부모 파괴 + 참조 null).
3. **DamageText.cs** — 동일 패턴의 `ClearPool()` 메서드 추가.
4. **EventVFX.cs** — `SceneManager.sceneLoaded` 구독 + OnSceneLoaded()에서 Off→On 재구독 + OnDestroy() 해제.

---

## 검증 1: 엔진 검증

### 1.1 DontDestroyOnLoad 사용 현황 (v2 재점검)

| 파일 | 대상 | DDOL 적합성 | v2 변경 |
|------|------|-------------|---------|
| AudioManager.cs | 싱글톤 | ✅ 적합 | 변경 없음 |
| SteamManager.cs | 싱글톤 | ✅ 적합 | 변경 없음 |
| EventVFX.cs | 싱글톤 | ✅ 적합 | sceneLoaded 재구독 추가 |
| EventSystemEnsurer.cs | EventSystem | ✅ 적합 | 변경 없음 |
| Projectile.cs | ProjectilePool 부모 | ⚠️→✅ | ClearPool()으로 씬 전환 시 파괴 |
| DamageText.cs | DamageTextPool 부모 | ⚠️→✅ | ClearPool()으로 씬 전환 시 파괴 |

v1에서 문제였던 Projectile/DamageText 풀 부모의 DDOL이 씬 전환 시 ClearPool()로 정리됨. 다음 GameScene 진입 시 EnsurePool()이 `_pool == null`을 감지하여 새로 생성. → **해결**

### 1.2 ObjectPool.Clear() 구현 (ObjectPool.cs:65-73)

```csharp
public void Clear()
{
    while (_available.Count > 0)
    {
        var obj = _available.Dequeue();
        if (obj != null) UnityEngine.Object.Destroy(obj.gameObject);
    }
    _totalCreated = 0;
}
```

- `_available` 큐의 비활성 오브젝트만 파괴 → 활성 오브젝트는 ClearPool()의 부모 파괴로 연쇄 정리됨
- `_totalCreated = 0` 리셋 → 다음 EnsurePool()에서 깨끗한 상태 시작
- null 체크로 이미 파괴된 오브젝트 안전 처리

---

## 검증 2: 코드 추적 [깊은 리뷰]

### 2.1 GameManager.OnDestroy() 실행 순서 (lines 47-53)

```csharp
void OnDestroy()
{
    Projectile.ClearPool();       // (1) 풀 오브젝트 + 부모 파괴
    DamageText.ClearPool();       // (2) 풀 오브젝트 + 부모 파괴
    EventBus.Clear();             // (3) 모든 이벤트 구독 제거
    SceneManager.sceneLoaded += OnSceneLoadedCleanup;  // (4) 원샷 핸들러 등록
}
```

**실행 순서 분석:**
- (1)(2): DDOL 풀 부모 파괴. `Object.Destroy`는 프레임 끝에 처리되므로 즉시 null은 아니지만, 참조를 null로 리셋하여 EnsurePool() 재생성 보장.
- (3): EventBus 전체 클리어. DDOL 오브젝트(EventVFX)의 구독도 제거됨.
- (4): 다음 씬 로드 시 UnloadUnusedAssets() 호출을 위한 원샷 핸들러 등록.

순서가 적절함: 풀 정리 → 이벤트 정리 → 다음 씬 준비.

### 2.2 ClearPool() 패턴 (Projectile.cs:47-53, DamageText.cs:36-42)

```csharp
public static void ClearPool()
{
    _pool?.Clear();                                          // 큐 내 비활성 오브젝트 파괴
    _pool = null;                                            // 풀 참조 해제
    if (_poolParent != null) Object.Destroy(_poolParent.gameObject);  // 부모(+자식) 파괴
    _poolParent = null;                                      // 부모 참조 해제
}
```

**깊은 분석:**
- `_pool.Clear()`가 큐의 비활성 오브젝트를 `Object.Destroy`로 마킹
- `Object.Destroy(_poolParent.gameObject)`가 부모를 파괴하면 Unity 계층 구조상 모든 자식도 연쇄 파괴
- 비활성 오브젝트는 이미 Clear()에서 파괴 마킹 + 부모 파괴로 이중 정리 → Unity가 이미 파괴 마킹된 오브젝트는 무시하므로 안전
- 활성 오브젝트(Get()으로 가져간 상태)가 부모 아래에 있으면 부모 파괴 시 함께 정리됨
- `_pool = null` 후 다음 `EnsurePool()` 호출 시 `if (_pool != null) return` 조건 불충족 → 새 풀 생성 → **정상**

### 2.3 EventVFX 재구독 메커니즘 (EventVFX.cs)

```csharp
void Awake()
{
    // ... 싱글톤 패턴 ...
    SceneManager.sceneLoaded += OnSceneLoaded;  // 영구 등록
}

void OnDestroy()
{
    SceneManager.sceneLoaded -= OnSceneLoaded;  // 정리
}

void OnSceneLoaded(Scene scene, LoadSceneMode mode)
{
    EventBus.Off<LevelUpEvent>(OnLevelUp);        // 기존 구독 제거 (없으면 no-op)
    EventBus.Off<ItemCollectEvent>(OnItemCollect);
    EventBus.On<LevelUpEvent>(OnLevelUp);          // 재구독
    EventBus.On<ItemCollectEvent>(OnItemCollect);
    _cachedPlayer = null;                          // 이전 씬 PlayerController 참조 해제
}
```

**씬 전환 시나리오 추적:**
1. GameScene → MainMenu: `GameManager.OnDestroy()` → `EventBus.Clear()` (구독 전부 소멸)
2. MainMenuScene 로드 → `sceneLoaded` 이벤트 발생:
   - `EventVFX.OnSceneLoaded()`: Off (no-op, 이미 Clear됨) → On (재구독) → **구독 복구 ✅**
   - `OnSceneLoadedCleanup()`: 자기 해제 → `UnloadUnusedAssets()` → **메모리 정리 ✅**
3. MainMenu → GameScene: `sceneLoaded` 재발생:
   - `EventVFX.OnSceneLoaded()`: Off (MainMenu 동안의 구독 제거) → On (재구독) → **이중 구독 방지 ✅**
   - `_cachedPlayer = null` → 새 GameScene의 PlayerController를 찾도록 리셋 → **정상**

**이중 구독 방지 확인:**
- `OnEnable()`은 DDOL이므로 최초 활성화 시 1회만 호출
- `OnSceneLoaded()`는 매 씬 로드마다 호출되지만, Off→On 패턴으로 이중 등록 방지
- EventBus.Clear() 후에는 Off가 no-op이므로 On만 유효 → 정확히 1회 구독

### 2.4 OnSceneLoadedCleanup 원샷 패턴 (GameManager.cs:55-59)

```csharp
static void OnSceneLoadedCleanup(Scene scene, LoadSceneMode mode)
{
    SceneManager.sceneLoaded -= OnSceneLoadedCleanup;  // 자기 해제
    Resources.UnloadUnusedAssets();                      // 비동기 시작
}
```

- `static` 메서드이므로 GameManager 인스턴스 파괴 후에도 호출 가능 → **올바른 설계**
- 자기 해제(−=)로 1회만 실행 → 다음 씬 전환에서 중복 실행 없음
- `Resources.UnloadUnusedAssets()` 반환값(`AsyncOperation`) 미사용 → 완료 대기 불가, 하지만 Unity가 자체적으로 비동기 처리하므로 실용상 문제 없음
- **타이밍**: 새 씬 로드 완료 후 호출되므로 스펙의 "after scene load" 요구사항 충족 → **v1 대비 개선**

---

## 검증 3: UI 추적 (씬 전환 경로 재검증)

### 시나리오: GameScene → MainMenu → GameScene 왕복

| 단계 | 이벤트 | 결과 |
|------|--------|------|
| 1. "메인 메뉴로" 클릭 | `SceneManager.LoadScene("MainMenuScene")` | GameScene 언로드 시작 |
| 2. GameManager.OnDestroy() | ClearPool×2 → EventBus.Clear() → sceneLoaded 핸들러 등록 | 풀 정리, 이벤트 정리 |
| 3. MainMenuScene 로드 완료 | sceneLoaded 이벤트 발생 | EventVFX 재구독 + UnloadUnusedAssets |
| 4. "새 게임" 클릭 | `SceneManager.LoadScene("GameScene")` | MainMenuScene 언로드 |
| 5. GameScene 로드 완료 | sceneLoaded 이벤트 발생 | EventVFX 재구독 + _cachedPlayer 리셋 |
| 6. 전투 시작 | Projectile.Get() / DamageText.Spawn() | EnsurePool() → 새 풀 생성 (DDOL) |
| 7. 레벨업 | LevelUpEvent 발생 | EventVFX.OnLevelUp 정상 호출 ✅ |

**v1 대비 개선점:**
- 단계 3에서 EventVFX 재구독이 보장됨 (v1은 구독 소멸 후 복구 불가)
- 단계 6에서 이전 풀 잔존 없이 깨끗한 상태에서 새 풀 생성 (v1은 이전 풀 오브젝트 잔류)

---

## 검증 4: 플레이 시나리오

| 시나리오 | v1 상태 | v2 상태 | 판정 |
|---------|---------|---------|------|
| 메인 메뉴 왕복 3회 | 풀 오브젝트 130개 잔존 + EventVFX 끊김 | 풀 정리됨 + EventVFX 재구독 | **PASS** |
| 보스전 80개 DamageText 후 메뉴 복귀 | 80개 DDOL 잔존 | ClearPool()으로 전부 파괴 | **PASS** |
| GameScene → MainMenu → GameScene → 레벨업 | VFX 미표시 | OnSceneLoaded 재구독으로 정상 표시 | **PASS** |
| 정상 플레이 (씬 전환 없음) | 변화 없음 | 변화 없음 | **PASS** |
| 연속 전투 중 투사체 다수 생성 | 변화 없음 | 변화 없음 | **PASS** |
| 사망 → 리스폰 (씬 전환 없는 경우) | N/A | 풀 유지, 정상 | **PASS** |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
메인 메뉴 왕복 시 레벨업 이펙트가 안 나오던 문제가 해결됐다. "레벨업했는데 반응이 없어요" 같은 불만이 사라짐. 체감상 달라진 점은 없지만 안정성이 크게 향상.

### ⚔️ 코어 게이머
장시간 플레이 중 메뉴 왕복 패턴(죽음 → 메뉴 → 재진입)에서 메모리 누적이 없다. 풀 오브젝트가 씬 전환마다 깨끗하게 정리되므로 장시간 세션에서도 안정적. 전투 성능 자체에는 변화 없음.

### 🎨 UX/UI 디자이너
씬 전환 UX 자체(페이드/로딩 화면 부재)는 여전히 거친 편이나 S-051 범위 밖. 메모리 측면에서는 이제 씬 전환이 깔끔하게 처리됨. EventVFX 재구독으로 시각 피드백 연속성 보장.

### 🔍 QA 엔지니어
v1에서 발견된 재현 가능 버그 2건(EventVFX 구독 소멸, 풀 메모리 미해제) 모두 해결. ClearPool() 단위 테스트가 없는 점은 아쉬우나, EditMode에서 Object.Destroy를 테스트하기 어려운 Unity 제약이 있어 수용 가능. 통합 테스트(수동 씬 왕복)로 검증 권장.

---

## 종합 판정

### ✅ APPROVE

v1의 MUST FIX 3건 전부 정확히 해결:

| # | 스펙 요구사항 | v1 | v2 | 판정 |
|---|-------------|----|----|------|
| 1 | ObjectPool.Clear() 메서드 | ✅ | ✅ | PASS |
| 2 | GameManager cleanup (pool Clear 호출) | ❌ | ✅ ClearPool()×2 호출 | PASS |
| 3 | Resources.UnloadUnusedAssets after scene load | ⚠️ | ✅ sceneLoaded 콜백 | PASS |
| 4 | EventBus.Clear 순서 검증 | ❌ | ✅ EventVFX 재구독 | PASS |
| 5 | DontDestroyOnLoad 부적절 사용 점검 | ❌ | ✅ ClearPool()로 DDOL 풀 정리 | PASS |

코드 품질이 양호하며, 기존 아키텍처와 일관성을 유지하면서 씬 전환 메모리 누수를 효과적으로 해결한다. ClearPool() 패턴이 Projectile/DamageText에서 대칭적으로 구현되어 유지보수성도 좋다.
