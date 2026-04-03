# SPEC-S-047: DialogueSystem 동시 대화 방지

> **우선순위:** P2
> **태그:** 🔧 안정성
> **관련 파일:** DialogueController.cs, GameManager.cs

## 현재 상태

### 기존 가드
1. **`_dialogueGenerating` (bool)** — AI 응답 생성 중 재진입 방지 (DialogueController:119)
2. **`player.Frozen`** — 대화 중 이동/입력 차단 (GameManager:115)

### 누락된 가드
- `TryInteract()` 자체에 재진입 방지 없음
- `player.Frozen` 체크는 GameManager.Update()에서 F키 입력 전에 수행되므로 대화 중 F키가 차단됨
- **하지만:** 다른 진입점(예: trigger collider, auto-interact)이 추가될 경우 동시 대화 가능

## 분석

현재 코드 기준으로는 **GameManager.Update()의 `if (player.Frozen) return;`** 가드 덕분에 동시 대화가 발생하지 않음.

**잠재 리스크:**
1. 향후 trigger 기반 자동 대화 진입점이 추가되면 Frozen 체크를 우회할 수 있음
2. 대화 UI가 열린 상태에서 코드 경로가 TryInteract()를 직접 호출하면 이중 대화

## 수정 범위 (방어적)

### DialogueController.cs

1. **`_inDialogue` 플래그 추가:**
```csharp
private bool _inDialogue;
```

2. **TryInteract() 진입 가드:**
```csharp
public void TryInteract() {
    if (_inDialogue) return;  // 이미 대화 중
    // ... existing logic ...
    _inDialogue = true;
}
```

3. **대화 종료 시 플래그 해제:**
- CloseDialogue / EndDialogue 경로에서 `_inDialogue = false;`

4. **공개 프로퍼티 (선택적):**
```csharp
public bool InDialogue => _inDialogue;
```

## 검증 항목

- [ ] 대화 중 F키 재입력 시 새 대화 안 열림 (기존 동작 유지)
- [ ] 대화 정상 종료 후 다시 대화 가능
- [ ] _inDialogue가 예외 발생 시에도 해제됨 (try-finally 또는 HandleDialogueResponse catch 블록)
- [ ] _dialogueNpc null 상태에서 TryInteract 안전

## 호출 진입점

- **현재:** GameManager.Update() → F키 → DialogueController.TryInteract()
- **향후 가능:** Trigger collider → TryInteract() (이 경우 _inDialogue 가드 필수)

## 판정

현재는 **이론적 리스크** 수준이지만, 방어적 코딩 관점에서 `_inDialogue` 가드 1줄 추가로 안전성 확보 가능. 저비용 고효과 작업.
