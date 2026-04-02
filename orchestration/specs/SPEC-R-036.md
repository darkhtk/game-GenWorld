# SPEC-R-036: Steam 클라우드 저장 연동

**관련 태스크:** R-036

---

## 개요
기존 `DataManager`의 로컬 저장을 Steam Remote Storage API와 연동하여 클라우드 세이브를 지원한다.

## 상세 설명
`DataManager`의 저장/불러오기 로직에 Steam Remote Storage 래퍼를 추가한다. 저장 시 로컬 파일 기록과 동시에 Steam 클라우드에 업로드하고, 불러오기 시 클라우드와 로컬 타임스탬프를 비교하여 최신 데이터를 사용한다. Steam 미초기화 상태에서는 로컬 저장만 수행하는 폴백 처리를 한다. Steamworks 파트너 사이트에서 클라우드 저장 할당량(바이트, 파일 수)을 설정해야 한다.

## 데이터 구조
```csharp
public static class SteamCloudStorage
{
    public static bool SaveToCloud(string fileName, byte[] data);
    public static byte[] LoadFromCloud(string fileName);
    public static bool CloudFileExists(string fileName);
    public static bool DeleteCloudFile(string fileName);
    public static DateTime GetCloudFileTimestamp(string fileName);
}
```

## 연동 경로
| From | To | 방식 |
|------|----|------|
| DataManager | SteamCloudStorage | 저장/불러오기 시 클라우드 동기화 호출 |
| SteamCloudStorage | SteamManager | `Initialized` 상태 확인 후 API 호출 |
| DataManager | EventBus | `SaveCompleted`, `LoadCompleted` 이벤트 |

## UI 와이어프레임
```
┌─────────────────────────────────┐
│   ⚠ 세이브 충돌 감지            │
│                                 │
│  로컬: 2026-04-02 14:30         │
│  클라우드: 2026-04-01 22:15     │
│                                 │
│  [로컬 사용]  [클라우드 사용]    │
└─────────────────────────────────┘
```

## 호출 진입점
- **어디서:** `DataManager.Save()`, `DataManager.Load()`
- **어떻게:** 기존 저장/불러오기 흐름 끝에 `SteamCloudStorage` 호출 추가

## 수용 기준
- [ ] 게임 저장 시 로컬 파일과 Steam 클라우드에 동시 기록됨
- [ ] 게임 불러오기 시 클라우드/로컬 타임스탬프 비교 후 최신 데이터 로드
- [ ] 타임스탬프 충돌 시 사용자 선택 UI 표시
- [ ] Steam 미초기화 상태에서 로컬 저장만 정상 동작 (폴백)
- [ ] 저장 파일 크기가 Steam 할당량 초과 시 경고 로그 출력
