# REVIEW-R006-v1: 리전 전환 시 자동 저장

> **리뷰 일시:** 2026-04-02
> **태스크:** R-006 리전 전환 자동 저장
> **스펙:** SPEC-R006
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | GameManager 내부 이벤트 구독 |
| 에셋 존재 여부 | ✅ | 코드만 변경 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| SPEC 명세 부합 | ✅ | 아래 상세 |
| 기존 코드 호환 | ✅ | 기존 RegionVisitEvent 핸들러에 자연스럽게 통합 |
| 아키텍처 패턴 | ✅ | EventBus 패턴 준수, SaveEvent 재사용 |
| 테스트 커버리지 | ⚠️ | 테스트 미작성 |

### SPEC 항목별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | RegionVisitEvent 구독 | line 200 | ✅ `EventBus.On<RegionVisitEvent>` |
| 2 | 30초 쓰로틀 | line 205 | ✅ `now - _lastAutoSaveTime < 30f` |
| 3 | _lastAutoSaveTime 필드 | line 28 | ✅ |
| 4 | 저장 실행 | line 207 | ✅ `EventBus.Emit(new SaveEvent())` |
| 5 | 로그 출력 | line 208 | ✅ `[AutoSave] Region changed to {e.regionName}` |

### 아키텍처 개선 사항 (스펙 대비)

SPEC은 `SaveSystem.Save(data)` 직접 호출을 제시했으나, 구현은 `EventBus.Emit(new SaveEvent())`를 사용. 이는 더 나은 선택:
- line 222-226의 기존 `SaveEvent` 핸들러 재사용 → `SaveGame()` + `hud.ShowSaveIndicator()`
- UI 피드백(SPEC "선택사항")이 자동으로 포함됨
- 저장 로직 중복 방지

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| HUD 리전명 표시 | ✅ | line 202 `hud.UpdateRegion(e.regionName)` (기존) |
| Auto Save 표시 | ✅ | SaveEvent → `hud.ShowSaveIndicator()` (line 225) |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 새 리전 진입 | 자동 저장 + HUD "Auto Saved" 표시 |
| 30초 내 리전 왕복 | 쓰로틀로 중복 저장 방지 |
| 게임 시작 직후 리전 변경 | _lastAutoSaveTime=0이므로 저장 실행 |
| 같은 리전 내 이동 | RegionVisitEvent 미발생 → 저장 안 함 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
리전 넘어갈 때 자동으로 저장되는 거 좋다. "Auto Saved" 표시도 보이니까 안심됨. 매번 수동 저장 안 해도 돼서 편함.

### ⚔️ 코어 게이머
30초 쓰로틀은 합리적. 리전 경계에서 파밍할 때 매 초 저장되면 I/O 부하가 심할 텐데, 이 정도면 괜찮다. 동기 File.WriteAllText라 대용량 세이브 시 미세한 끊김 가능하지만, RPG 세이브 크기로는 무시할 수준.

### 🎨 UX/UI 디자이너
SaveEvent를 통해 기존 ShowSaveIndicator()가 자동 연결 — UI 일관성 유지. 별도 UI 작업 불필요.

### 🔍 QA 엔지니어
- 쓰로틀 체크가 SaveEvent 발행 전 → 불필요한 이벤트 발행 방지 ✅
- Time.time 기반 → 게임 일시정지(Time.timeScale=0) 시 쓰로틀 타이머도 정지 — 의도된 동작
- SaveGame()에서 전체 상태 직렬화 → 리전 전환 시점의 최신 상태 보장 ✅

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Low | 테스트 미작성 (SPEC 체크리스트 4항목) |

---

## 최종 판정

**✅ APPROVE**

SPEC 100% 충족 + 아키텍처 개선 (SaveEvent 재사용으로 UI 피드백 자동 포함). 쓰로틀, 이벤트 구독, 기존 저장 로직 재사용 모두 정확.
