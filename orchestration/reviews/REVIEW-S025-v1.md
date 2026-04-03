# REVIEW-S025-v1: DialogueCameraZoom 복원 보장

> **리뷰 일시:** 2026-04-03
> **태스크:** S-025 DialogueCameraZoom 복원 보장
> **스펙:** 없음
> **커밋:** 3ae7aac
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 변경 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| TASK 명세 부합 | ✅ | OnDisable에서 카메라 크기 강제 복원 |
| 기존 코드 호환 | ✅ | 정상 ZoomOut 경로 불영향 |
| 아키텍처 패턴 | ✅ | OnDisable 정리 — Unity 표준 패턴 |
| 테스트 커버리지 | ⚠️ | 미작성 — MonoBehaviour 라이프사이클이므로 PlayMode 필요 |

### 변경 분석

**DialogueCameraZoom.cs:38-45 (추가된 OnDisable):**
```csharp
void OnDisable()
{
    if (_cam != null && _zooming)
    {
        _cam.orthographicSize = _originalSize;
        _zooming = false;
    }
}
```

**동작 흐름:**
1. `ZoomIn()` → `_originalSize = _cam.orthographicSize` 캡처 → `_targetSize = zoomInSize` → `_zooming = true`
2. 정상 종료: `ZoomOut()` → `_targetSize = _originalSize` → Update Lerp → 복원 완료
3. 비정상 종료: 오브젝트 비활성화/파괴 → `OnDisable()` → 즉시 `_originalSize` 복원

**가드 조건:**
- `_cam != null`: Camera.main이 이미 파괴된 경우 안전
- `_zooming`: 줌 중이 아닌 상태에서 불필요한 크기 변경 방지

**`_originalSize` 정확성:**
- `Awake()`에서 초기값 캡처
- `ZoomIn()`에서 현재 크기로 갱신 (재줌인 시에도 올바른 원본 유지)
- `OnDisable()`에서 사용하는 `_originalSize`는 항상 줌인 직전 값

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 카메라 상태 | ✅ | 비정상 종료 시 줌 원복 보장 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 정상 대화 종료 | DialogueEndEvent → ZoomOut → Update Lerp → 복원 (기존과 동일) |
| 대화 중 씬 전환 | OnDisable → 즉시 원복 → 다음 씬에서 카메라 정상 |
| 대화 중 오브젝트 파괴 | OnDisable → 즉시 원복 |
| 줌 아닌 상태에서 비활성화 | `_zooming=false` → OnDisable 무동작 |
| Camera.main 파괴 후 비활성화 | `_cam=null` → OnDisable 무동작 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
NPC 대화 후 카메라가 확대된 채로 안 돌아오면 답답하다. 이런 예외 상황에서도 카메라가 정상으로 돌아온다니 안심.

### ⚔️ 코어 게이머
`OnDisable`은 Unity 라이프사이클에서 오브젝트 파괴/씬 언로드 전에 반드시 호출되므로, 이벤트 기반(DialogueEndEvent) 복원이 실패하는 모든 케이스를 한 곳에서 잡는다. `_zooming` 플래그로 불필요한 복원을 방지한 것도 깔끔하다.

### 🎨 UX/UI 디자이너
카메라 줌은 몰입감을 위한 연출인데, 복원 실패하면 오히려 불편함의 원인이 된다. OnDisable 안전망은 필수.

### 🔍 QA 엔지니어

| 체크 | 결과 |
|------|------|
| null camera 방어 | ✅ `_cam != null` |
| 불필요한 복원 방지 | ✅ `_zooming` 가드 |
| 정상 경로 영향 | ✅ 없음 — ZoomOut이 먼저 완료하면 `_zooming=false` |
| 즉시 복원 vs Lerp | ✅ OnDisable에서 Lerp는 의미 없으므로 즉시 할당 올바름 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 비정상 종료 시 카메라 줌 복원 보장 |
| 기존 호환성 | ✅ 정상 ZoomOut 경로 불영향 |
| 코드 품질 | ✅ 최소 변경, 표준 Unity 패턴 |

**결론:** ✅ **APPROVE** — `OnDisable()`에서 카메라 orthographicSize를 즉시 복원하여 대화 비정상 종료(씬 전환, 오브젝트 파괴 등) 시 줌인 상태가 잔류하는 것을 방지. 깔끔한 안전망 구현.
