# REVIEW-S050-v1: InputSystem UI/게임 입력 분리

> **리뷰 일시:** 2026-04-03
> **태스크:** S-050 InputSystem UI/게임 입력 분리
> **스펙:** SPEC-S-050
> **커밋:** `f3ec603` fix: S-050 UI/게임 입력 분리 — IsInputBlocked 가드로 UI 패널 중 전투/스킬/상호작용 차단
> **판정:** ✅ APPROVE (조건부 — 후속 이슈 3건 권장)

---

## 변경 요약

- `UIManager.cs`에 `IsInputBlocked()` 메서드 추가 (`_dialogueOpen || IsAnyPanelOpen()`)
- `GameManager.Update()`에서 `inputBlocked` 플래그로 자동공격, 스킬 입력, NPC 상호작용(F키)을 일괄 차단
- `HandleMonsterAttacks()`는 inputBlocked 밖에 배치하여 몬스터의 플레이어 공격은 UI 열린 상태에서도 정상 동작
- NPC 상호작용 `TryInteract()`를 Update 끝에서 inputBlocked 가드 내부로 이동

총 변경: UIManager +2줄, GameManager +9/-7줄 (net +2줄). 최소 침습적 수정.

---

## 검증 1: 엔진 검증

### IsInputBlocked() 구현 확인
```csharp
// UIManager.cs:87
public bool IsInputBlocked() => _dialogueOpen || IsAnyPanelOpen();
```

`IsAnyPanelOpen()` 검사 대상:
| 패널 | 포함 여부 |
|------|-----------|
| InventoryUI | O |
| ShopUI | O |
| CraftingUI | O |
| EnhanceUI | O |
| SkillTreeUI | O |
| QuestUI | O |
| NpcProfilePanel | **X** (IsOpen 프로퍼티 없음) |
| NpcQuestPanel | **X** (IsOpen 프로퍼티 없음) |
| PauseMenuUI | **X** (IsOpen 프로퍼티 없음, Time.timeScale=0으로 별도 처리) |
| DialogueUI | `_dialogueOpen` 플래그로 별도 처리 O |

**소견:** NpcProfilePanel/NpcQuestPanel은 현재 `IsOpen` 프로퍼티가 없어서 IsAnyPanelOpen에 포함 불가. 단, 이 패널들은 대화 흐름(`_dialogueOpen=true`) 중에만 표시되므로 `_dialogueOpen`이 커버한다. PauseMenuUI는 `Time.timeScale=0f` 설정으로 Update 자체가 멈추기 때문에 별도 가드 불필요. 현재 구현은 논리적으로 정확하다.

### Null Safety
```csharp
// GameManager.cs:142
bool inputBlocked = uiManager != null && uiManager.IsInputBlocked();
```
`uiManager`가 null일 경우 `inputBlocked = false`로 폴백. Inspector 미설정 시에도 크래시 없음. 안전.

---

## 검증 2: 코드 추적

### GameManager.Update() 흐름

```
Update()
  ├─ 초기 가드: player == null || PlayerState == null || player.Frozen → return
  ├─ TimeSystem.Update(), WorldEvents.Update()
  ├─ 몬스터 AI 업데이트 (inputBlocked 무관, 정상)
  ├─ 죽은 몬스터 처리 (inputBlocked 무관, 정상)
  ├─ bool inputBlocked = uiManager.IsInputBlocked()
  ├─ if (!inputBlocked)
  │     ├─ combatManager.PerformAutoAttack()   ← 차단됨
  │     ├─ HandleSkillInput()                  ← 차단됨
  │     └─ Input.GetKeyDown(F) → TryInteract() ← 차단됨
  ├─ combatManager.HandleMonsterAttacks()  ← 차단 안됨 (의도적: 몬스터는 계속 공격)
  ├─ PlayerEffects.Tick(), RegenHp(), RefreshHud(), AutoUsePotion()
  └─ RegionTracker, HandleRegionTransition()
```

**스펙 대비 구현:**

| 스펙 요구사항 | 구현 여부 | 비고 |
|---------------|-----------|------|
| 1. UIManager.IsInputBlocked() 추가 | O | line 87 |
| 2. GameManager.Update() 게임 입력 가드 | O | line 142-151 |
| 3. CombatManager 자동공격 가드 | **방식 변경** | CombatManager 내부가 아닌 GameManager에서 호출 자체를 차단 |
| 4. HandleSkillInput() 스킬 입력 가드 | O | HandleSkillInput() 호출 자체가 차단됨 |

**스펙 #3 차이점:** 스펙은 CombatManager.PerformAutoAttack() 내부에서 IsInputBlocked를 체크하라고 했으나, 실제 구현은 GameManager에서 호출 자체를 막았다. 결과는 동일하며, CombatManager가 UIManager 의존성을 갖지 않아 더 깨끗한 설계. 상위 호환.

### HandleSkillInput() 내부 추가 가드 여부
```csharp
void HandleSkillInput()
{
    for (int i = 0; i < GameConfig.SkillSlotCount; i++)
    {
        if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            combatManager.ExecuteSkill(i);
    }
}
```
HandleSkillInput 내부에는 별도 가드 없음. 그러나 호출 자체가 `!inputBlocked` 블록 안에 있으므로 문제 없음.

---

## 검증 3: UI 추적

### UIManager.Update()와의 상호작용
UIManager.Update()에서 처리하는 입력:
- Escape → HideAll / PauseMenu.Toggle
- I → Inventory.Toggle
- K → SkillTree.Toggle
- J → Quest.Toggle
- R → HpPotion, T → MpPotion
- H → ToggleHistory, Tab → ToggleMinimap

**이 입력들은 GameManager의 inputBlocked와 무관하게 동작한다.** UIManager.Update()는 자체적으로 `_dialogueOpen` 체크만 한다. 이는 올바른 설계:
- UI 토글 키(I/K/J)는 패널이 열려 있어도 닫을 수 있어야 한다
- 포션 키(R/T)는 UI 열린 상태에서도 사용 가능 (디자인 의도)

### PlayerController 이동 입력
PlayerController.Update()에서 WASD/방향키 이동과 Ctrl/Space 회피를 처리한다. **이 입력은 IsInputBlocked와 무관하다.** UI 패널 열린 상태에서도 캐릭터가 움직인다.

### LateUpdate 카메라 줌
GameManager.LateUpdate()에서 Mouse ScrollWheel로 카메라 줌을 처리한다. **이 입력도 IsInputBlocked와 무관하다.**

---

## 검증 4: 플레이 시나리오

### 시나리오 A: 인벤토리 열고 스킬 사용 시도
1. I 키로 인벤토리 열기 → `inventory.IsOpen = true`
2. `IsInputBlocked()` → `IsAnyPanelOpen()` → `true`
3. 1~4 키 입력 → HandleSkillInput() 호출 안됨
4. **결과: 스킬 발동 안됨** ✅

### 시나리오 B: 상점에서 좌클릭 구매 시 자동공격
1. 상점 열림 → `shop.IsOpen = true`
2. `IsInputBlocked()` → `true`
3. 좌클릭으로 Buy 버튼 클릭
4. PerformAutoAttack() 호출 안됨
5. **결과: 자동공격 없음** ✅

### 시나리오 C: 대화 중 F키로 다른 NPC 상호작용
1. 대화 시작 → `_dialogueOpen = true`
2. `IsInputBlocked()` → `true`
3. F 키 → TryInteract() 호출 안됨
4. **결과: 이중 대화 방지** ✅

### 시나리오 D: UI 열린 상태에서 몬스터 공격
1. 인벤토리 열림
2. `HandleMonsterAttacks()`는 inputBlocked 블록 밖에서 실행
3. 몬스터가 사거리 내에서 공격 → 데미지 정상 적용
4. **결과: 몬스터 공격은 UI 무관하게 동작** ✅ (의도된 설계)

### 시나리오 E: 패널 열린 상태에서 캐릭터 이동
1. 인벤토리 열림
2. PlayerController.Update()는 독립적으로 WASD 처리
3. 캐릭터가 움직임
4. **결과: 이동 가능** (후속 이슈 참고)

### 시나리오 F: PauseMenu 열린 상태
1. PauseMenu.Open() → Time.timeScale = 0f
2. Update()가 호출되지 않음 (timeScale=0)
3. 모든 게임 입력 자연 차단
4. **결과: 별도 가드 불필요** ✅

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
"인벤토리 열어놓고 실수로 공격 나가던 게 없어졌어요. 상점에서 아이템 클릭하는데 몬스터 때리는 일이 없어서 좋습니다. 다만 인벤토리 열고도 캐릭터가 움직이는 건 좀 어색해요. 인벤토리 정리하는데 몬스터한테 맞으면서 움직이고 있으면..."

**만족도: 8/10**

### ⚔️ 코어 게이머
"전투 중 인벤토리 열어서 장비 바꾸면서 스킬 못 쓰는 건 맞는 동작이에요. 근데 포션(R/T)은 UI 열려있어도 쓸 수 있네요. 이건 괜찮은 건가? 인벤토리 열고 포션 키 연타하면서 장비 정리하는 플레이는 할 수 있으니까 오히려 편할 수도. 다만 회피(Ctrl/Space)도 UI 열린 상태에서 되는데, 의도한 거면 OK."

**만족도: 9/10**

### 🎨 UX/UI 디자이너
"입력 차단의 범위가 명확합니다. 전투 행동(공격/스킬/상호작용)만 차단하고, UI 토글/포션/이동은 허용하는 계층이 깔끔합니다. 다만 향후 고려사항:
- 패널 열릴 때 시각적 피드백(게임 화면 어둡게 처리)이 있으면 '입력 차단 중'임을 직관적으로 전달 가능
- 카메라 줌(스크롤)은 UI 위에서도 작동하는데, 스크롤 가능한 UI(메모리 목록 등)와 충돌 가능성 있음"

**만족도: 8/10**

### 🔍 QA 엔지니어
"핵심 버그 4건(스펙 Bugs 1~4) 모두 수정 확인됨. 코드 추적 상 누락 없음.

**발견된 잠재 이슈:**

1. **[Low] PlayerController 이동 미차단**: UI 패널 열린 상태에서 WASD 이동 + 회피 가능. 몬스터 공격은 계속 들어오므로, 인벤토리 정리 중 이동해서 위험 지역으로 가면 죽을 수 있음. 의도적 설계일 수 있으나 확인 필요.

2. **[Low] LateUpdate 스크롤 줌 미차단**: 상점/인벤토리 등 스크롤 가능한 UI 위에서 마우스 휠 시 카메라 줌과 UI 스크롤이 동시 발생 가능. EventSystem.IsPointerOverGameObject()로 가드 권장.

3. **[Info] 단위 테스트 없음**: 스펙의 Test Plan에 'Unit test: IsInputBlocked() returns true when any panel open, false when all closed' 항목이 있으나, 해당 테스트 미작성. 현재 IsInputBlocked()는 단순 OR 로직이라 실질적 위험은 낮음.

4. **[OK] NpcProfilePanel/NpcQuestPanel**: IsOpen 프로퍼티 없어서 IsAnyPanelOpen()에 미포함이나, 대화 흐름 중에만 표시되므로 _dialogueOpen이 커버. 논리적 정합성 확인됨."

**만족도: 9/10**

---

## 종합 판정

### ✅ APPROVE

**스펙 충족도: 4/4** (구현 방식이 스펙과 약간 다르나 결과 동일, 오히려 더 깨끗한 설계)

**잘 된 점:**
- 최소 변경으로 핵심 버그 4건 모두 해결
- CombatManager에 UIManager 의존성을 주입하지 않고, 호출부(GameManager)에서 차단하는 상위 가드 패턴 채택
- null safety 확보 (uiManager != null 체크)
- HandleMonsterAttacks를 inputBlocked 밖에 배치하여 몬스터 AI 동작 보장
- 기존 UIManager.Update()의 UI 토글 입력에 영향 없음

**후속 권장 (별도 태스크):**
1. **PlayerController 이동 차단 검토** — UI 패널 열릴 때 이동/회피를 차단할지 디자인 결정 필요
2. **ScrollWheel/UI 충돌 방지** — EventSystem.IsPointerOverGameObject() 가드 추가
3. **IsInputBlocked 단위 테스트 작성** — 스펙 Test Plan 항목 이행
