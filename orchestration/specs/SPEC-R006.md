# SPEC-R006: 리전 전환 시 자동 저장

## 목적
플레이어가 새 리전에 진입할 때 자동으로 게임을 저장하여 진행 손실을 방지한다.

## 현재 상태
- `RegionTracker.cs:28-30` — 리전 변경 시 `RegionVisitEvent`를 EventBus로 발행.
- `SaveSystem.cs` — `Save(SaveData)` 메서드 존재.
- 현재는 수동 저장(PauseMenu) 또는 게임 종료 시만 저장됨.

## 구현 명세

### 수정 파일
- `Assets/Scripts/Core/GameManager.cs` — 이벤트 구독 추가

### 변경사항

1. **GameManager에 이벤트 구독:**
   ```csharp
   // Init() 또는 Start()에서:
   EventBus.On<RegionVisitEvent>(OnRegionVisit);
   ```

2. **OnRegionVisit 핸들러:**
   ```csharp
   void OnRegionVisit(RegionVisitEvent e)
   {
       // Throttle: 마지막 자동 저장 후 30초 이내면 스킵
       float now = Time.time;
       if (now - _lastAutoSaveTime < 30f) return;
       _lastAutoSaveTime = now;
       
       SaveData data = BuildSaveData();
       SaveSystem.Save(data);
       Debug.Log($"[AutoSave] Region changed to {e.regionName}");
   }
   ```

3. **GameManager에 필드 추가:**
   ```csharp
   float _lastAutoSaveTime;
   ```

### 수치
- **쓰로틀:** 30초 (빈번한 리전 경계 왕복 시 과다 저장 방지)
- **저장 대상:** 기존 `Save()` 로직 그대로 (전체 상태)

### UI 피드백
- HUD에 "Auto Saved" 텍스트 1초간 표시 (선택사항 — 기존 History Log에 추가 가능).

### 세이브 연동
- 기존 SaveSystem.Save() 재사용. 추가 포맷 변경 없음.

## 호출 진입점
- 자동. 플레이어가 리전 경계를 넘으면 EventBus를 통해 트리거.
- UI 진입점 없음.

## 테스트 항목
- [ ] 리전 전환 시 세이브 파일이 갱신되는지
- [ ] 30초 이내 재전환 시 중복 저장 안 되는지
- [ ] 저장 중 프레임 드롭이 없는지 (동기 I/O 부하 확인)
- [ ] 저장된 데이터가 정상 로드되는지
