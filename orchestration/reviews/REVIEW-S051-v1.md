# REVIEW-S051-v1: SceneTransition 메모리 누수

> **리뷰 일시:** 2026-04-03
> **태스크:** S-051 SceneTransition 메모리 누수
> **스펙:** SPEC-S-051
> **커밋:** `e65e5da` fix: S-051 SceneTransition 메모리 — ObjectPool.Clear 추가 + OnDestroy UnloadUnusedAssets
> **판정:** ❌ NEEDS_WORK

---

## 변경 요약

커밋 `e65e5da`에서 두 가지 변경이 이루어졌다:

1. **ObjectPool.cs** — `Clear()` 메서드 신규 추가 (65~73행). 큐에 남은 모든 오브젝트를 `Dequeue` 후 `Object.Destroy(obj.gameObject)` 호출, `_totalCreated = 0` 리셋.
2. **GameManager.cs** — `OnDestroy()`에 `Resources.UnloadUnusedAssets()` 1줄 추가 (기존 `EventBus.Clear()` 다음).

변경 파일 2개, 총 +16/-3 라인.

---

## 검증 1: 엔진 검증 (ObjectPool.Clear 구현)

### 확인 사항
- `Clear()` 메서드는 `_available` 큐를 순회하며 `Object.Destroy`로 파괴 후 `_totalCreated = 0` 리셋 -- **구현 자체는 정확하다.**
- 스펙에서 요구한 `(obj as MonoBehaviour)?.gameObject` 캐스팅 대신 `obj.gameObject`를 직접 사용 -- `T : Component` 제약 덕분에 더 간결하고 올바른 접근.

### 문제점
- **큐에 반환되지 않은 활성 오브젝트는 Clear 대상이 아니다.** `_available`에 들어있는 비활성 오브젝트만 파괴되고, 현재 Get()으로 꺼내서 씬에서 활성 상태인 오브젝트는 남는다. `DontDestroyOnLoad` 풀 부모의 자식이 아닌 경우(예: `transform.SetParent(null)` 호출 후) 씬 언로드로 자동 파괴되지만, 풀 부모가 DDOL이면 해당 부모 아래로 Return된 오브젝트는 계속 생존한다.
- 심각도: 중간. 대부분의 활성 투사체/데미지텍스트는 짧은 수명이므로 실질적 누수 규모는 작지만, 원칙적으로 완전한 정리가 아니다.

---

## 검증 2: 코드 추적 (Clear 호출 경로 부재)

### 치명적 문제: ObjectPool.Clear()가 어디에서도 호출되지 않는다

코드베이스 전체를 `pool.Clear`, `Pool.Clear`, `_pool.Clear` 패턴으로 검색한 결과 **호출 지점이 0건**이다.

#### 스펙 요구사항 vs 실제 구현

| 스펙 항목 | 요구 사항 | 구현 상태 |
|-----------|-----------|-----------|
| 2. ObjectPool.Clear() 추가 | Clear 메서드 추가 | ✅ 완료 |
| 3. GameManager cleanup | SceneManager.LoadScene 전에 pool Clear 호출 | ❌ 미구현 |
| 3. Resources.UnloadUnusedAssets | 씬 로드 후 호출 | ⚠️ OnDestroy에서 호출 (타이밍 부적절) |
| 4. EventBus.Clear 순서 | 새 씬 구독 전에 실행 보장 | ⚠️ 아래 상세 |
| 5. DontDestroyOnLoad 점검 | 부적절한 DDOL 없음 확인 | ⚠️ 풀 부모에 DDOL 사용 중, 리뷰 미흡 |

### DontDestroyOnLoad 사용 현황 (6건)

| 파일 | 대상 | DDOL 적합성 |
|------|------|-------------|
| `AudioManager.cs:25` | AudioManager 싱글톤 | ✅ 적합 (크로스-씬 BGM 유지) |
| `SteamManager.cs:14` | SteamManager 싱글톤 | ✅ 적합 (Steam API 연속성) |
| `EventVFX.cs:12` | EventVFX 싱글톤 | ⚠️ 조건부 적합 (아래 참조) |
| `EventSystemEnsurer.cs:14` | EventSystem | ✅ 적합 (Unity 필수 인프라) |
| `Projectile.cs:31` | ProjectilePool 부모 Transform | ⚠️ 문제 — 풀 오브젝트 씬 간 누수 원인 |
| `DamageText.cs:23` | DamageTextPool 부모 Transform | ⚠️ 문제 — 풀 오브젝트 씬 간 누수 원인 |

### Projectile/DamageText 풀 부모의 DDOL 문제

`Projectile.EnsurePool()` (30~31행)과 `DamageText.EnsurePool()` (22~23행)은 풀 부모 Transform에 `DontDestroyOnLoad`을 적용한다. 이는 다음 시나리오에서 메모리 누수를 일으킨다:

1. GameScene에서 전투 중 Projectile/DamageText 생성
2. 사용 후 `Return()` -> 풀 부모 아래로 이동, `SetActive(false)`
3. "메인 메뉴로 돌아가기" -> `SceneManager.LoadScene("MainMenuScene")`
4. 풀 부모가 DDOL이므로 비활성 오브젝트들이 파괴되지 않고 메모리에 잔존
5. 다시 GameScene 진입 -> `_pool != null`이므로 `EnsurePool()` 스킵, 이전 풀 재사용
6. 반복할수록 누적 (풀의 maxSize로 상한은 있지만 해제되지 않음)

**`ObjectPool.Clear()`가 구현되었지만 호출하는 코드가 없으므로, 이 문제는 해결되지 않았다.**

---

## 검증 3: UI 추적 (씬 전환 경로)

### 씬 전환 경로 분석

1. **GameScene -> MainMenu**: `GameUIWiring.WirePauseMenuCallbacks()` (198행)
   - `pm.OnMainMenuRequested = () => SceneManager.LoadScene("MainMenuScene");`
   - **pre-cleanup 없음.** 직접 `LoadScene` 호출.
   - GameManager는 씬 언로드 시 `OnDestroy()` 에서 `EventBus.Clear()` + `Resources.UnloadUnusedAssets()` 호출.

2. **MainMenu -> GameScene**: `MainMenuController.OnNewGame()` / `OnContinue()` (62, 69행)
   - 마찬가지로 직접 `SceneManager.LoadScene("GameScene")` 호출.

3. **Boot -> MainMenu**: `BootSceneController` (72행)
   - 직접 `LoadScene`.

### Resources.UnloadUnusedAssets 타이밍 문제

`Resources.UnloadUnusedAssets()`는 `GameManager.OnDestroy()`에서 호출된다. Unity 문서에 따르면 이 API는 비동기 `AsyncOperation`을 반환하며, 씬 전환 중 `OnDestroy` 시점에 호출하면:
- 아직 새 씬의 오브젝트가 로드 중이므로 "미사용" 판정이 부정확할 수 있다.
- 스펙은 **"after scene load"** 호출을 요구했으나, 실제로는 **"during scene unload"** 시점에 호출되고 있다.
- 반환값(`AsyncOperation`)을 무시하고 있어 완료 확인 불가.

### EventBus.Clear() 순서 위험

`EventBus.Clear()`는 `GameManager.OnDestroy()`에서 호출된다.
- `EventVFX`는 DDOL 싱글톤이며, `OnEnable`에서 `EventBus.On<>`, `OnDisable`에서 `EventBus.Off<>`를 사용한다.
- 시나리오: GameScene 언로드 -> `GameManager.OnDestroy()` -> `EventBus.Clear()` -> EventVFX의 구독도 소멸.
- MainMenuScene 로드 -> EventVFX는 이미 활성 상태(DDOL)이므로 `OnEnable` 재호출 안 됨 -> **EventVFX 이벤트 구독이 영구적으로 끊어진다.**
- 다시 GameScene 진입해도 EventVFX의 LevelUp/ItemCollect VFX가 동작하지 않는다.

### GameUIWiring EventBus 구독 해제 누락

`GameUIWiring`은 `WireAudio()`와 `SubscribeEvents()`에서 총 15개의 `EventBus.On<>` 구독을 등록하지만, `EventBus.Off<>` 호출이 **단 한 건도 없다.** 람다로 등록되어 있어 `Off` 호출 자체가 불가능하다 (참조를 저장하지 않았으므로). `EventBus.Clear()`에 전적으로 의존하는 구조이며, 이는 DDOL 오브젝트의 구독을 함께 파괴하는 부작용을 유발한다.

---

## 검증 4: 플레이 시나리오

### 시나리오 A: 메인 메뉴 왕복 3회

1. GameScene 진입 -> 전투 -> 투사체/데미지텍스트 20개 생성 -> 풀로 반환
2. 일시정지 -> "메인 메뉴로" 클릭
3. `SceneManager.LoadScene("MainMenuScene")` 직접 호출
4. `GameManager.OnDestroy()`: `EventBus.Clear()` -> `Resources.UnloadUnusedAssets()`
5. ProjectilePool(DDOL, 최대 50개) + DamageTextPool(DDOL, 최대 80개) 메모리에 잔존
6. EventVFX 구독 소멸 (재등록 경로 없음)
7. 다시 "새 게임" -> GameScene 재진입
8. EventVFX: LevelUp/ItemCollect VFX 미동작 (구독 끊김)
9. 풀 오브젝트: 이전 씬의 비활성 오브젝트 재활용 시도 (오래된 참조 문제 가능)
10. 3회 반복 시 풀은 maxSize 상한으로 무한 성장은 아니지만, 130개의 불필요한 GameObject가 메모리에 상주

### 시나리오 B: 전투 중 다수 이펙트 후 씬 전환

1. 보스전에서 크리티컬 데미지텍스트 80개 풀 포화
2. 메인 메뉴 복귀 -> 풀 오브젝트 80개 DDOL로 잔존
3. 저사양 모바일 디바이스에서 메모리 압박 가능

---

## 페르소나 리뷰

### 캐주얼 게이머
메인 메뉴 왕복 자체는 크래시 없이 작동할 것으로 보인다. 하지만 여러 번 왕복하면 EventVFX 끊김으로 레벨업 이펙트가 안 보이는 현상을 경험할 수 있다. "레벨업했는데 이펙트가 안 나와요"는 체감 품질에 영향이 크다.

### 코어 게이머
장시간 플레이 중 메뉴 왕복이 드물겠지만, 죽음 후 메인 메뉴 -> 재진입 패턴에서 VFX 누락을 발견할 가능성 높다. 풀 오브젝트의 메모리 상주는 maxSize 덕분에 무한 증가는 아니지만, 불필요한 자원 점유다.

### UX/UI 디자이너
씬 전환 시 로딩 스크린이나 페이드 전환이 없어 직접 `LoadScene` 호출로 화면이 순간적으로 끊긴다. 메모리 이슈와 별개로 전환 UX 자체가 거칠다. (단, 이는 S-051 범위 외)

### QA 엔지니어
**재현 가능한 버그 2건:**
1. **[BUG] EventVFX 구독 소멸**: GameScene -> MainMenu -> GameScene 진입 후 레벨업 시 VFX 미표시. EventBus.Clear()가 DDOL 오브젝트의 구독까지 제거하기 때문.
2. **[CONCERN] 풀 메모리 미해제**: ObjectPool.Clear()가 구현되었으나 호출 지점이 없음. Projectile(50개 상한)과 DamageText(80개 상한) 풀 오브젝트가 DDOL로 씬 간 잔존.

**단위 테스트 부재**: ObjectPool.Clear() 동작을 검증하는 테스트가 없다.

---

## 종합 판정

### ❌ NEEDS_WORK

스펙의 5개 요구사항 중 1개만 완전히 충족되었고, 핵심 항목에서 미흡하다.

| # | 요구사항 | 상태 | 비고 |
|---|----------|------|------|
| 1 | ObjectPool.Clear() 메서드 추가 | ✅ | 구현 정확 |
| 2 | GameManager cleanup (pool Clear 호출) | ❌ | Clear()를 호출하는 코드가 어디에도 없음 |
| 3 | Resources.UnloadUnusedAssets() 호출 | ⚠️ | 호출 위치가 스펙과 다름 (after scene load -> during scene unload) |
| 4 | EventBus.Clear() 순서 검증 | ❌ | DDOL 오브젝트(EventVFX) 구독 소멸 버그 발견 |
| 5 | DontDestroyOnLoad 부적절 사용 점검 | ❌ | Projectile/DamageText 풀 부모의 DDOL이 누수 원인이나 미조치 |

### 필수 수정사항 (Must Fix)

1. **Projectile/DamageText에 씬 전환 시 풀 정리 메커니즘 추가.**
   - 방법 A: GameManager.OnDestroy()에서 정적 메서드로 `Projectile.ClearPool()`, `DamageText.ClearPool()` 호출.
   - 방법 B: 풀 부모의 DDOL을 제거하고, 씬별 풀 재생성으로 전환.

2. **EventVFX와 EventBus.Clear() 충돌 해결.**
   - EventVFX가 DDOL이므로, `EventBus.Clear()` 후에 재구독할 수 있는 메커니즘 필요.
   - 예: `SceneManager.sceneLoaded` 콜백에서 DDOL 오브젝트들이 재구독하도록 처리.

3. **Resources.UnloadUnusedAssets() 호출 시점을 씬 로드 완료 후로 이동.**
   - `SceneManager.sceneLoaded` 이벤트 핸들러에서 호출하거나, 코루틴으로 1프레임 대기 후 호출.

### 권장 수정사항 (Should Fix)

4. ObjectPool.Clear() 단위 테스트 추가.
5. GameUIWiring의 EventBus 구독을 람다 대신 명시적 메서드 참조로 전환하여 Off() 호출 가능하도록 개선 (장기).
