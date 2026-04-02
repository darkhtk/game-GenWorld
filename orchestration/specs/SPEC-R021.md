# SPEC-R021: 주/야간 사이클

## 목적
게임 내 시간 흐름에 따라 조명이 변하고, 야간에는 몬스터 스폰이 변경된다.

## 현재 상태
- TimeSystem은 R-019에서 도입 예정 (GameHour 0~24).
- URP 사용 중 — Global Light 2D로 조명 제어 가능.
- MonsterSpawner — 리전 기반 스폰. 시간대별 분기 없음.

## 의존성
- **R-019 (NPC 일과 시스템)** — TimeSystem 공유. R-019 먼저 구현 필요.

## 구현 명세

### 새 파일
- `Assets/Scripts/Effects/DayNightCycle.cs` — 조명 제어 MonoBehaviour

### 수정 파일
- `Assets/Scripts/Entities/MonsterSpawner.cs` — 야간 스폰 변경

### 데이터 구조

```csharp
// DayNightCycle.cs
public class DayNightCycle : MonoBehaviour
{
    [SerializeField] UnityEngine.Rendering.Universal.Light2D globalLight;
    
    // 시간대별 조명 설정
    static readonly Dictionary<string, (Color color, float intensity)> LightPresets = new()
    {
        {"dawn",      (new Color(1.0f, 0.85f, 0.7f), 0.7f)},
        {"morning",   (new Color(1.0f, 1.0f, 0.95f), 1.0f)},
        {"afternoon", (new Color(1.0f, 0.95f, 0.9f), 1.0f)},
        {"evening",   (new Color(0.9f, 0.6f, 0.4f), 0.6f)},
        {"night",     (new Color(0.3f, 0.3f, 0.6f), 0.3f)},
    };
    
    string _currentPeriod;
    
    public void UpdateLighting(string period, float lerpT);
}
```

### 로직

1. **DayNightCycle.Update():**
   - TimeSystem.Period 확인.
   - Period 변경 시 현재 조명 → 다음 조명으로 Lerp (10초간 전환).
   - globalLight.color / intensity 조정.

2. **MonsterSpawner 야간 변경:**
   ```csharp
   // 스폰 시 TimeSystem.Period 확인
   if (period == "night")
   {
       // 몬스터 스폰 밀도 1.5배
       // 야간 전용 몬스터 추가 스폰 (regions.json에 nightMonsters 필드)
       // 몬스터 atk 1.2배 보너스
   }
   ```

3. **regions.json 확장:**
   ```json
   {
     "id": "dark_forest",
     "nightMonsters": [
       { "id": "shadow_wolf", "weight": 30 },
       { "id": "phantom", "weight": 20 }
     ],
     "nightDensityMult": 1.5
   }
   ```

### 수치
| 파라미터 | 값 | 비고 |
|---------|-----|------|
| 조명 전환 시간 | 10초 | Lerp 기간 |
| 야간 스폰 밀도 | 1.5x | |
| 야간 공격력 보너스 | 1.2x | |
| dawn 조명 | 따뜻한 주황, 70% | |
| night 조명 | 차가운 청색, 30% | |

### 세이브 연동
- TimeSystem.GameHour (R-019에서 세이브 포함).
- 조명 상태는 저장 불필요 (GameHour에서 복원).

## 호출 진입점
- 자동. DayNightCycle.Update() + MonsterSpawner 스폰 로직.
- HUD: 시간/시간대 표시 (선택사항).

## ��스트 항목
- [ ] 시간 경과에 따라 조명 색상이 변하는지
- [ ] 조명 전환이 부드러운 Lerp인지 (갑작스럽지 않은지)
- [ ] 야간에 몬스터 스폰 밀도가 1.5배인지
- [ ] nightMonsters가 야간에만 스폰되는지
- [ ] 낮에는 nightMonsters가 스폰되지 않는지
- [ ] GameHour 로드 후 조명이 즉시 올바른 상태인지
