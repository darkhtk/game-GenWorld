# SPEC-S-078: DialogueSystem AI 응답 타임아웃 — 폴백 텍스트 표시

> **태스크:** S-078
> **우선순위:** P2
> **태그:** 🔧 안정성 개선
> **관련 파일:**
> - `Assets/Scripts/AI/OllamaClient.cs`
> - `Assets/Scripts/AI/AIManager.cs`
> - `Assets/Scripts/Core/DialogueController.cs`
> - `Assets/Scripts/UI/DialogueUI.cs`
> - `Assets/Tests/EditMode/AIManagerTimeoutTests.cs` (신규)

## 요약

Ollama AI 응답이 30초 이상 지연되거나 무응답일 때, 플레이어가 로딩 화면에 무한 대기하는 문제를 해결한다. `CancellationTokenSource` 기반 타임아웃을 도입하고, 타임아웃 발생 시 오프라인 폴백 응답을 표시하며 UI에 시각적 피드백을 제공한다.

## 현재 상태

### OllamaClient.GenerateDialogue (line 66-111)

`UnityWebRequest.timeout`을 `(int)DialogueTimeout` (30초)로 설정하지만, 이것은 UnityWebRequest의 내장 타임아웃이며 **서버가 연결은 수락하되 응답을 보내지 않는 경우** 제대로 작동하지 않을 수 있다. 또한 반환값이 `null`일 때 caller 측에서 별도 타임아웃 인지/피드백이 없다.

```csharp
// OllamaClient.cs line 91
req.timeout = (int)DialogueTimeout;  // 30s — UnityWebRequest 내장 타임아웃만 의존
```

### AIManager.TryGenerateWithAI (line 123-164)

최대 2회 재시도 루프가 있다. 각 시도마다 `_client.GenerateDialogue(prompt)`를 await하므로, 최악의 경우 **60초**(30초 x 2회)까지 대기할 수 있다. `CancellationToken` 전달 없이 `await`만 사용한다.

```csharp
// AIManager.cs line 127-129
for (int attempt = 0; attempt < 2; attempt++)
{
    string rawResponse = await _client.GenerateDialogue(prompt);  // 최대 30s 대기
```

### DialogueController.HandleDialogueResponse (line 151-212)

`_ai.GenerateDialogue()`를 await하는 동안 `dlg.ShowLoading(true)` 로딩 UI를 표시한다. 그러나 **타임아웃 구분 없이** `response == null`이면 단순 return하고, exception 발생 시 `"..."` 텍스트만 표시한다. 플레이어에게 "AI 응답 지연" 상황이 명시적으로 전달되지 않는다.

```csharp
// DialogueController.cs line 154-155
_dialogueGenerating = true;
dlg?.ShowLoading(true);
// ... line 161 — await, 최대 60초 블로킹 가능
var response = await _ai.GenerateDialogue(...);
// line 170 — null이면 조용히 return (로딩 UI만 꺼짐)
if (response == null || dlg == null) return;
```

### DialogueUI.ShowLoading (line 210-225)

`LoadingAnimation` 코루틴이 `ThinkingPhrases` 배열을 순환하며 표시한다. 타임아웃 경과 시간이나 진행 표시는 없다. 무한 루프(`while(true)`)이므로 외부에서 `ShowLoading(false)`를 호출하지 않으면 영원히 회전한다.

```csharp
// DialogueUI.cs line 227-248
IEnumerator LoadingAnimation()
{
    // ...
    while (true)  // 무한 루프 — 외부 ShowLoading(false) 호출 필수
    {
        loadingText.text = ThinkingPhrases[index % ThinkingPhrases.Length];
```

### 핵심 문제

1. Ollama 서버가 연결 후 응답을 보내지 않으면 `UnityWebRequest.timeout`이 정확히 동작하는지 보장 불가
2. 재시도 2회로 인해 최대 60초 대기 가능
3. 타임아웃과 에러의 구분 없이 동일하게 처리됨
4. 플레이어에게 "AI 지연" vs "AI 실패" 피드백 부재

## 수정 방안

### 1단계: OllamaClient — CancellationToken 지원 추가

`GenerateDialogue`에 `CancellationToken` 파라미터를 추가하고, `Task.Delay` 기반 타임아웃 경쟁을 구현한다.

```csharp
// OllamaClient.cs — 상단 using 추가
using System.Threading;

// GenerateDialogue 시그니처 변경 (line 66)
public async Task<string> GenerateDialogue(string prompt,
    CancellationToken cancellationToken = default)
{
    try
    {
        // ... 기존 payload/request 구성 코드 유지 (line 70-91)

        var op = req.SendWebRequest();
        while (!op.isDone)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
        }

        // ... 기존 결과 처리 코드 유지 (line 96-104)
    }
    catch (OperationCanceledException)
    {
        Debug.LogWarning("[OllamaClient] Request cancelled (timeout)");
        return null;
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[OllamaClient] GenerateDialogue error: {e.Message}");
        return null;
    }
}
```

### 2단계: AIManager — 전체 타임아웃 30초 상한 적용

`GenerateDialogue`에 `CancellationTokenSource`를 생성하여 전체 AI 호출(재시도 포함)에 30초 상한을 건다. 재시도 루프 전체를 감싸므로 2회 재시도를 합쳐 30초를 초과할 수 없다.

```csharp
// AIManager.cs — 상단 using 추가
using System.Threading;

// GenerateDialogue 시그니처 (line 82) — 변경 없음, 내부에서 CTS 생성

// TryGenerateWithAI 수정 (line 123)
async Task<DialogueResponse> TryGenerateWithAI(NPCBrain brain, DialogueContext ctx)
{
    string prompt = PromptBuilder.BuildDialoguePrompt(brain, ctx);

    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

    for (int attempt = 0; attempt < 2; attempt++)
    {
        if (cts.Token.IsCancellationRequested) break;

        string rawResponse = await _client.GenerateDialogue(prompt, cts.Token);
        if (string.IsNullOrEmpty(rawResponse)) continue;

        try
        {
            // ... 기존 JSON 파싱 + 검증 코드 유지 (line 132-156)
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AIManager] JSON parse failed (attempt {attempt + 1}): {e.Message}");
        }
    }

    return null;
}
```

### 3단계: DialogueController — 타임아웃 폴백 처리 강화

`HandleDialogueResponse`에서 `response == null` 시 오프라인 폴백으로 전환하고, 로그에 시스템 메시지를 추가한다.

```csharp
// DialogueController.cs — HandleDialogueResponse (line 151)
async Task HandleDialogueResponse(VillageNPC npc, string playerInput)
{
    _dialogueGenerating = true;
    var dlg = _uiManager.Dialogue;
    dlg?.ShowLoading(true);

    try
    {
        string loreContext = BuildLoreContext(npc.Def);

        var response = await _ai.GenerateDialogue(
            npc.Def.id, playerInput, _dialogueHistory,
            _playerState.Level, _playerState.Gold,
            _inventory, _data.Items, _quests,
            loreContext, npc.Def.actions, npc.Def.dialogueTraits);

        dlg?.ShowLoading(false);
        _dialogueGenerating = false;

        if (dlg == null) return;

        // response가 null이면 (타임아웃 또는 AI 실패) 오프라인 폴백 사용
        if (response == null)
        {
            dlg.AppendLog("System", "(응답 지연 — 대체 응답)", "#999999");
            // AIManager.BuildOfflineResponse는 private이므로
            // public GenerateDialogue 자체가 이미 폴백을 반환.
            // 여기 도달 = AiEnabled=true인데 AI+폴백 모두 실패한 극단 케이스.
            // FallbackResponse와 동일한 형태로 직접 처리.
            response = new DialogueResponse
            {
                dialogue = "...무슨 말을 하려 했는데, 기억이 안 나는군.",
                options = new[] { "괜찮습니다.", "다시 말해주세요." },
                relationshipChange = 0,
                newMemory = "대화 시도"
            };
        }

        _dialogueHistory.Add(new DialogueEntry { role = "npc", text = response.dialogue });
        dlg.AppendLog(npc.Def.name, response.dialogue, npc.Def.color);
        // ... 이하 기존 옵션/퀘스트/액션 처리 로직 유지
    }
    catch (Exception ex)
    {
        // ... 기존 catch 유지
    }
}
```

**중요:** 현재 `AIManager.GenerateDialogue` (line 82-121)는 `AiEnabled == true`일 때 `TryGenerateWithAI`가 null을 반환하면 `BuildOfflineResponse`로 폴백한다. 따라서 정상 흐름에서는 `DialogueController`에 null이 도달하지 않는다. 그러나 `BuildOfflineResponse` 자체가 예외를 던지는 극단 케이스를 방어하기 위해 DialogueController에도 null 체크 폴백을 추가한다.

### 4단계: DialogueUI — 타임아웃 경과 표시 (선택적 개선)

`LoadingAnimation`에 경과 시간 표시를 추가한다.

```csharp
// DialogueUI.cs — LoadingAnimation 수정 (line 227)
IEnumerator LoadingAnimation()
{
    int index = 0;
    float totalElapsed = 0f;
    var cg = loadingPanel.GetComponent<CanvasGroup>();
    if (cg == null) cg = loadingPanel.AddComponent<CanvasGroup>();

    while (true)
    {
        string phrase = ThinkingPhrases[index % ThinkingPhrases.Length];
        if (loadingText != null)
        {
            if (totalElapsed > 10f)
                loadingText.text = $"{phrase} ({(int)totalElapsed}s)";
            else
                loadingText.text = phrase;
        }
        index++;

        float elapsed = 0f;
        while (elapsed < 2f)
        {
            elapsed += Time.unscaledDeltaTime;
            totalElapsed += Time.unscaledDeltaTime;
            float alpha = 0.3f + 0.7f * (0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI));
            cg.alpha = alpha;
            yield return null;
        }
    }
}
```

10초 이후부터 경과 시간을 표시하여 플레이어에게 "아직 처리 중"임을 알린다.

## 연동 경로

```
PlayerInput (E키)
  → DialogueController.TryInteract()                    [line 78]
    → DialogueUI.Show()                                  [DialogueUI line 90]
    → DialogueController.HandleDialogueResponse()        [line 151]
      → DialogueUI.ShowLoading(true)                     [line 155]
      → AIManager.GenerateDialogue()                     [line 161, AIManager line 82]
        → AIManager.TryGenerateWithAI()                  [line 111, AIManager line 123]
          → CancellationTokenSource(30s) 생성             [신규]
          → OllamaClient.GenerateDialogue(prompt, token) [AIManager line 129]
            → UnityWebRequest + CancellationToken 검사   [OllamaClient line 93-94]
            ← 타임아웃 시 OperationCanceledException → null 반환
          ← null 반환 (2회 재시도 포함 30초 상한)
        ← null → BuildOfflineResponse() 폴백             [AIManager line 120]
      ← DialogueResponse (폴백 포함 항상 non-null)
      → DialogueUI.ShowLoading(false)                    [line 167]
      → DialogueUI.AppendLog()                           [line 173]
      → DialogueUI.ShowOptions()                         [line 176]
```

## UI 와이어프레임

### 정상 흐름 (AI 응답 < 30초)

```
┌─────────────────────────────────┐
│ [NPC 이름]                   [X] │
│─────────────────────────────────│
│ [NPC] 어서 오게, 모험가여!      │
│ [You] 안녕하세요                │
│ [NPC] (AI 생성 응답)            │
│─────────────────────────────────│
│ [옵션1] [옵션2]                 │
└─────────────────────────────────┘
```

### 로딩 중 (10초 초과 시)

```
┌─────────────────────────────────┐
│ [NPC 이름]                   [X] │
│─────────────────────────────────│
│ [NPC] 어서 오게, 모험가여!      │
│ [You] 안녕하세요                │
│─────────────────────────────────│
│     ┌───────────────────┐       │
│     │  고민 중... (15s)  │       │
│     └───────────────────┘       │
└─────────────────────────────────┘
```

### 타임아웃 폴백 (30초 초과)

```
┌─────────────────────────────────┐
│ [NPC 이름]                   [X] │
│─────────────────────────────────│
│ [System] (응답 지연 — 대체 응답) │
│ [NPC] ...무슨 말을 하려 했는데,  │
│       기억이 안 나는군.          │
│─────────────────────────────────│
│ [괜찮습니다.] [다시 말해주세요.] │
└─────────────────────────────────┘
```

"다시 말해주세요" 선택 시 동일 NPC에게 재시도 가능 (기존 `OnPlayerResponse` 흐름 재진입).

## 세이브 연동

해당 없음. 타임아웃은 런타임 전용 로직이며 저장/복원 대상이 아니다. `DialogueResponse.newMemory`는 폴백 응답에서도 `"대화 시도"`로 설정되어 `NPCBrain.AddMemory`에 기록되지만, 이는 기존 세이브 흐름(`AIManager.SerializeAllBrains`)에서 이미 처리된다.

## 테스트 방안

`Assets/Tests/EditMode/AIManagerTimeoutTests.cs` (신규):

```csharp
[TestFixture]
public class AIManagerTimeoutTests
{
    // 1. TryGenerateWithAI가 30초 내에 반드시 반환하는지 검증
    [Test]
    public void GenerateDialogue_WhenAITimesOut_ReturnsFallbackWithin30Seconds()
    // Setup: OllamaClient mock — GenerateDialogue가 Task.Delay(60s) 후 null 반환
    // Assert: 30초 이내에 non-null DialogueResponse 반환 (BuildOfflineResponse 폴백)

    // 2. CancellationToken이 OllamaClient에 전파되는지 검증
    [Test]
    public void GenerateDialogue_CancellationToken_PropagatedToClient()
    // Setup: OllamaClient mock — CancellationToken 수신 확인
    // Assert: token.IsCancellationRequested == true after timeout

    // 3. 재시도 루프가 전체 30초 상한을 공유하는지 검증
    [Test]
    public void TryGenerateWithAI_RetryLoop_SharesSingle30SecondBudget()
    // Setup: 1차 시도 20초 소요 후 null → 2차 시도 시작 시 이미 CTS 잔여 10초
    // Assert: 전체 소요 시간 <= 31초 (1초 여유)

    // 4. 폴백 응답에 올바른 dialogue/options가 포함되는지 검증
    [Test]
    public void GenerateDialogue_AiFailure_ReturnsOfflineResponseWithOptions()
    // Setup: AiEnabled=true, OllamaClient mock — 항상 null 반환
    // Assert: response.dialogue != null, response.options.Length > 0

    // 5. 정상 AI 응답 시 기존 동작 유지 (regression)
    [Test]
    public void GenerateDialogue_NormalAIResponse_ReturnsAIResult()
    // Setup: OllamaClient mock — 즉시 valid JSON 반환
    // Assert: response.dialogue == AI 생성 텍스트
}
```

> **참고:** `OllamaClient`는 순수 C# 클래스이므로 interface 추출(`IOllamaClient`) 후 mock 주입이 이상적이나, 현재 코드베이스에 DI/interface 패턴이 없으므로 `virtual` 메서드 + 서브클래스 mock 또는 `AIManager` 생성자에 `OllamaClient` 직접 주입(이미 가능)으로 테스트한다.

## 검증 항목

- [ ] Ollama 서버 중지 상태에서 대화 시작 → 30초 이내 폴백 응답 표시
- [ ] Ollama 서버 느린 응답(> 30초) 시 타임아웃 후 폴백 표시
- [ ] 정상 AI 응답(< 30초) 시 기존 동작 변화 없음
- [ ] 로딩 UI에 10초 이후 경과 시간 표시
- [ ] 폴백 응답의 "다시 말해주세요" 옵션 선택 시 재시도 정상 동작
- [ ] `_dialogueGenerating` 플래그가 타임아웃 후 정상 해제됨
- [ ] 대화 중 닫기(X) 버튼 동작 정상 (타임아웃 대기 중에도)
