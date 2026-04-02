# SPEC-B-003: NPC 대화 불가 수정

> **우선순위:** P0 (사용자 버그 리포트)
> **증상:** NPC에게 대화를 걸 수 없음.

---

## 진단 포인트

### 1. 인터랙션 입력 처리 (가장 유력)
- **파일:** `Assets/Scripts/Entities/PlayerController.cs`
- NPC 인터랙션 키(E 또는 Space)에 대한 입력 핸들링이 있는지 확인.
- 현재 확인된 입력: `LeftControl`/`Space` = 공격, WASD = 이동. **E키 인터랙션 핸들링 부재 가능성.**
- **확인:** PlayerController에서 NPC 근접 감지 + 키 입력 → DialogueUI 호출 체인이 존재하는지.

### 2. NPC 인터랙션 범위 판정
- **파일:** `Assets/Scripts/Entities/VillageNPC.cs:123-126`
- `IsInInteractionRange(playerPos, range=1.5f)` 메서드 존재하나, 이를 호출하는 코드 확인 필요.
- 호출자가 없으면 범위 체크 자체가 실행되지 않음.

### 3. DialogueUI 호출 체인
- **파일:** `Assets/Scripts/UI/DialogueUI.cs`
- `DialogueStartEvent` 이벤트 발행 → DialogueUI 수신 구조 확인.
- **파일:** `Assets/Scripts/Core/GameEvents.cs` — DialogueStartEvent 정의 확인.
- **파일:** `Assets/Scripts/Core/GameManager.cs:348-354` — Dialogue 콜백 연결 확인.

### 4. 진입점 체인 확인 순서
1. 플레이어 입력 (E키 등) → NPC 탐색
2. NPC 범위 체크 (`IsInInteractionRange`)
3. 대화 데이터 로드 (`EvaluateConditionalDialogue` 또는 기본 대화)
4. `DialogueUI.Show()` 호출
5. `EventBus.Emit(DialogueStartEvent)` → 카메라 줌 등

**각 단계에서 끊어지는 지점을 Debug.Log로 추적.**

## 수정 방향
1. PlayerController에 NPC 인터랙션 키 핸들링 추가/수정.
2. E키 또는 Space 근접 NPC 감지 → 가장 가까운 NPC 선택 → 대화 시작.
3. 기존 `VillageNPC.IsInInteractionRange` 활용.

## 검증
- E키로 NPC 근접 시 대화 UI 열리는지.
- 조건부 대화 분기(R-008) 정상 동작.
- 대화 종료 후 NPC 이동 재개 (`ResumeMoving`).
