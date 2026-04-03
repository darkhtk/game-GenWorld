# SPEC-S-081: InventorySystem 중복 아이템 ID 병합 검증

> **우선순위:** P3
> **유형:** 🔧 검증
> **방향:** stabilize — 기존 로직 정확성 확인

---

## 목적

InventorySystem.AddItem()의 스택 병합 로직이 모든 경계 조건에서 올바르게 동작하는지 검증.

## 현재 구현 (InventorySystem.cs:27-53)

AddItem은 2-pass 방식:
1. **Pass 1:** 동일 itemId 기존 슬롯 탐색 → maxStack 미만이면 채움
2. **Pass 2:** 잔여분 빈 슬롯에 배치 → maxStack 단위로 분할

반환값: 인벤토리에 넣지 못한 잔여 수량 (overflow)

## 검증 항목

| # | 시나리오 | 예상 결과 |
|---|---------|-----------|
| 1 | stackable 아이템 10개 추가 (maxStack=20, 기존 슬롯 15개) | 기존 슬롯 20 + 새 슬롯 5 |
| 2 | stackable 아이템 추가 시 인벤토리 풀 | overflow > 0 반환 |
| 3 | non-stackable 아이템 3개 추가 | 3개 별도 슬롯 |
| 4 | count=0 또는 음수 AddItem 호출 | 0 반환, 변경 없음 |
| 5 | 동일 ID 여러 슬롯에 분산 시 병합 우선순위 | 첫 번째 미충족 슬롯부터 채움 |

## 호출 진입점

- CombatRewardHandler → InventorySystem.AddItem (몬스터 드롭)
- ShopUI.BuyItem → InventorySystem.AddItem (상점 구매)
- QuestSystem.CompleteQuest → InventorySystem.AddItem (퀘스트 보상)
- LootSystem.PickupItem → InventorySystem.AddItem (필드 아이템)

## 세이브 연동

- InventorySystem.SaveData()에서 슬롯별 {itemId, count} 직렬화
- 로드 후 스택 무결성 확인 필요 (합산값 = 저장 시 합산값)

## 데이터 구조

```csharp
public class InventorySlot
{
    public string itemId;
    public int count;
}
```

## 수정 범위

- **코드 수정:** 없음 (검증만) 또는 경계 조건 가드 추가
- **테스트:** Assets/Tests/EditMode/InventorySystemTests.cs에 시나리오별 테스트 추가 권장
