# SPEC-R014: 퀘스트 추적 HUD 위젯

## 목적
현재 진행 중인 퀘스트의 목표를 화면 우측에 상시 표시하여 QuestUI를 열지 않아도 진행 상황을 확인할 수 있게 한다.

## 현재 상태
- `HUD.cs` — 퀘스트 추적 영역 없음.
- `QuestUI.cs` — QuestSystem에서 active/completed 퀘스트 목록 표시.
- `QuestSystem.cs` — 퀘스트 진행률 (kill count 등) 추적 중.
- `GameEvents.cs` — MonsterKillEvent 등 퀘스트 관련 이벤트 존재.

## 구현 명세

### 수정 파일
- `Assets/Scripts/UI/HUD.cs` — 퀘스트 추적 위젯 추가

### UI 와이어프레임
```
                          ┌─────────────────────┐
                          │ 📜 약초 수집          │
                          │   healing_herb: 2/3  │
                          │   mushroom: 5/5 ✅   │
                          │ ───────────────────  │
                          │ 📜 늑대 퇴치          │
                          │   wolf 처치: 3/10    │
                          └─────────────────────┘
                          화면 우상단, 반투명 배경
```

### 데이터 구조

```csharp
// HUD.cs에 추가
[Header("Quest Tracker")]
[SerializeField] GameObject questTrackerRoot;     // 우상단 앵커
[SerializeField] Transform questTrackerContent;   // VerticalLayoutGroup
[SerializeField] GameObject questTrackerEntryPrefab;  // 퀘스트 1건 표시

// questTrackerEntryPrefab 구조:
// - TextMeshProUGUI questName (퀘스트 이름)
// - Transform objectiveList (VerticalLayout)
//   - TextMeshProUGUI objectiveText (목표 텍스트 "wolf: 3/10")
```

### 로직

1. **표시 대상:** QuestSystem.GetActiveQuests() 중 최대 3건.
2. **갱신 시점:**
   - MonsterKillEvent 발생 시 (킬 카운트 변동)
   - 퀘스트 수락/완료 시
   - 인벤토리 변경 시 (아이템 수집 퀘스트)
3. **목표 텍스트 포맷:**
   - `"{targetName}: {current}/{required}"` — 미완료
   - `"{targetName}: {required}/{required} ✅"` — 완료 (녹색)
4. **애니메이션:**
   - 목표 달성 시 해당 줄 잠시 하이라이트 (선택사항).
5. **토글:**
   - `V` 키로 위젯 표시/숨김.

### 수치
| 파라미터 | 값 | 비고 |
|---------|-----|------|
| 최대 표시 퀘스트 | 3건 | 화면 공간 제한 |
| 배경 알파 | 0.6 | 반투명 |
| 위치 | 우상단 | 앵커: top-right |
| 토글 키 | V | |

### 세이브 연동
없음. 위젯 표시 여부는 세션 전용. 퀘스트 데이터 자체는 QuestSystem에서 이미 관리.

## 호출 진입점
- HUD 자동 표시. `V` 키로 토글.
- QuestSystem 이벤트 구독으로 자동 갱신.

## 테스트 항목
- [ ] 퀘스트 수락 시 위젯에 즉시 표시되는지
- [ ] 킬 카운트가 실시간 갱신되는지
- [ ] 퀘스트 완료 시 위젯에서 제거되는지
- [ ] 3건 초과 시 최근 3건만 표시되는지
- [ ] V 키로 토글이 동작하는지
- [ ] 완료 목표에 ✅ + 녹색이 표시되는지
