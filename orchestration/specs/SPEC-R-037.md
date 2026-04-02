# SPEC-R-037: Steam 업적(Achievement) 시스템

**관련 태스크:** R-037

---

## 개요
게임 내 이벤트를 감지하여 Steam 업적을 해제하는 `SteamAchievementManager` 구현.

## 상세 설명
`EventBus`에 구독하여 특정 조건 달성 시 Steam 업적을 해제한다. 업적 정의는 ScriptableObject로 관리하며, Steamworks 파트너 사이트의 업적 ID와 매핑한다. 통계(stat) 기반 업적(예: 몬스터 100마리 처치)도 지원한다. 업적 해제 시 Steam 오버레이 알림이 자동 표시되므로 별도 UI는 불필요하나, 오프라인 해제분은 다음 Steam 연결 시 동기화한다.

## 데이터 구조
```csharp
[CreateAssetMenu(menuName = "Game/Achievement Def")]
public class AchievementDef : ScriptableObject
{
    public string steamApiName;      // Steam 파트너 사이트 API Name
    public string displayName;       // 표시 이름
    public string description;       // 설명
    public GameEventType triggerEvent; // 감지할 EventBus 이벤트
    public string statName;          // 통계 기반일 경우 stat 이름 (optional)
    public int statThreshold;        // 통계 목표치 (optional)
}

public class SteamAchievementManager : MonoBehaviour
{
    [SerializeField] private AchievementDef[] achievements;
    
    public void UnlockAchievement(string apiName);
    public void IncrementStat(string statName, int amount);
    public bool IsUnlocked(string apiName);
    public void ResetAll(); // 개발용
}
```

## 연동 경로
| From | To | 방식 |
|------|----|------|
| EventBus | SteamAchievementManager | 이벤트 구독으로 업적 조건 감지 |
| CombatSystem | EventBus | 몬스터 처치, 보스 클리어 등 이벤트 발행 |
| CraftingSystem | EventBus | 아이템 제작 이벤트 발행 |
| LootSystem | EventBus | 레어 아이템 획득 이벤트 발행 |
| SteamAchievementManager | SteamManager | Steam API 호출 |

## UI 와이어프레임
N/A (Steam 오버레이가 자체 업적 알림 표시)

## 호출 진입점
- **어디서:** `SteamAchievementManager`가 `EventBus`에 자동 구독
- **어떻게:** 이벤트 수신 → 조건 판정 → `SteamUserStats.SetAchievement()` → `StoreStats()`

## 수용 기준
- [ ] `AchievementDef` ScriptableObject로 업적 정의 가능
- [ ] 일회성 업적(이벤트 트리거)과 누적 업적(stat 기반) 모두 지원
- [ ] 업적 해제 시 `SteamUserStats.SetAchievement()` + `StoreStats()` 정상 호출
- [ ] 이미 해제된 업적에 대해 중복 호출하지 않음
- [ ] Steam 미초기화 상태에서 에러 없이 무시(폴백)
- [ ] 에디터에서 `ResetAll()` 호출로 업적 초기화 가능 (테스트용)
