# SPEC-S-063: EnhanceUI 강화 전 확인 팝업

> **우선순위:** P3
> **태그:** 안정성 개선
> **관련 파일:**
> - `Assets/Scripts/UI/EnhanceUI.cs` (주 수정 대상)
> - `Assets/Scripts/UI/UIManager.cs` (EnhanceUI 참조)
> - `Assets/Scripts/Core/GameUIWiring.cs` (강화 완료 콜백 연결)
> - `Assets/Scripts/Data/StatTypes.cs` (ItemInstance.enhanceLevel)

---

## 배경

강화(Enhancement)는 파괴 확률이 있는 비가역적(destructive) 작업이다. +4 이상 강화 시 `EnhanceTable`에 `destroy` 확률(5%~30%)이 부여되며, 실패하면 `_equipment.Remove(slotName)`으로 장비가 영구 삭제된다. 현재는 슬롯 버튼 클릭 한 번으로 즉시 실행되어 플레이어가 파괴 위험을 인지하지 못한 채 고가 장비를 잃을 수 있다. 이는 의도치 않은 클릭에 의한 데이터 손실 위험이므로, "stabilize" 방향에서 최우선으로 차단해야 하는 UX 버그다.

---

## 현재 동작

`EnhanceUI.AddSlotEntry()` (line 160) 에서 슬롯 버튼에 다음 리스너가 등록된다:

```csharp
btn.onClick.AddListener(() => TryEnhance(captured));
```

`TryEnhance()` (line 165-193)는 클릭 즉시:
1. `_spendGold(info.gold)` — 골드 차감
2. `UnityEngine.Random.value`로 결과 판정
3. 실패 시 `inst.enhanceLevel++`, 성공 시 `_equipment.Remove(slotName)`으로 장비 파괴
4. `Refresh()` 호출

파괴 확률이 존재하는 구간(+4~+9)에서도 확인 없이 즉시 실행된다. 슬롯 목록의 텍스트(line 152-153)에 "X% destroy"가 표시되지만 경고 팝업은 없다.

---

## 목표 동작

슬롯 버튼 클릭 시:

1. **파괴 확률 > 0%인 경우**: 확인 팝업을 표시한다.
   - 팝업에는 장비명, 강화 단계, 성공률, **파괴 확률**을 표시한다.
   - [확인] 클릭 시 `TryEnhance()` 실행.
   - [취소] 클릭 시 팝업 닫고 아무 작업 없음.

2. **파괴 확률 = 0%인 경우(+0~+3)**: 기존과 동일하게 팝업 없이 즉시 실행한다.

이렇게 하면 안전한 구간(+0~+3)의 UX 흐름을 유지하면서, 위험 구간(+4~+9)에서만 확인을 요구한다.

---

## 구현 상세

### 1. EnhanceUI 내부에 인라인 팝업 상태 추가

새 MonoBehaviour나 Prefab 없이 `EnhanceUI` 내부에 팝업용 `GameObject` 참조를 추가한다. 팝업은 EnhanceUI 패널의 자식 오브젝트로 존재한다.

```csharp
[Header("Confirm Popup")]
[SerializeField] GameObject confirmPopup;
[SerializeField] TextMeshProUGUI confirmMessageText;
[SerializeField] Button confirmOkButton;
[SerializeField] Button confirmCancelButton;
```

`Awake()`에 추가:
```csharp
if (confirmPopup != null) confirmPopup.SetActive(false);
if (confirmOkButton != null) confirmOkButton.onClick.AddListener(OnConfirmOk);
if (confirmCancelButton != null) confirmCancelButton.onClick.AddListener(OnConfirmCancel);
```

### 2. 대기 중인 강화 슬롯 저장 필드 추가

```csharp
string _pendingEnhanceSlot;
```

### 3. `AddSlotEntry()`의 버튼 리스너 교체

기존 (line 160):
```csharp
btn.onClick.AddListener(() => TryEnhance(captured));
```

변경 후:
```csharp
btn.onClick.AddListener(() => RequestEnhance(captured));
```

### 4. `RequestEnhance()` 신규 메서드 추가

`TryEnhance()` 앞에 삽입:

```csharp
void RequestEnhance(string slotName)
{
    if (_equipment == null || !_equipment.TryGetValue(slotName, out var inst) || inst == null) return;
    var info = GetEnhanceInfo(inst.enhanceLevel);
    if (info.gold <= 0) return;

    if (info.destroy > 0f)
    {
        // Show confirmation popup
        string itemName = _itemDefs != null && _itemDefs.TryGetValue(inst.itemId, out var def)
            ? def.name : inst.itemId;
        _pendingEnhanceSlot = slotName;
        ShowConfirmPopup(itemName, inst.enhanceLevel, info.success, info.destroy, info.gold);
    }
    else
    {
        TryEnhance(slotName);
    }
}
```

### 5. `ShowConfirmPopup()`, `OnConfirmOk()`, `OnConfirmCancel()` 추가

```csharp
void ShowConfirmPopup(string itemName, int level, float success, float destroy, int gold)
{
    if (confirmPopup == null) return;
    if (confirmMessageText != null)
        confirmMessageText.text =
            $"Enhance {itemName} +{level} → +{level + 1}?\n\n" +
            $"Cost: {gold:N0}G\n" +
            $"Success: {success * 100:F0}%\n" +
            $"<color=#ff4444>Destroy: {destroy * 100:F0}%</color>";
    confirmPopup.SetActive(true);
}

void OnConfirmOk()
{
    HideConfirmPopup();
    if (!string.IsNullOrEmpty(_pendingEnhanceSlot))
        TryEnhance(_pendingEnhanceSlot);
    _pendingEnhanceSlot = null;
}

void OnConfirmCancel()
{
    HideConfirmPopup();
    _pendingEnhanceSlot = null;
}

void HideConfirmPopup()
{
    if (confirmPopup != null) confirmPopup.SetActive(false);
}
```

### 6. `Close()` 수정 — 팝업도 함께 닫기

기존 `Close()` (line 77-80)에 추가:
```csharp
public void Close()
{
    HideConfirmPopup();
    _pendingEnhanceSlot = null;
    if (panel != null) panel.SetActive(false);
    AudioManager.Instance?.PlaySFX("sfx_menu_close");
}
```

---

## 호출 진입점

| 경로 | 메서드 |
|------|--------|
| EnhanceUI 슬롯 버튼 (`AddSlotEntry`) | `RequestEnhance(slotName)` |
| 팝업 [확인] 버튼 | `OnConfirmOk()` |
| 팝업 [취소] 버튼 / ESC (`Close()`) | `OnConfirmCancel()` |

EnhanceUI는 `UIManager.Enhance`를 통해 노출되며, NPC 대화 액션 또는 기타 트리거에서 `uiManager.Enhance.Open(...)` 형태로 열린다. 강화 완료 콜백(`_onEnhance`)은 `GameUIWiring`이 아닌 호출 측에서 주입하므로 `GameUIWiring.cs`의 수정은 불필요하다.

---

## UI 와이어프레임

```
┌────────────────────────────────────────┐
│          ENHANCE                [X]    │
│  Gold: 12,000                          │
│ ┌──────────────────────────────────┐   │
│ │ Weapon  Iron Sword +4            │   │
│ │ Cost: 1200G | 60% success |      │   │
│ │ ▶ [ENHANCE]                      │   │
│ └──────────────────────────────────┘   │
│  ...                                   │
│  [Result text area]                    │
│                                        │
│  ╔══════════════════════════════════╗  │
│  ║  Enhance Iron Sword +4 → +5?    ║  │
│  ║                                  ║  │
│  ║  Cost: 1,200G                    ║  │
│  ║  Success: 60%                    ║  │
│  ║  Destroy: 5%                     ║  │  ← 붉은색 텍스트
│  ║                                  ║  │
│  ║     [Cancel]    [Confirm]        ║  │
│  ╚══════════════════════════════════╝  │
└────────────────────────────────────────┘
```

팝업은 EnhanceUI 패널 위에 오버레이로 표시된다. 팝업이 열려 있는 동안 슬롯 목록 버튼은 UI 레이어 차단으로 자연스럽게 비활성화된다(팝업이 상위 레이어). 별도 입력 블로킹 로직 불필요.

---

## 데이터 구조

데이터 구조 변경 없음.

- `EnhanceTable`의 `destroy` 필드는 이미 올바르게 정의되어 있다 (line 27-39).
- `ItemInstance.enhanceLevel` (`Assets/Scripts/Data/StatTypes.cs`, line 61)은 변경 없음.
- `SaveData.equipment` 직렬화 구조 변경 없음.

---

## 세이브 연동

직접적인 세이브 연동 변경 없음.

확인 팝업은 `TryEnhance()` 실행 전 단계를 추가하는 것이므로 실제 강화 결과(`enhanceLevel` 변경 또는 `equipment.Remove()`)는 기존 코드와 동일하게 발생한다. 강화 후 자동 저장은 기존 `_onEnhance` 콜백 체인이 처리한다.

---

## 검증 항목

- [ ] `info.destroy == 0`인 슬롯(+0~+3) 클릭 시 팝업 없이 즉시 강화 실행
- [ ] `info.destroy > 0`인 슬롯(+4~+9) 클릭 시 확인 팝업 표시
- [ ] 팝업 메시지에 장비명, 강화 단계, 성공률, 파괴율이 모두 표시됨
- [ ] [확인] 클릭 시 `TryEnhance()` 실행 → 결과 텍스트 업데이트 및 슬롯 목록 갱신
- [ ] [취소] 클릭 시 팝업 닫힘, 골드/장비 변화 없음
- [ ] ESC 또는 Close 버튼 클릭 시 팝업과 패널 모두 닫힘, `_pendingEnhanceSlot` 초기화됨
- [ ] 팝업이 열린 상태에서 다른 슬롯 버튼이 클릭되지 않음 (레이어 차단 확인)
- [ ] 기존 sfx 재생 유지 (sfx_enchant_success / sfx_enchant_fail / sfx_enchant_critfail)
- [ ] `GameConfig.EnhanceCost`와 `EnhanceTable.gold`의 값이 팝업에 올바르게 표시됨

---

## Test Plan

EditMode 테스트 위치: `Assets/Tests/EditMode/EnhanceUIConfirmTests.cs`

`EnhanceUI`는 MonoBehaviour이므로 순수 로직 단위 테스트는 `GetEnhanceInfo()` static 메서드를 통해 수행한다.

```csharp
// 1. 파괴 확률이 없는 레벨에서는 팝업 불필요 확인
[Test]
public void GetEnhanceInfo_Level0To3_HasZeroDestroyChance()
{
    for (int level = 0; level <= 3; level++)
    {
        var info = EnhanceUI.GetEnhanceInfo(level);
        Assert.AreEqual(0f, info.destroy,
            $"Level {level} should have 0 destroy chance, got {info.destroy}");
    }
}

// 2. +4 이상에서 파괴 확률 존재 확인 (팝업 트리거 조건)
[Test]
public void GetEnhanceInfo_Level4AndAbove_HasDestroyChance()
{
    for (int level = 4; level <= 9; level++)
    {
        var info = EnhanceUI.GetEnhanceInfo(level);
        Assert.Greater(info.destroy, 0f,
            $"Level {level} should have destroy chance > 0, got {info.destroy}");
    }
}

// 3. EnhanceTable 경계 - 최대 레벨(10) 이상은 (0,0,0) 반환
[Test]
public void GetEnhanceInfo_AtOrAboveMaxLevel_ReturnsZero()
{
    var info = EnhanceUI.GetEnhanceInfo(10);
    Assert.AreEqual(0f, info.success);
    Assert.AreEqual(0f, info.destroy);
    Assert.AreEqual(0, info.gold);
}

// 4. 파괴 확률이 destroy * 100 % 로 정확히 표시되는지 포맷 확인
[Test]
public void GetEnhanceInfo_Level9_DestroyIs30Percent()
{
    var info = EnhanceUI.GetEnhanceInfo(9);
    Assert.AreEqual(0.30f, info.destroy, 0.001f);
}
```

PlayMode 시나리오(수동):
1. +4 이상 장비 장착 후 강화 패널 열기 → 슬롯 클릭 → 팝업 확인
2. 팝업 [취소] → 골드/장비 변화 없음 확인
3. 팝업 [확인] → 강화 결과 표시 및 골드 감소 확인
4. +3 이하 장비 클릭 → 팝업 없이 즉시 실행 확인
