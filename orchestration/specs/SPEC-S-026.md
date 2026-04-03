# SPEC-S-026: NPC 이동 재개 실패 — 대화 종료 후 ResumeMoving 미호출 경로

> **Priority:** P1
> **Tag:** Bug Fix (Stabilize)
> **Depends on:** None

## 문제

`VillageNPC.StopMoving()`은 대화 시작 시 호출되어 NPC를 멈추지만, `ResumeMoving()`이 모든 대화 종료 경로에서 호출되지 않는다. NPC가 `IsStopped = true` 상태로 영구 정지되어 패트롤을 재개하지 못한다.

### 미호출 경로 3가지

**1. 초기 OnClose 콜백 덮어쓰기 (WireUICallbacks vs TryInteractNPC)**

`WireUICallbacks()` (line ~646)에서 설정하는 `dlg.OnClose`는 `player.Frozen = false`만 처리하고 `ResumeMoving()`을 호출하지 않는다:

```csharp
// WireUICallbacks() — line 646
dlg.OnClose = () =>
{
    uiManager.SetDialogueOpen(false);
    player.Frozen = false;
    // ResumeMoving() 누락, _dialogueNpc 정리 누락
};
```

`TryInteractNPC()` (line ~204)에서는 올바르게 `capturedNpc.ResumeMoving()`을 호출한다. 그러나 `WireUICallbacks()`가 `Start()`에서 실행된 후, 첫 번째 `TryInteractNPC()` 호출 전에 어떤 경로로든 `OnClose`가 트리거되면 `ResumeMoving()`이 누락된다. 또한 `WireUICallbacks()`의 콜백은 어떤 NPC 참조도 캡처하지 않아 `_dialogueNpc`를 정리하지 못한다.

**2. UIManager.HideAll() 경로**

`UIManager.HideAll()` (line 57)는 `dialogue.Hide()`를 직접 호출하지만 `OnClose` 콜백을 트리거하지 않는다:

```csharp
public void HideAll()
{
    ...
    if (dialogue != null) dialogue.Hide();  // Hide()만 호출, OnClose 미호출
    ...
    _dialogueOpen = false;
}
```

`HideAll()`은 ESC 키 입력 시 `IsAnyPanelOpen()`이 true인 경우 호출되지만, `_dialogueOpen` 상태가 true이면 `Update()`가 조기 리턴하므로 이 경로는 현재 직접 도달하기 어렵다. 그러나 `HideAll()`이 외부에서 호출되는 경우(예: 씬 전환 전 정리) NPC가 정지 상태로 남는다.

**3. SceneManager.LoadScene("MainMenuScene") 경로**

`PauseMenuUI.OnMainMenuRequested` (line 698)는 대화 중에도 호출 가능하다:

```csharp
pm.OnMainMenuRequested = () => SceneManager.LoadScene("MainMenuScene");
```

씬 로드 시 대화 상태 정리 없이 오브젝트가 파괴된다. 재시작 후 세이브 데이터에 NPC의 `IsStopped` 상태가 잔류할 수 있다. 현재는 `IsStopped`가 직렬화되지 않으므로 씬 재로드 시 자연 복구되지만, `OnDestroy()`에서 `EventBus.Clear()`만 호출하고 대화 상태를 정리하지 않는 것은 방어적이지 않다.

**4. HandleDialogueResponse 예외 경로**

`HandleDialogueResponse()` (line 220)의 `catch` 블록에서 `_dialogueGenerating = false`만 설정하고 대화를 닫지 않는다. 사용자가 이후 UI를 닫으면 `OnClose`를 통해 정리되지만, 예외 반복 시 UI가 응답 불능 상태가 될 수 있다.

## 연동 경로

```
[F 키 입력] -> GameManager.TryInteractNPC()
  -> VillageNPC.StopMoving()     // IsStopped = true
  -> DialogueUI.Show()
  -> dlg.OnClose = { ResumeMoving() }  // 정상 경로

[대화 종료]
  (a) closeButton 클릭 -> DialogueUI.Hide() + OnClose?.Invoke()  // OK
  (b) AutoCloseDialogue -> dlg.Hide() + dlg.OnClose?.Invoke()    // OK
  (c) UIManager.HideAll() -> dialogue.Hide()                     // OnClose 미호출
  (d) SceneManager.LoadScene -> 오브젝트 파괴                     // 정리 없음
  (e) WireUICallbacks 초기 콜백 -> player.Frozen=false만           // ResumeMoving 누락
```

## 수정 방안

### 1. CloseDialogue 중앙 메서드 추가 (GameManager)

```csharp
void CloseDialogue()
{
    var dlg = uiManager?.Dialogue;
    if (dlg == null) return;

    dlg.Hide();
    uiManager.SetDialogueOpen(false);

    if (player != null) player.Frozen = false;

    if (_dialogueNpc != null)
    {
        var npc = _dialogueNpc;
        _dialogueNpc = null;
        npc.ResumeMoving();
        int turns = _dialogueHistory.Count(e => e.role == "npc");
        EventBus.Emit(new DialogueEndEvent { npcId = npc.Def?.id, turns = turns });
    }

    _dialogueGenerating = false;
}
```

### 2. 모든 종료 경로에서 CloseDialogue 사용

- `TryInteractNPC()` — `dlg.OnClose = () => CloseDialogue();`
- `WireUICallbacks()` — `dlg.OnClose` 초기 콜백도 `CloseDialogue()` 호출
- `AutoCloseDialogue()` — `dlg.Hide()` + `dlg.OnClose?.Invoke()` 대신 `CloseDialogue()` 직접 호출
- `OnMainMenuRequested` — `SceneManager.LoadScene` 전에 `CloseDialogue()` 호출

### 3. UIManager.HideAll() 방어

`HideAll()`에서 dialogue를 닫을 때 `OnClose` 콜백도 호출:

```csharp
public void HideAll()
{
    ...
    if (dialogue != null)
    {
        dialogue.Hide();
        dialogue.OnClose?.Invoke();
    }
    ...
}
```

### 4. 중복 호출 방어

`CloseDialogue()`는 `_dialogueNpc == null`이면 NPC 관련 로직을 스킵하므로 중복 호출에 안전하다. `DialogueUI.Hide()`도 이미 멱등이다.

## 수정 대상 파일

| 파일 | 변경 내용 |
|------|-----------|
| `Assets/Scripts/Core/GameManager.cs` | `CloseDialogue()` 추가, `TryInteractNPC` / `WireUICallbacks` / `AutoCloseDialogue` / `OnMainMenuRequested` 수정 |
| `Assets/Scripts/UI/UIManager.cs` | `HideAll()`에서 dialogue `OnClose` 호출 추가 |

## Acceptance Criteria

1. 대화 정상 종료 (closeButton) 후 NPC가 패트롤 재개
2. AutoCloseDialogue 경로 후 NPC가 패트롤 재개
3. 메인 메뉴 전환 시 대화 상태 정리 (예외 없음)
4. `CloseDialogue()` 중복 호출 시 예외 없음
5. `_dialogueNpc`가 null인 상태에서 `CloseDialogue()` 호출 시 안전
6. 기존 대화 관련 이벤트 (`DialogueStartEvent`, `DialogueEndEvent`) 정상 발행

## Data / Save Impact

없음. `IsStopped`는 직렬화 대상이 아니므로 세이브 포맷 변경 없음.

## 테스트

### Edit Mode Unit Test

- `CloseDialogue` 호출 시 `VillageNPC.IsStopped`가 `false`로 복원되는지 검증
- `_dialogueNpc == null`일 때 `CloseDialogue` 호출 시 예외 없음 검증
- `CloseDialogue` 2회 연속 호출 시 예외 없음 검증

### Play Mode / 수동 테스트

1. NPC 대화 시작 -> closeButton으로 종료 -> NPC 패트롤 확인
2. NPC 대화 시작 -> 응답 옵션 없음(farewell) -> AutoClose 후 NPC 패트롤 확인
3. NPC 대화 시작 -> ESC -> 메인 메뉴 전환 -> 재진입 후 NPC 정상 확인
4. NPC 대화 중 AI 응답 예외 발생 시 -> 대화 닫기 -> NPC 패트롤 확인
