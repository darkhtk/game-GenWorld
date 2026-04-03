# REVIEW-S002-v1: EventBus 구독 누수 방지

> **리뷰 일시:** 2026-04-03
> **태스크:** S-002 EventBus 구독 누수 방지
> **스펙:** (없음)
> **판정:** ✅ APPROVE
> **커밋:** ca8dbf5

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | GameManager는 기존 씬 오브젝트, 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | OnDestroy 라이프사이클 콜백 추가만, 참조 변경 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| TASK 명세 부합 | ✅ | "GameManager OnDestroy에서 EventBus.Clear()" — 정확히 구현됨 |
| 기존 코드 호환 | ✅ | EventBus.Clear()는 기존 public 메서드, 신규 API 없음 |
| 아키텍처 패턴 | ✅ | MonoBehaviour 라이프사이클(OnDestroy) 활용, Unity 패턴 준수 |
| 테스트 커버리지 | ⚠️ | EventBus 클리어 관련 테스트 미작성. 기존 테스트에 영향 없음 |

**코드 분석:**

1. **변경 범위:** GameManager.cs에 `OnDestroy()` 메서드 추가 (5줄). `EventBus.Clear()` 단일 호출.
2. **누수 원인:** EventBus는 `static Dictionary<Type, List<Delegate>>`을 사용. 씬 재로드 시 파괴된 MonoBehaviour 인스턴스의 핸들러가 잔존하여 NullReferenceException 또는 중복 호출 유발.
3. **구독 현황:** GameManager 내 ~15개 람다 구독(WireAudio 8건, SubscribeEvents 8건). 람다는 `Off<>` 개별 해제 불가 — `Clear()` 일괄 해제가 현실적 최선.
4. **다른 구독자:** DeathScreenUI, AchievementUI, DialogueCameraZoom, EventVFX, QuestSystem, SteamAchievementManager 등도 EventBus 구독 중. EventVFX만 `Off<>` 사용. `Clear()`는 전체 해제이나, GameManager가 씬 라이프타임을 관장하므로 OnDestroy 시점에 다른 구독자도 이미 무효 — 안전.
5. **잠재 이슈 — 낮음:** Unity OnDestroy 순서 미보장. GameManager OnDestroy가 먼저 실행되면 다른 컴포넌트의 OnDestroy에서 EventBus.Emit 시 핸들러 누락 가능. 단, teardown 시점의 이벤트 발행은 실질적으로 무의미하므로 수용 가능.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 해당 없음 | — | 인프라 수정, UI 변경 없음 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 일반 플레이 → 게임 종료 | OnDestroy → Clear, 정상 종료 |
| 씬 전환 (메인 메뉴 복귀) | GameManager 파괴 → 구독 전부 해제, 재진입 시 중복 핸들러 없음 |
| 빠른 씬 재로드 | Clear 후 Start에서 재구독 — 정상 동작 |
| 에디터 Play/Stop 반복 | static 리스너 잔존 방지 — 에디터 안정성 향상 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머

메뉴로 나갔다가 다시 들어오면 가끔 소리가 두 번 나거나 이상해지는 경험이 있었을 수 있는데, 이런 종류의 수정이 그걸 막아줄 것 같다. 플레이에 직접 보이는 변경은 아니지만 안정성 측면에서 좋다.

### ⚔️ 코어 게이머

씬 재로드 시 이벤트 중복 구독은 누적되면 프레임 드랍으로 이어질 수 있다. 특히 MonsterKillEvent 같은 고빈도 이벤트가 N회 중복 처리되면 플로팅 텍스트, 골드 지급 등이 N배로 실행될 수 있었다. 근본적 수정이다.

### 🎨 UX/UI 디자이너

직접적 UI 변경 없음. 간접적으로 씬 전환 후 UI 핸들러 중복으로 인한 비정상 동작(이중 팝업, 중복 알림 등)을 예방한다.

### 🔍 QA 엔지니어

**안정성 평가:** 양호

| 체크 | 결과 |
|------|------|
| null 방어 | ✅ Clear()는 Dictionary.Clear() — null-safe |
| 예외 처리 | ✅ OnDestroy에서 예외 발생 가능성 없음 |
| 부작용 | ⚠️ 타 컴포넌트 OnDestroy 중 Emit 시 핸들러 없음 — 실해 없음 |
| 에디터 호환 | ✅ Play/Stop 반복 시 static 누수 방지 |

**미비 사항:**
- 테스트: Clear() 호출 후 Emit 시 핸들러 미실행 확인 테스트 권장
- 테스트: 씬 재로드 시뮬레이션(구독 → Clear → 재구독 → Emit) 테스트 권장

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 명세 항목 정확히 구현 |
| 기존 호환성 | ✅ 기존 API/동작 변경 없음 |
| 코드 품질 | ✅ 최소 변경, 올바른 라이프사이클 활용 |
| 테스트 | ⚠️ EventBus Clear 관련 단위 테스트 미작성 (후속 권장) |

**결론:** ✅ **APPROVE** — 최소 침습적 수정으로 EventBus 정적 구독 누수 문제를 정확히 해결. 람다 구독이 다수인 현 구조에서 Clear()가 최적의 접근법. 테스트 보완은 후속 태스크로 권장.
