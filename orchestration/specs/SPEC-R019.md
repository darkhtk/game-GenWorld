# SPEC-R019: NPC 일과 시스템

## 목적
NPC가 시간대별로 다른 위치에서 활동하여 마을에 생동감을 부여한다.

## 현재 상태
- `VillageNPC.cs` — patrol(cx, cy, radius) 기반 랜덤 순찰만 존재.
- `NpcDef.cs:17` — `NpcPatrol { cx, cy, radius }` — 단일 위치 순찰 데이터.
- 시간 시스템 없음. NPC는 항상 같은 위치에서 순찰.

## 구현 명세

### 새 파일
- `Assets/Scripts/Systems/TimeSystem.cs` — 게임 내 시간 관리 (순수 C# 클래스)

### 수정 파일
- `Assets/Scripts/Entities/VillageNPC.cs` — 시간대별 위치 이동
- `Assets/Scripts/Data/NpcDef.cs` — 일과 데이터 구조 추가

### 데이터 구조

```csharp
// TimeSystem.cs
public class TimeSystem
{
    public float GameHour { get; private set; } // 0~24
    public string Period => GameHour switch  // dawn/morning/afternoon/evening/night
    {
        < 6 => "night", < 9 => "dawn", < 12 => "morning",
        < 17 => "afternoon", < 20 => "evening", _ => "night"
    };
    
    const float REAL_SECONDS_PER_GAME_HOUR = 60f; // 1실시간분 = 1게임시간
    // 24게임시간 = 24실시간분 (한 사이클)
    
    public void Update(float deltaTime);
}

// NpcDef.cs에 추가
[Serializable]
public class NpcSchedule
{
    public string period;     // "morning", "afternoon", "evening", "night"
    public int cx, cy;        // 위치 (타일 좌표)
    public int radius;        // 순찰 반경
    public string activity;   // "working", "resting", "shopping" (UI 표시용)
}

// NpcDef에 추가
public NpcSchedule[] schedule;
```

### JSON 예시 (npcs.json)
```json
{
  "id": "blacksmith",
  "schedule": [
    { "period": "morning", "cx": 50, "cy": 30, "radius": 2, "activity": "working" },
    { "period": "afternoon", "cx": 50, "cy": 30, "radius": 2, "activity": "working" },
    { "period": "evening", "cx": 45, "cy": 35, "radius": 3, "activity": "resting" },
    { "period": "night", "cx": 48, "cy": 40, "radius": 1, "activity": "sleeping" }
  ]
}
```

### 로직

1. **TimeSystem:** GameManager에서 Update마다 호출. `GameHour` 증가.
2. **VillageNPC.Update():**
   - TimeSystem.Period 변경 감지.
   - 새 period에 해당하는 schedule 항목 찾기.
   - `_patrolCenter`를 새 좌표로 이동 (걸어서 이동, 순간이동 아님).
   - 도착 후 새 radius로 순찰.
3. **schedule 없는 NPC:** 기존 patrol 유지 (하위 호환).
4. **"sleeping" 활동:** NPC 정지 + 대화 시 "zzz..." 메시지.

### 세이브 연동
- `TimeSystem.GameHour`를 SaveData에 추가.
- NPC 현재 위치는 저장 불필요 (로드 시 GameHour 기준 위치 계산).

## 호출 진입점
- 자동. TimeSystem.Update()에서 시간 흐름 → NPC 위치 변경.
- HUD: 현재 시간 표시 추가 (선택사항).

## 테스트 항목
- [ ] 시간 경과에 따라 GameHour가 증가하는지
- [ ] Period 전환 시 NPC가 새 위치로 이동하는지
- [ ] schedule 없는 NPC가 기존대로 동작하는지
- [ ] "sleeping" NPC에게 대화 시도 시 적절한 반응
- [ ] GameHour가 세이브/로드 후 유지되는지
