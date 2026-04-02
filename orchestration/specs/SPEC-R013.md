# SPEC-R013: 인벤토리 필터/정렬 강화

## 목적
인벤토리에 타입별 필터(전체/무기/방어구/소모품/재료)와 다중 정렬 기준을 추가한다.

## 현재 상태
- `InventoryUI.cs` — sortButton 존재, OnSortCallback 연결됨.
- `InventorySystem.cs` — SortItems() 메서드 존재 (기본 정렬만).
- 필터 기능 없음. 정렬 기준 선택 UI 없음.

## 구현 명세

### 수정 파일
- `Assets/Scripts/UI/InventoryUI.cs` — 필터 버튼 행 + 정렬 드롭다운
- `Assets/Scripts/Systems/InventorySystem.cs` — 필터/정렬 로직

### UI 와이어프레임
```
[전체] [무기] [방어] [소모] [재료]    ← 필터 탭 버튼 행
정렬: [이름순 ▼]                     ← 드롭다운 (또는 순환 버튼)
┌──┬──┬──┬──┬──┐
│  │  │  │  │  │                    ← 기존 5열 그리드
├──┼──┼──┼──┼──┤
│  │  │  │  │  │                    ← 필터 적용된 항목만 표시
└──┴──┴──┴──┴──┘
```

### 데이터 구조

```csharp
// InventoryUI.cs에 추가
public enum ItemFilter { All, Weapon, Armor, Consumable, Material }
public enum ItemSortMode { Name, Grade, Type, Recent }

[Header("Filter")]
[SerializeField] Button[] filterButtons;   // 5개: 전체/무기/방어/소모/재료
[SerializeField] Button sortModeButton;    // 클릭마다 순환
[SerializeField] TextMeshProUGUI sortModeText;

ItemFilter _currentFilter = ItemFilter.All;
ItemSortMode _currentSort = ItemSortMode.Name;
```

### 로직

1. **필터:**
   - ItemDef.type에 따라 분류: "weapon" → Weapon, "helmet"/"armor"/"boots"/"accessory" → Armor, "potion"/"food" → Consumable, "material" → Material
   - All은 전체 표시.
   - 필터 적용 후 빈 슬롯은 빈칸 표시 (슬롯 수 유지).

2. **정렬:**
   - Name: 이름 알파벳순
   - Grade: common < uncommon < rare < epic < legendary
   - Type: 무기 → 방어구 → 소모품 → 재료
   - Recent: 인벤토리에 추가된 순서 (역순, 최신 먼저)

3. **필터 탭 시각:**
   - 선택된 탭: ActiveTabColor 배경
   - 미선택: InactiveTabColor 배경

4. **정렬 순환:**
   - sortModeButton 클릭 → Name → Grade → Type → Recent → Name...
   - sortModeText에 현재 모드 표시.

### 세이브 연동
없음. 필터/정렬 상태는 세션 전용.

## 호출 진입점
- 인벤토리 UI 내부: 필터 탭 클릭 / 정렬 버튼 클릭.
- 키보드: Tab 키로 인벤토리 열기 → 내부 필터/정렬 사용.

## 테스트 항목
- [ ] Weapon 필터 시 무기만 표시되는지
- [ ] All 필터 시 전체 표시
- [ ] Grade 정렬이 올바른 순서인지
- [ ] 필터 + 정렬 조합이 정상 동작하는지
- [ ] 빈 인벤토리에서 필터 변경 시 크래시 없는지
- [ ] 필터 후 아이템 사용/장착이 정상 동작하는지
