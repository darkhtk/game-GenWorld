# REVIEW-S038-v1: WorldEvent 동시 실행 방지

> **리뷰 일시:** 2026-04-03
> **태스크:** S-038 WorldEvent duplicate event guard
> **스펙:** 없음 (SPEC-R-038은 별개 태스크)
> **판정:** ✅ APPROVE

---

## 변경 요약

**WorldEventSystem.cs — StartEvent()에 3줄 가드 추가:**

```csharp
if (_activeEvent != null)
    EndEvent();
```

StartEvent()가 호출될 때 이미 활성 이벤트가 있으면 먼저 EndEvent()를 호출하여 멀티플라이어(GlobalDropMultiplier, GlobalGoldMultiplier)를 1f로 초기화한 후 새 이벤트를 시작한다.

**문제 상황:** Restore() 경로(세이브/로드)에서 기존 활성 이벤트 없이 StartEvent() 직접 호출 시 이전 이벤트의 멀티플라이어가 고아 상태로 남아 2x 드롭이 영구 지속될 수 있었음.

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 순수 C# 클래스, 씬 참조 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| TASK 명세 부합 | ✅ | 중복 이벤트 시작 방지 — EndEvent 가드 추가 |
| 기존 코드 호환 | ✅ | 정상 Update() 흐름에서는 _activeEvent null 시에만 StartEvent 호출되므로 가드 미작동 (무영향) |
| 아키텍처 패턴 | ✅ | 순수 C# 클래스 패턴 유지 |
| 테스트 커버리지 | ⚠️ | 단위 테스트 미작성 — 3줄 방어 코드로 합리적 |

### 호출 경로 분석

1. **정상 경로 (Update:56→StartEvent):** _activeEvent가 null일 때만 foreach 진입 → StartEvent 호출. 가드 조건 항상 false. **기존 동작 변경 없음.**

2. **Restore 경로 (Restore:121→StartEvent):** 세이브 로드 시 직접 호출. 만약 이전 게임 세션의 이벤트가 이미 _activeEvent에 남아있으면 가드 발동 → EndEvent()로 멀티플라이어 초기화 후 새 이벤트 시작. **버그 수정.**

3. **EndEvent() 안전성:** 이미 `if (_activeEvent == null) return;` 가드 보유 (line 93). 이중 호출 안전.

4. **이벤트 순서:** EndEvent → WorldEventEndEvent 방출 → 멀티플라이어 리셋 → StartEvent → WorldEventStartEvent 방출. 올바른 순서.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 입력 → 이벤트 → UI 반응 | ✅ | WorldEventEndEvent → WorldEventStartEvent 순서 방출, UI 수신측 올바르게 반영 예상 |
| 패널 열기/닫기 | N/A | UI 패널 변경 없음 |
| 데이터 바인딩 | ✅ | GlobalDropMultiplier/GlobalGoldMultiplier 리셋 후 재설정 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 이벤트 없이 게임 플레이 | 변경 없음 — 가드 미작동 |
| Golden Hour 진행 중 세이브→로드 | EndEvent(Golden Hour) → 멀티플라이어 1f → StartEvent(복원 이벤트) |
| Blood Moon 진행 중 다른 이벤트 강제 시작 | EndEvent(Blood Moon) → 새 이벤트 시작 (멀티플라이어 깔끔) |
| 이벤트 없이 세이브→로드 | Restore에서 activeId 빈 문자열 → StartEvent 미호출 → 변경 없음 |
| 이벤트 만료 | Update:44에서 EndEvent() 호출 → 기존 흐름 유지 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
세이브/로드하면 가끔 드롭이 평소보다 많이 나오는 것 같았는데 이게 그 버그였나? 멀티플라이어가 리셋 안 돼서 2배 드롭이 계속 유지됐다면 밸런스가 깨졌을 거다. 수정 좋다.

### ⚔️ 코어 게이머
Golden Hour 2x 드롭이 영구 지속되면 게임 경제가 무너진다. 세이브/로드 악용으로 의도치 않게 이득을 볼 수 있는 구조였다. EndEvent 가드 한 줄로 깔끔하게 차단. 다만 Restore 경로에서 EndEvent가 WorldEventEndEvent를 방출하는데, 로드 직후 "이벤트 종료" 알림이 잠깐 뜨고 바로 "이벤트 시작" 알림이 뜰 수 있다. 기능 문제는 아니지만 UX 측면에서 살짝 어색할 수 있음.

### 🎨 UX/UI 디자이너
Restore 시 EndEvent→StartEvent 연속 방출은 UI 알림이 빠르게 연속으로 뜰 수 있다. 알림 큐가 있다면 문제 없지만, 동시 표시라면 깜빡임 현상 가능. 현재 단계에서 차단 사유는 아니고 관찰 사항.

### 🔍 QA 엔지니어

| 체크 | 결과 |
|------|------|
| 동작 변경 (정상 경로) | ✅ 없음 — Update 경로에서 가드 미작동 |
| 버그 수정 (Restore 경로) | ✅ 멀티플라이어 고아 방지 |
| null 안전성 | ✅ EndEvent 내부에 _activeEvent null 가드 존재 |
| 이벤트 순서 | ✅ End → Start 순서 |
| 메모리/성능 | ✅ 영향 없음 |
| 회귀 위험 | ✅ 최소 — 3줄 방어 코드 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 중복 이벤트 시작 시 기존 이벤트 정리 |
| 기존 호환성 | ✅ 정상 경로 동작 변경 없음 |
| 코드 품질 | ✅ 최소 변경, 방어적 프로그래밍 |
| 아키텍처 | ✅ 기존 패턴 유지 |

**결론:** ✅ **APPROVE** — 세이브/로드 Restore 경로에서 멀티플라이어 고아 버그를 3줄 가드로 깔끔하게 수정. 정상 게임 흐름에 영향 없음. Restore 시 End/Start 알림 연속 표시는 관찰 사항이나 차단 사유 아님.
