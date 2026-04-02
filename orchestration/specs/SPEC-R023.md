# SPEC-R023: 장비 강화 실패/파괴 시스템

## 목적
장비 강화에 확률적 실패와 파괴 메커니즘을 추가하여 장비 진행에 리스크/보상 긴장감을 부여한다.

## 현재 상태
- `EnhanceUI.cs` — 강화 UI 존재 (슬롯 선택 → 골드 지불 → 강화 콜백).
- 현재 강화는 100% 성공. 실패/파괴 로직 없음.
- `ItemDef.cs` — 아이템 정의에 enhanceLevel 등 관련 필드 없음.

## 구현 명세

### 수정 파일
- `Assets/Scripts/UI/EnhanceUI.cs` — 확률 표시 + 결과 연출
- `Assets/Scripts/Data/ItemDef.cs` — enhanceLevel 필드 (또는 ItemInstance에)

### 데이터 구조

```csharp
// ItemInstance (인벤토리 아이템 인스턴스)에 추가
public int EnhanceLevel { get; set; } = 0;  // +0 ~ +10

// 강화 확률 테이블
public static class EnhanceTable
{
    // enhanceLevel → (successRate, destroyRate, goldCost)
    static readonly (float success, float destroy, int gold)[] Table = {
        (1.0f,  0.0f,  100),   // +0 → +1: 100%
        (0.9f,  0.0f,  200),   // +1 → +2: 90%
        (0.8f,  0.0f,  400),   // +2 → +3: 80%
        (0.7f,  0.0f,  700),   // +3 → +4: 70%
        (0.6f,  0.05f, 1200),  // +4 → +5: 60%, 5% 파괴
        (0.5f,  0.10f, 2000),  // +5 → +6: 50%, 10% 파괴
        (0.4f,  0.15f, 3500),  // +6 → +7: 40%, 15% 파괴
        (0.3f,  0.20f, 5000),  // +7 → +8: 30%, 20% 파괴
        (0.2f,  0.25f, 8000),  // +8 → +9: 20%, 25% 파괴
        (0.1f,  0.30f, 12000), // +9 → +10: 10%, 30% 파괴
    };
    
    public static (float success, float destroy, int gold) Get(int level);
}
```

### 강화 스탯 보너스
```
+N 강화 시: 기본 스탯 × (1 + N × 0.1)
예: ATK 10인 +5 무기 → ATK 15
```

### UI 와이어프레임
```
┌───────────────────────────────┐
│ 강화: Steel Sword +3          │
│                               │
│ ATK: 13 → 14.3 (+1.3)        │
│                               │
│ 성공률: 70%                    │
│ 파괴 확률: 0%                  │
│ 비용: 700 Gold                 │
│                               │
│ [강화!]        [취소]          │
└───────────────────────────────┘
```

**결과 연출:**
- 성공: 녹색 플래시 + "강화 성공! +4" 텍스트
- 실패: 주황색 흔들림 + "강화 실패..." (레벨 유지)
- 파괴: 빨간색 + "장비 파괴!" + 아이템 인벤토리에서 제거

### 로직

1. **강화 시도:**
   - RNG roll → [0, 1) 구간:
     - [0, successRate): 성공 → EnhanceLevel++
     - [successRate, successRate + destroyRate): 파괴 → 아이템 삭제
     - [successRate + destroyRate, 1): 실패 → 레벨 유지
   - 골드 차감 (성공/실패 무관).

2. **최대 강화:** +10. 이미 +10이면 강화 버튼 비활성.

3. **이름 표시:** "Steel Sword +3" 형태로 인벤토리/장비 표시.

### 세이브 연동
- ItemInstance.EnhanceLevel을 SaveData에 포함 (인벤토리 직렬화에 이미 포함되어야 함).

## 호출 진입점
- EnhanceUI: NPC 상호작용 → 강화 메뉴 → 슬롯 선택 → 강화 버튼.

## 테스트 항목
- [ ] +0→+1 강화가 100% 성공하는지
- [ ] +5 이상에서 파괴 확률이 작동하는지
- [ ] 파괴 시 아이템이 인벤토리에서 사라지는지
- [ ] 실패 시 레벨 유지 + 골드만 차감
- [ ] +10에서 강화 불가
- [ ] EnhanceLevel이 세이브/로드 후 유지
- [ ] 강화된 아이템 스탯이 올바르게 적용되는지
