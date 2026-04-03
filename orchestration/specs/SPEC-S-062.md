# SPEC-S-062: ShopUI 구매 실패 피드백

> **우선순위:** P3
> **태그:** 🔧 안정성 개선
> **관련 파일:**
> - `Assets/Scripts/UI/ShopUI.cs`
> - `Assets/Scripts/Systems/InventorySystem.cs`
> - `Assets/Scripts/UI/HUD.cs`
> - `Assets/Scripts/Core/GameUIWiring.cs`

## 배경

ShopUI에서 아이템 구매 실패 시 플레이어에게 어떠한 피드백도 제공되지 않는다.
골드 부족의 경우 버튼 비활성화(`btn.interactable = false`)로 예방은 되지만, 인벤토리 풀(full) 상태는
버튼이 활성화된 상태에서 구매를 시도해도 `AddItem()` 반환값 `overflow > 0` 인 경우
아무 소리, 텍스트, 애니메이션 없이 조용히 실패한다.

플레이어 입장에서는 버튼을 눌렀는데 아무 일도 일어나지 않는 것처럼 느껴져 혼란을 준다.

## 현재 동작

### 골드 부족

`ShopUI.AddShopEntry()` (line 91, 109):
```csharp
bool canAfford = gold >= def.shopPrice;
btn.interactable = canAfford;
```
버튼이 회색으로 표시되고 클릭이 차단된다. 실패 사유는 표시되지 않는다.
(`Refresh()` 호출 시 골드 변화를 감지해 자동 갱신.)

### 인벤토리 풀

`ShopUI.BuyItem()` (line 117-129):
```csharp
int overflow = _inventory.AddItem(itemId, 1, stackable, maxStack);
if (overflow == 0)
{
    _spendGold(price);
    AudioManager.Instance?.PlaySFX("sfx_coin");
    Refresh();
}
// overflow > 0 인 경우: 아무 처리 없음 — 조용한 실패
```
`InventorySystem.AddItem()`은 삽입하지 못한 수량을 반환하므로 `overflow > 0`이면 인벤이 꽉 찬 것이다.

### InventorySystem 용량 확인

`InventorySystem.cs` 에는 사전 확인용 `IsFull` / `CanAdd` 헬퍼가 없다.
`OccupiedSlots` 프로퍼티(line 8-16)는 존재하지만, 스택 가능 아이템의 경우 슬롯이 남아 있어도
`maxStack` 한도 도달 시 `overflow > 0`이 반환될 수 있다.

## 목표 동작

| 실패 사유 | 목표 동작 |
|-----------|-----------|
| 골드 부족 | HUD 히스토리 로그에 `"골드 부족!"` 출력 (빨간색), `sfx_error` 재생. 버튼 비활성화는 유지. |
| 인벤 풀 | HUD 히스토리 로그에 `"인벤토리가 가득 찼습니다!"` 출력 (노란색), `sfx_error` 재생. |

HUD의 `AddHistoryEntry(string text, Color color)` 메서드를 재사용한다 — 이미 다른 시스템(전투 보상, 퀘스트 완료, 사망 등)에서 동일 패턴으로 사용 중.

## 구현 상세

### 1. ShopUI에 HUD 참조 추가

`ShopUI.cs`에 `HUD` 참조 필드를 추가하고 `Open()` 호출부에서 주입한다.

```csharp
// ShopUI.cs — 필드 추가
HUD _hud;

// Open() 시그니처 변경
public void Open(InventorySystem inventory, Dictionary<string, ItemDef> itemDefs,
    Func<int> getGold, Action<int> spendGold, HUD hud = null)
{
    _hud = hud;
    // ... 기존 코드 유지
}
```

`Toggle()` 도 동일하게 `hud` 파라미터를 추가한다.

### 2. BuyItem() 실패 분기 처리

`ShopUI.BuyItem()` (line 117-129)을 다음으로 교체:

```csharp
void BuyItem(string itemId, int price, bool stackable, int maxStack)
{
    if (_getGold == null || _spendGold == null || _inventory == null) return;

    if (_getGold() < price)
    {
        AudioManager.Instance?.PlaySFX("sfx_error");
        _hud?.AddHistoryEntry("골드 부족!", Color.red);
        return;
    }

    int overflow = _inventory.AddItem(itemId, 1, stackable, maxStack);
    if (overflow == 0)
    {
        _spendGold(price);
        AudioManager.Instance?.PlaySFX("sfx_coin");
        Refresh();
    }
    else
    {
        AudioManager.Instance?.PlaySFX("sfx_error");
        _hud?.AddHistoryEntry("인벤토리가 가득 찼습니다!", Color.yellow);
    }
}
```

골드 부족 분기는 `AddShopEntry()`의 `btn.interactable = canAfford` 가드가 있으므로 정상 플레이에서는
도달하지 않는다. 그러나 외부에서 골드가 감소하는 경쟁 조건(퀘스트 보상 차감 등)을 대비해
방어적으로 처리한다.

### 3. GameUIWiring에서 HUD 주입

`GameUIWiring.cs`에서 ShopUI를 열 때 HUD를 넘긴다. 현재 ShopUI.Open/Toggle은
`DialogueController` 또는 유사한 NPC 인터랙션 코드에서 호출될 가능성이 높다.

해당 호출부를 찾아 `hud` 인자를 추가한다:

```csharp
// 호출 예시 (NPC 상점 오픈 코드에서)
_uiManager.Shop.Open(_inventory, _data.Items,
    () => _playerState.Gold,
    amount => { _playerState.Gold -= amount; EventBus.Emit(new GoldChangeEvent { gold = _playerState.Gold }); },
    _uiManager.Hud);   // <-- hud 추가
```

### 4. InventorySystem — CanAdd 헬퍼 (선택적, 사전 확인용)

인벤이 가득 찼는지 사전 확인을 원할 경우 `InventorySystem.cs`에 헬퍼를 추가할 수 있다.
그러나 현재 `AddItem()` 반환값 패턴이 명확하므로 이 스펙에서는 추가하지 않는다.
`overflow > 0` 분기 처리만으로 충분하다.

## 호출 진입점

| 트리거 | 위치 |
|--------|------|
| 상점 아이템 클릭 | `ShopUI` — `AddShopEntry()` 에서 버튼 onClick에 등록된 `BuyItem()` 람다 |
| 상점 열기 | NPC 인터랙션 코드 (`DialogueController` 또는 NPC MonoBehaviour) → `ShopUI.Open()` / `Toggle()` |

## 데이터 구조

변경 없음. `ItemDef`, `ItemInstance`, `InventorySystem`, `PlayerStats` 구조체/클래스 수정 불필요.

## 세이브 연동

영향 없음. 구매 실패 시 골드·인벤토리 상태가 변경되지 않으므로 저장 이벤트가 발생하지 않는다.

## 검증 항목

- [ ] 인벤토리 풀(20/20 슬롯) 상태에서 상점 아이템 클릭 시 히스토리 로그에 `"인벤토리가 가득 찼습니다!"` 표시
- [ ] 인벤 풀 구매 실패 시 `sfx_error` 사운드 재생됨
- [ ] 인벤 풀 구매 실패 시 골드가 차감되지 않음
- [ ] 인벤 풀 구매 실패 시 `Refresh()` 가 호출되지 않음 (UI 상태 유지)
- [ ] 스택 가능 아이템이 maxStack 한도에 도달하고 빈 슬롯도 없을 때 동일하게 실패 피드백 발생
- [ ] 골드가 충분하고 인벤에 공간이 있을 때 정상 구매 흐름 (`sfx_coin`, 골드 차감, Refresh) 변화 없음
- [ ] HUD가 null인 경우(`_hud == null`) 피드백 없이 조용히 실패하지 않음 — sfx_error는 HUD와 무관하게 재생됨
- [ ] `ShopUI.Open()` 에 `hud` 미전달 시 기본값 null이 적용되고 NullReferenceException 없음

## Test Plan

`Assets/Tests/EditMode/ShopUITests.cs` 신규 생성 (ShopUI가 MonoBehaviour이므로 EditMode에서 직접 테스트 불가 — `InventorySystem`과 `BuyItem` 로직을 분리한 헬퍼로 추출하거나 PlayMode 테스트로 작성).

대신 로직 핵심인 `InventorySystem.AddItem()` 반환값에 의존하는 부분은 기존
`InventorySystemTests.cs`에서 커버된다고 간주하고, 아래 항목을 추가 확인한다:

| 테스트 케이스 | 검증 내용 |
|---------------|-----------|
| `AddItem_WhenFull_ReturnsOverflow` | 슬롯 20/20 상태에서 `AddItem("sword", 1, false, 1)` 반환값 == 1 |
| `AddItem_StackFull_ReturnsOverflow` | 스택 가능 아이템이 모든 슬롯에서 maxStack 도달 시 overflow 반환 |
| `AddItem_StackableWithSpace_Returns0` | 같은 아이템 스택에 공간 있을 때 overflow == 0 |

ShopUI 피드백 경로(HUD 호출, sfx)는 PlayMode 씬 테스트 또는 수동 QA로 검증한다.
