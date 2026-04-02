# SPEC-R011: 아이템/스킬 툴팁 시스템

## 목적
인벤토리 아이템과 스킬 슬롯에 마우스를 올리면 상세 정보를 표시하는 범용 툴팁 시스템.

## 현재 상태
- `HUD.cs:56-59` — 스킬 툴팁 패널/텍스트 필드가 이미 SerializeField로 존재하나 로직 미구현.
- `InventoryUI.cs` — 아이템 슬롯 표시 있으나 호버 시 상세 정보 없음.
- `Assets/Art/Sprites/UI/tooltip_bg.png` — 툴팁 배경 스프라이트 이미 존재 (R-025에서 생성).

## 구현 명세

### 새 파일
- `Assets/Scripts/UI/TooltipUI.cs` — 범용 툴팁 매니저

### 수정 파일
- `Assets/Scripts/UI/InventoryUI.cs` — 아이템 슬롯에 호버 이벤트 추가
- `Assets/Scripts/UI/HUD.cs` — 기존 skillTooltipPanel 로직 연결

### 데이터 구조

```csharp
// TooltipUI.cs — 싱글턴 패턴
public class TooltipUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] TextMeshProUGUI statsText;
    [SerializeField] Image iconImage;
    [SerializeField] Image gradeBar; // 등급 색상 표시
    
    static TooltipUI _instance;
    
    public static void ShowItem(ItemDef item, Vector2 screenPos);
    public static void ShowSkill(SkillDef skill, int level, Vector2 screenPos);
    public static void Hide();
}
```

### UI 와이어프레임
```
┌──────────────────────┐
│ [아이콘] 아이템 이름    │  ← titleText (등급별 색상)
│─────────────────────│
│ 설명 텍스트            │  ← descText
│                      │
│ ATK +10  DEF +5      │  ← statsText (스탯 보너스)
│ Grade: Rare          │
│ Stack: 1/10          │
└──────────────────────┘
```

### 로직

1. **아이템 툴팁:**
   - 이름 (등급별 색상: common=흰, uncommon=녹, rare=청, epic=보라, legendary=금)
   - 설명
   - 스탯 보너스 (atk, def, maxHp, maxMp, crit, spd)
   - 판매가, 스택 수

2. **스킬 툴팁:**
   - 스킬 이름 + 트리(melee/ranged/magic)
   - 설명
   - 현재 레벨 / 최대 레벨
   - MP 소모, 쿨다운, 데미지/효과 수치
   - 다음 레벨 미리보기 (있으면)

3. **위치:**
   - 마우스 커서 우측 하단에 표시.
   - 화면 밖으로 넘어가면 좌측/상단으로 플립.

4. **InventoryUI 슬롯에 IPointerEnterHandler / IPointerExitHandler 추가:**
   ```csharp
   // 슬롯 프리팹에 EventTrigger 또는 인터페이스 구현
   public void OnPointerEnter(PointerEventData e) => TooltipUI.ShowItem(item, e.position);
   public void OnPointerExit(PointerEventData e) => TooltipUI.Hide();
   ```

### 세이브 연동
없음. UI 전용.

## 호출 진입점
- 인벤토리 UI: 아이템 슬롯 호버 시 자동 표시.
- HUD: 스킬 바 슬롯 호버 시 자동 표시.

## 테스트 항목
- [ ] 아이템 호버 시 툴팁이 표시되는지
- [ ] 호버 해제 시 툴팁이 사라지는지
- [ ] 등급별 이름 색상이 올바른지
- [ ] 화면 경계에서 플립이 동작하는지
- [ ] 스킬 슬롯 호버 시 스킬 정보가 표시되는지
- [ ] 빈 슬롯에서는 툴팁이 나타나지 않는지
