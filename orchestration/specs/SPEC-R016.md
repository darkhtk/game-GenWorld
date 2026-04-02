# SPEC-R016: 장비 비교 팝업

## 목적
장비 장착 전 현재 장비와 새 장비의 스탯을 나란히 비교하여 더 나은 선택을 할 수 있게 한다.

## 현재 상태
- `InventoryUI.cs:54-57` — tooltipPanel 존재 (이름/설명/스탯 텍스트).
- 장비 슬롯 5개: weapon, helmet, armor, boots, accessory.
- 장비 장착 시 `OnEquipCallback(slotIndex)` 호출.
- **문제:** 장착 시 현재 장비와의 스탯 차이를 확인할 수 없음.

## 구현 명세

### 수정 파일
- `Assets/Scripts/UI/InventoryUI.cs` — 비교 팝업 로직 추가

### UI 와이어프레임
```
┌─────────────────────────────────────────┐
│        장비 비교                         │
├──────────────────┬──────────────────────┤
│ 현재: Iron Sword │ 새로: Steel Sword     │
│ ATK: 10         │ ATK: 15  (+5) ▲      │
│ DEF: 2          │ DEF: 1   (-1) ▼      │
│ SPD: 0          │ SPD: 3   (+3) ▲      │
│ CRIT: 5%        │ CRIT: 8% (+3%) ▲     │
├──────────────────┴──────────────────────┤
│     [장착]              [취소]           │
└─────────────────────────────────────────┘
```

### 데이터 구조

```csharp
// InventoryUI.cs에 추가
[Header("Equipment Compare")]
[SerializeField] GameObject comparePanel;
[SerializeField] TextMeshProUGUI compareCurrentName;
[SerializeField] TextMeshProUGUI compareNewName;
[SerializeField] TextMeshProUGUI compareCurrentStats;
[SerializeField] TextMeshProUGUI compareNewStats;
[SerializeField] TextMeshProUGUI compareDiffStats;    // +5 ▲ / -1 ▼
[SerializeField] Button compareEquipButton;
[SerializeField] Button compareCancelButton;
```

### 로직

1. **트리거:** 인벤토리에서 장비 타입 아이템 클릭 시 (기존 즉시 장착 → 비교 팝업).
2. **스탯 비교 계산:**
   ```csharp
   void ShowCompare(ItemDef newItem, ItemDef currentEquipped)
   {
       // 비교 대상 스탯: atk, def, maxHp, maxMp, crit, spd
       // 각 스탯 diff 계산: new - current
       // diff > 0: 녹색 + ▲
       // diff < 0: 빨간색 + ▼
       // diff == 0: 회색 (표시 안 함)
   }
   ```
3. **현재 장비 없는 슬롯:** "없음" 표시, 모든 스탯이 + 로 표시.
4. **장착 버튼:** 기존 OnEquipCallback 호출 + 팝업 닫기.
5. **취소 버튼:** 팝업만 닫기.
6. **ShopUI 연동:** 상점에서 장비 구매 시에도 동일한 비교 팝업 표시 (선택사항).

### 색상
| 상태 | 색상 | 비고 |
|------|------|------|
| 상승 (▲) | (0.4, 1.0, 0.4) — 녹색 | |
| 하락 (▼) | (1.0, 0.4, 0.4) — 빨간색 | |
| 동일 | 회색 | 표시 생략 가능 |

### 세이브 연동
없음. UI 전용.

## 호출 진입점
- 인벤토리 UI: 장비 아이템 슬롯 클릭 시 comparePanel 표시.
- (선택) 상점 UI: 장비 구매 확인 시.

## 테스트 항목
- [ ] 장비 클릭 시 비교 팝업이 나타나는지
- [ ] 스탯 차이가 올바르게 계산되는지
- [ ] 상승은 녹색 ▲, 하락은 빨간색 ▼ 표시
- [ ] 현재 장비 없을 때 "없음" 표시
- [ ] 장착 버튼 클릭 시 실제 장착 + 팝업 닫힘
- [ ] 취소 시 장착 안 되고 팝업만 닫힘
- [ ] 비장비 아이템 클릭 시 비교 팝업 미표시
