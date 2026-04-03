# REVIEW-S003-v1: async fire-and-forget 방어

> **리뷰 일시:** 2026-04-03
> **태스크:** S-003 async fire-and-forget 방어
> **스펙:** (없음)
> **판정:** ✅ APPROVE
> **커밋:** 51f16a4

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 기존 참조 변경 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| TASK 명세 부합 | ✅ | "AI.Init(), HandleDialogueResponse 예외 핸들링" — 두 지점 모두 처리 |
| 기존 코드 호환 | ✅ | 외부 API 변경 없음, 내부 리팩토링만 |
| 아키텍처 패턴 | ✅ | async/await 예외 처리 표준 패턴 준수 |
| 테스트 커버리지 | ⚠️ | 예외 시나리오 테스트 미작성 (async 테스트 구조 필요) |

**코드 분석:**

1. **InitAISafe() 추가 (line 57-60):**
   - `_ = AI.Init()` → `_ = InitAISafe()` 변경
   - try-catch로 AI 초기화 실패를 LogError 처리
   - 기존: `_ = AI.Init()` — 예외 발생 시 UnobservedTaskException → 앱 크래시 가능
   - 수정 후: 예외 로깅 후 게임 계속 진행 (AI 없어도 나머지 시스템 정상 작동)

2. **HandleDialogueResponse try-catch (line 215-280):**
   - 전체 로직을 try 블록으로 감쌈
   - catch에서 **상태 복구** 수행:
     - `dlg?.ShowLoading(false)` — 로딩 스피너 해제
     - `_dialogueGenerating = false` — 대화 생성 플래그 초기화
     - `dlg?.AppendLog("System", "...", "#999999")` — 무언의 시스템 메시지
   - **핵심 개선점:** 기존에는 AI.GenerateDialogue 예외 시 로딩 UI가 영구 표시되고 `_dialogueGenerating = true`가 유지되어 대화 기능 전체 잠금 발생

3. **에러 복구 품질:**
   - catch 블록이 `finally` 대신 사용됨 — 정상 경로에서도 `ShowLoading(false)`와 `_dialogueGenerating = false`를 명시적 수행하므로 중복이지만 안전
   - `"..."` 시스템 메시지는 유저에게 에러를 노출하지 않으면서 대화 흐름이 끊기지 않았음을 시사

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 로딩 스피너 | ✅ | 예외 시 ShowLoading(false) — 로딩 영구 표시 방지 |
| 대화 입력 | ✅ | _dialogueGenerating = false 복구 — 입력 재활성화 |
| 시스템 메시지 | ✅ | "..." 출력 — 에러 표시 최소화 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| AI 서버(Ollama) 미실행 상태 | InitAISafe catch → LogError, 게임 정상 시작 |
| 대화 중 AI 응답 타임아웃 | HandleDialogueResponse catch → 로딩 해제, 대화 계속 가능 |
| AI 네트워크 에러 | catch → "..." 표시, 유저가 대화 닫기 가능 |
| 정상 AI 응답 | 기존과 동일하게 동작 |
| 연속 대화 시도 | _dialogueGenerating 플래그 복구 → 재시도 가능 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머

NPC 말 걸었는데 로딩이 멈춰서 게임 전체를 껐다 켜야 하는 경험은 최악이다. 이제 에러가 나도 "..."이 뜨고 대화를 닫을 수 있으면 훨씬 낫다. 다만 "..."만으로는 뭐가 잘못됐는지 모르니까 "잠시 후 다시 시도해주세요" 같은 메시지면 더 좋겠다.

### ⚔️ 코어 게이머

async fire-and-forget은 Unity에서 가장 흔한 무소음 크래시 원인 중 하나. InitAISafe에서 AI 초기화 실패를 격리한 건 좋다 — AI는 옵셔널 피처(Ollama 기반)이므로 게임 핵심 루프를 차단하면 안 된다. 다만 AI 초기화 실패 시 이후 대화에서 반복적으로 GenerateDialogue가 실패할 텐데, AI 상태를 확인하고 폴백 대화를 보여주는 게 더 깔끔할 수 있다. 현 스코프에서는 충분.

### 🎨 UX/UI 디자이너

에러 시 "..." 메시지는 최소한의 피드백. 개선 여지:
- 에러 메시지를 약간 더 명확하게 ("대화를 불러올 수 없습니다")
- 대화 UI에 닫기 버튼이 활성화된 상태인지 확인 필요 (현재는 OnClose 콜백이 이미 연결됨 — 정상)

그러나 **이번 태스크 스코프는 예외 방어**이므로 현 구현 수용 가능.

### 🔍 QA 엔지니어

**안정성 평가:** 양호

| 체크 | 결과 |
|------|------|
| 상태 복구 | ✅ ShowLoading + _dialogueGenerating 둘 다 catch에서 복구 |
| 예외 로깅 | ✅ Debug.LogError로 스택 추적 가능 |
| UI 잠금 방지 | ✅ 로딩/입력 모두 해제 |
| null 방어 | ✅ dlg?. 널 조건부 호출 |

**미비 사항:**
- catch에서 `ex.StackTrace`도 로깅하면 디버깅에 도움
- AI 초기화 실패 상태를 플래그로 저장하여 대화 시도 시 조기 폴백 가능 (후속 개선)

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 명세의 두 지점(AI.Init, HandleDialogueResponse) 모두 처리 |
| 기존 호환성 | ✅ 정상 경로 동작 변경 없음 |
| 코드 품질 | ✅ 상태 복구가 포함된 방어적 catch, UI 잠금 방지 |
| 테스트 | ⚠️ async 예외 시나리오 테스트 미작성 (후속 권장) |

**결론:** ✅ **APPROVE** — async fire-and-forget의 두 핵심 위험 지점(AI 초기화, 대화 응답)을 정확히 방어. 특히 HandleDialogueResponse의 상태 복구(로딩 해제 + 플래그 초기화)가 UX 잠금을 방지하여 실질적 안정성 개선. 에러 메시지 개선 및 AI 폴백은 후속 태스크 영역.
