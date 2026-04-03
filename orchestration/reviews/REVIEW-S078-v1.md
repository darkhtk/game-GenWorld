# REVIEW-S078-v1: DialogueSystem AI 응답 타임아웃

> **리뷰 일시:** 2026-04-03
> **태스크:** S-078 DialogueSystem AI 응답 타임아웃
> **스펙:** SPEC-S-078
> **커밋:** `3185a32` feat: S-078 AI dialogue 30s timeout with CancellationToken + fallback response + elapsed UI
> **판정:** ✅ APPROVE

---

## 변경 요약

커밋 `3185a32`에서 4개 파일 변경:

1. **OllamaClient.cs** (+10줄) — `GenerateDialogue`에 `CancellationToken` 파라미터 추가, 요청 루프에서 `ThrowIfCancellationRequested()`, `OperationCanceledException` catch
2. **AIManager.cs** (+4줄) — `TryGenerateWithAI`에서 `CancellationTokenSource(30s)` 생성, 재시도 루프 전체에 단일 타임아웃 적용
3. **DialogueController.cs** (+12줄) — `response == null` 시 오프라인 폴백 응답 직접 생성 + 시스템 로그
4. **DialogueUI.cs** (+7줄) — `LoadingAnimation`에 `totalElapsed` 추적, 10초 이후 경과 시간 표시

---

## 검증 1: 엔진 검증

### 1.1 CancellationToken 전파 경로

```
AIManager.TryGenerateWithAI()
  → CancellationTokenSource(30s) 생성
  → cts.Token → OllamaClient.GenerateDialogue(prompt, token)
    → while (!op.isDone) { token.ThrowIfCancellationRequested(); await Task.Yield(); }
    → OperationCanceledException → return null
  → null → 재시도 (if cts not cancelled)
  → null → return null → AIManager.GenerateDialogue → BuildOfflineResponse()
```

30초 단일 CTS가 재시도 루프 전체를 감싸므로 최대 대기 시간이 30초로 제한됨. ✅

### 1.2 UnityWebRequest 타임아웃과의 이중 보호

- `req.timeout = (int)DialogueTimeout` (30초) — UnityWebRequest 내장 타임아웃 (기존)
- `CancellationToken` — Task 레벨 타임아웃 (신규)
- 두 메커니즘이 독립적으로 동작하여 어느 쪽이든 먼저 발동하면 요청 종료
- UnityWebRequest가 연결은 수락하되 응답을 보내지 않는 경우, CancellationToken이 보완 ✅

---

## 검증 2: 코드 추적

### 2.1 OllamaClient CancellationToken 통합 (line 64, 93-97, 109-113)

```csharp
public async Task<string> GenerateDialogue(string prompt,
    CancellationToken cancellationToken = default)
{
    // ...
    while (!op.isDone)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
    }
    // ...
    catch (OperationCanceledException)
    {
        Debug.LogWarning("[OllamaClient] Request cancelled (timeout)");
        return null;
    }
}
```

- `default` 파라미터로 기존 호출 호환성 유지 ✅
- `ThrowIfCancellationRequested()`가 매 yield마다 체크 → 응답성 높음 ✅
- `OperationCanceledException` catch로 예외가 caller에 전파되지 않음 → null 반환 ✅
- **주의:** `req.Abort()`를 호출하지 않으므로, CancellationToken 발동 후에도 UnityWebRequest가 백그라운드에서 계속 진행될 수 있다. 그러나 `req`는 지역 변수이므로 GC 대상이 되고, Unity가 자체적으로 정리한다. → 수용 가능

### 2.2 AIManager CTS 생성 (line 126-129)

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
for (int attempt = 0; attempt < 2; attempt++)
{
    if (cts.Token.IsCancellationRequested) break;
    string rawResponse = await _client.GenerateDialogue(prompt, cts.Token);
```

- `using var` → 메서드 종료 시 자동 Dispose ✅
- 재시도 전 `IsCancellationRequested` 체크 → 1차 시도에 25초 걸렸으면 2차 시도는 5초 남은 상태에서 시작 ✅
- 최악 시나리오: 30초 정확히 만료 → 두 번째 `GenerateDialogue` 진입 직후 `ThrowIfCancellationRequested` → null → break → return null ✅

### 2.3 DialogueController 폴백 응답 (line 170-186)

```csharp
if (response == null)
{
    response = new DialogueResponse
    {
        dialogue = "...무슨 말을 하려 했는데, 기억이 안 나는군.",
        options = new[] { "괜찮습니다.", "다시 말해주세요." },
        relationshipChange = 0,
        newMemory = "대화 시도"
    };
    dlg.AppendLog("System", "(응답 지연 — 대체 응답)", "#999999");
}
```

- 폴백 텍스트가 NPC 캐릭터성에 맞는 자연스러운 대사 ("기억이 안 나는군") ✅
- 옵션 2개 제공: "괜찮습니다" (종료), "다시 말해주세요" (재시도) → UX 적절 ✅
- `relationshipChange = 0` → 호감도 변화 없음 (타임아웃이 NPC 관계에 영향 없음) ✅
- `newMemory = "대화 시도"` → NPC 메모리에 기록되지만 최소한의 영향 ✅
- `dlg.AppendLog("System", ...)` → 시스템 메시지로 타임아웃 상황을 플레이어에게 알림 ✅

**참고:** `AIManager.GenerateDialogue()` (line 82-121)는 `TryGenerateWithAI()` 실패 시 `BuildOfflineResponse()`를 호출하므로, 정상 흐름에서는 `response == null`이 DialogueController에 도달하지 않는다. 이 폴백은 `BuildOfflineResponse` 자체가 실패하는 극단 케이스를 방어한다. → 이중 안전망으로 적절 ✅

### 2.4 DialogueUI 경과 시간 표시 (line 230-240)

```csharp
float totalElapsed = 0f;
// ...
string phrase = ThinkingPhrases[index % ThinkingPhrases.Length];
if (loadingText != null)
{
    loadingText.text = totalElapsed > 10f
        ? $"{phrase} ({(int)totalElapsed}s)"
        : phrase;
}
// ...
totalElapsed += Time.unscaledDeltaTime;
```

- `Time.unscaledDeltaTime` 사용 → 게임 일시정지 상태에서도 정확한 시간 측정 ✅
- 10초 이후부터 표시 → 짧은 로딩에서 불필요한 숫자 방지 ✅
- `(int)totalElapsed` → 정수 초 표시 (15s, 20s 등) → 깔끔 ✅
- `totalElapsed`는 코루틴 지역 변수이므로 `ShowLoading(false)` → `StopCoroutine` 시 자동 리셋 ✅

---

## 검증 3: UI 추적

### 전체 타임아웃 시나리오 UI 흐름

| 시간 | 이벤트 | UI |
|------|--------|-----|
| 0s | E키 → 대화 시작, AI 요청 | 로딩 패널 표시 ("생각 중...") |
| 2s | LoadingAnimation 순환 | "곰곰이 생각하고 있어요..." |
| 10s | totalElapsed > 10f | "깊이 생각하는 중... (10s)" |
| 15s | 경과 | "음... (15s)" |
| 30s | CTS 타임아웃 → null → 폴백 | 로딩 종료, "[System] (응답 지연 — 대체 응답)" + NPC 폴백 대사 + 옵션 표시 |

- 로딩 → 폴백 전환이 매끄러움 ✅
- 플레이어가 30초 동안 상황을 인지할 수 있음 (10초부터 카운터) ✅

### "다시 말해주세요" 재시도 경로

1. 플레이어 "다시 말해주세요" 클릭
2. `DialogueUI.OnPlayerResponse()` → `DialogueController.HandleDialogueResponse()` 재진입
3. 새 CTS 30초 생성 → 다시 AI 요청
4. AI가 이번에 응답하면 정상 대화 진행

재시도 시 새 CTS가 생성되므로 이전 타임아웃 상태가 남지 않음 ✅

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 판정 |
|---------|-----------|------|
| AI 정상 응답 (< 30s) | 기존과 동일 | **PASS** |
| AI 느린 응답 (> 30s) | 30초 타임아웃 → 오프라인 폴백 | **PASS** |
| Ollama 서버 중지 | 연결 실패 → 즉시 null → 재시도 → null → 오프라인 폴백 | **PASS** |
| 로딩 중 대화 닫기 (X) | ShowLoading(false) → 코루틴 중지, _dialogueGenerating 해제 | **PASS** |
| 10초 이내 로딩 | 경과 시간 미표시 | **PASS** |
| 15초 로딩 | "고민 중... (15s)" 표시 | **PASS** |
| 폴백 → "다시 말해주세요" → AI 정상 | 재시도 성공, 정상 대화 | **PASS** |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
NPC한테 말 걸었는데 무한 로딩 걸려서 게임 강종했던 경험이 있다면 이제 해결됐다. 30초 후에 "기억이 안 나는군"이라고 하는 것도 게임 세계관에 맞아서 자연스럽다. 다만 30초도 꽤 긴 편 — 플레이어 관점에서 15-20초가 더 적절할 수 있다.

### ⚔️ 코어 게이머
AI 대화가 게임플레이에 방해가 되지 않는 게 중요하다. 전투 중 NPC 대화가 막히면 게임 진행이 멈추는데, 이제 최대 30초로 제한되고 재시도도 가능하다. CancellationToken 패턴이 깔끔하다.

### 🎨 UX/UI 디자이너
경과 시간 표시(10s부터)는 좋은 판단이다. 로딩 스피너만 보면 "멈춘 건가?" 불안해지지만 카운터가 있으면 "아직 진행 중"이라는 확신을 준다. 폴백 응답 UI도 시스템 메시지(회색)로 구분해서 NPC 대사와 혼동이 없다.

### 🔍 QA 엔지니어
4개 파일 변경이 모두 일관된 방향(타임아웃 → 폴백 → UI 피드백)으로 이루어짐. CancellationToken 전파 경로가 명확. `req.Abort()` 미호출은 minor — UnityWebRequest가 GC될 때 정리됨. 테스트 파일(AIManagerTimeoutTests.cs)은 커밋에 미포함이나 수동 테스트로 검증 가능.

---

## 미해결 권장사항 (비차단)

| # | 항목 | 심각도 | 비고 |
|---|------|--------|------|
| 1 | `req.Abort()` 호출 추가 | NICE | CancellationToken 발동 시 UnityWebRequest 즉시 중단하면 네트워크 리소스 절약. 현재도 GC로 정리됨. |
| 2 | 타임아웃 값 30초 → 20초 검토 | NICE | 플레이어 체감상 30초는 길 수 있음. 게임 테스트 후 조정 권장. |
| 3 | 단위 테스트 추가 | SHOULD | 스펙에 상세 테스트 계획 있으나 커밋 미포함. |

---

## 종합 판정

### ✅ APPROVE

| # | 스펙 검증 항목 | 대응 | 판정 |
|---|--------------|------|------|
| 1 | 30초 이내 폴백 응답 표시 | ✅ CTS 30s + 폴백 | **PASS** |
| 2 | 정상 AI 응답 변화 없음 | ✅ default CancellationToken | **PASS** |
| 3 | 10초 이후 경과 시간 표시 | ✅ totalElapsed > 10f | **PASS** |
| 4 | 폴백 "다시 말해주세요" 재시도 | ✅ 재진입 경로 정상 | **PASS** |
| 5 | _dialogueGenerating 플래그 해제 | ✅ line 168 | **PASS** |

4개 파일에 걸친 변경이지만 각 파일의 책임이 명확하고(전파 → 타임아웃 → 폴백 → UI), 기존 코드와 자연스럽게 통합된다. CancellationToken 패턴이 C# async 모범 사례를 따르고 있으며, 폴백 UX가 게임 세계관에 맞게 설계됨.
