# SPEC-R022: 업적 시스템

## 목적
플레이어 마일스톤(첫 몬스터 처치, 레벨 달성, 퀘스트 완료 등)을 추적하고 달성 시 보상과 UI 배지를 표시한다.

## 현재 상태
- `GameEvents.cs` — MonsterKillEvent, LevelUpEvent, QuestCompleteEvent 등 이벤트 존재.
- `EventBus.cs` — 이벤트 구독/발행 시스템 존재.
- 업적 시스템 없음.

## 구현 명세

### 새 파일
- `Assets/Scripts/Systems/AchievementSystem.cs` — 업적 추적 + 보상 지급
- `Assets/Scripts/UI/AchievementUI.cs` — 업적 목록 패널 + 달성 팝업

### 데이터 구조

```csharp
// AchievementSystem.cs
public class AchievementSystem
{
    public struct AchievementDef
    {
        public string id, name, description, icon;
        public string eventType;      // "monster_kill", "level_up", "quest_complete", "craft", "gold"
        public int requiredCount;     // 목표 수량
        public string rewardType;     // "gold", "item", "title"
        public string rewardId;       // 아이템 ID 또는 칭호
        public int rewardAmount;
    }
    
    Dictionary<string, AchievementDef> _definitions;
    Dictionary<string, int> _progress;      // achievementId → current count
    HashSet<string> _completed;
    
    public void Init(AchievementDef[] defs);
    public void OnEvent(string eventType, int count = 1);
    public bool IsCompleted(string achievementId);
    public (int current, int required) GetProgress(string achievementId);
}
```

### 초기 업적 목록 (하드코딩 → 추후 JSON)
| ID | 이름 | 조건 | 보상 |
|----|------|------|------|
| first_blood | First Blood | 몬스터 1마리 처치 | 50 gold |
| monster_hunter | Monster Hunter | 몬스터 100마리 처치 | rare weapon |
| level_10 | Rising Star | 레벨 10 달성 | 100 gold |
| level_25 | Veteran | 레벨 25 달성 | epic accessory |
| quest_5 | Errand Runner | 퀘스트 5개 완료 | 200 gold |
| crafter | Artisan | 아이템 20개 제작 | rare recipe |
| rich | Wealthy | 골드 10,000 보유 | title "부자" |
| all_npcs | Social Butterfly | NPC 전원 대화 | special item |

### UI 와이어프레임

**업적 팝업 (달성 시):**
```
┌──────────────────────────────┐
│ 🏆 업적 달성!                │
│ "First Blood"                │
│ 보상: 50 Gold                │
└──────────────────────────────┘
 (3초 후 자동 닫힘, 상단 중앙)
```

**업적 목록 (별도 패널):**
```
[업적] ← 탭 버튼 (I키 인벤토리 옆)
┌────────────────────────────────┐
│ ✅ First Blood      — 1/1    │
│ 🔲 Monster Hunter   — 45/100 │
│ ✅ Rising Star      — 10/10  │
│ 🔲 Veteran          — 10/25  │
└────────────────────────────────┘
```

### 로직

1. **이벤트 구독:** EventBus에서 MonsterKillEvent, LevelUpEvent 등 구독.
2. **진행률 갱신:** `_progress[achievementId]++`, 목표 도달 시 `_completed` 추가 + 보상 지급.
3. **보상 지급:** gold → PlayerStats.Gold, item → InventorySystem.AddItem.
4. **달성 팝업:** EventBus.Emit(AchievementUnlockedEvent) → AchievementUI가 구독.

### 세이브 연동
- SaveData에 `Dictionary<string, int> achievementProgress`, `HashSet<string> completedAchievements` 추가.

## 호출 진입점
- 자동: EventBus 이벤트 구독으로 추적.
- UI: `J` 키로 업적 패널 열기. 달성 팝업은 자동.

## 테스트 항목
- [ ] 몬스터 처치 시 카운트 증가
- [ ] 목표 달성 시 보상 지급
- [ ] 이미 달성된 업적은 중복 보상 없음
- [ ] 진행률이 세이브/로드 후 유지
- [ ] 달성 팝업이 3초 후 사라짐
