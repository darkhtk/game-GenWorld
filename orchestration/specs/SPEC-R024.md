# SPEC-R024: 월드 이벤트 시스템

## 목적
일정 시간 간격으로 특수 이벤트(엘리트 몬스터 출현, 보너스 드롭, 침략 이벤트)가 발생하여 게임플레이에 변화를 준다.

## 의존성
- R-019 (TimeSystem) — 게임 내 시간 기반 이벤트 트리거.

## 구현 명세

### 새 파일
- `Assets/Scripts/Systems/WorldEventSystem.cs` — 이벤트 스케줄러 + 실행

### 데이터 구조

```csharp
public class WorldEventSystem
{
    public struct WorldEventDef
    {
        public string id, name, description;
        public string type;          // "elite_spawn", "bonus_drops", "invasion", "merchant"
        public float duration;       // 게임 시간 (시간 단위)
        public float minInterval;    // 최소 발생 간격 (게임 시간)
        public float chance;         // 발생 확률 (체크마다)
        public string regionId;      // 특정 리전 (null이면 전체)
        public string[] params;      // 이벤트별 추가 파라미터
    }
    
    WorldEventDef[] _definitions;
    WorldEventDef? _activeEvent;
    float _eventStartHour;
    float _lastCheckHour;
    
    public void Update(float gameHour);
    public bool IsEventActive => _activeEvent != null;
    public WorldEventDef? ActiveEvent => _activeEvent;
}
```

### 초기 이벤트 목록
| ID | 이름 | 타입 | 지속 | 간격 | 효과 |
|----|------|------|------|------|------|
| blood_moon | Blood Moon | elite_spawn | 2h | 8h | 엘리트 몬스터 3마리 스폰 (ATK/HP 2x) |
| golden_hour | Golden Hour | bonus_drops | 1h | 6h | 드롭률 2배, 골드 1.5배 |
| goblin_raid | Goblin Raid | invasion | 1.5h | 10h | 마을 근처 고블린 웨이브 5회 |
| wandering_merchant | 방랑 상인 | merchant | 3h | 12h | 특별 아이템 판매 NPC 출현 |

### 로직

1. **이벤트 체크 (매 게임 1시간마다):**
   ```csharp
   if (gameHour - _lastCheckHour >= 1f)
   {
       _lastCheckHour = gameHour;
       if (_activeEvent != null) { CheckEventEnd(); return; }
       // 각 이벤트 정의에 대해 확률 체크
       foreach (var def in ShuffledDefinitions())
       {
           if (gameHour - GetLastOccurrence(def.id) < def.minInterval) continue;
           if (Random.value > def.chance) continue;
           StartEvent(def);
           break;
       }
   }
   ```

2. **이벤트 실행:**
   - `elite_spawn`: MonsterSpawner에 엘리트 스폰 요청 (특정 리전).
   - `bonus_drops`: LootSystem.GlobalDropMultiplier 설정.
   - `invasion`: 웨이브 코루틴 — 30초 간격으로 몬스터 그룹 스폰.
   - `merchant`: 임시 NPC 생성 (특별 상점 인벤토리).

3. **이벤트 종료:** duration 경과 시 자동 종료 + 정리.

### UI
- HUD 상단에 이벤트 배너: `"🌙 Blood Moon — 엘리트 출현 중! (1:30 남음)"`.
- EventBus.Emit(WorldEventStartEvent), WorldEventEndEvent.

### 세이브 연동
- 활성 이벤트 ID + 시작 시간을 SaveData에 저장.
- 로드 시 남은 시간 계산하여 이벤트 복원 (또는 만료 처리).

## 호출 진입점
- 자동. WorldEventSystem.Update()에서 TimeSystem.GameHour 기반 트리거.
- UI: HUD 이벤트 배너 자동 표시.

## 테스트 항목
- [ ] 시간 경과 후 이벤트가 확률적으로 발생하는지
- [ ] 이벤트 지속 시간 후 자동 종료
- [ ] 최소 간격 이내에 같은 이벤트 재발생 안 됨
- [ ] elite_spawn에서 엘리트 몬스터 스탯이 2배인지
- [ ] bonus_drops에서 드롭률이 2배인지
- [ ] HUD 배너에 남은 시간 표시
- [ ] 세이브/로드 후 활성 이벤트 복원
