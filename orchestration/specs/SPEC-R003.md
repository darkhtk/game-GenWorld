# SPEC-R003: 세이브 파일 버전 마이그레이션

## 목적
세이브 파일에 버전 필드를 추가하고, 구버전 세이브를 자동으로 최신 스키마로 변환한다.

## 현재 상태
- `SaveSystem.cs`가 JSON으로 직렬화/역직렬화하지만 버전 관리가 없음.
- 새 필드 추가 시 기존 세이브 로드 실패 가능.

## 구현 명세

### 수치
- **현재 버전:** 1 (기존 세이브는 버전 0으로 취급)
- **최대 마이그레이션 단계:** 제한 없음 (0→1→2→... 순차 적용)

### 연동 경로
- **수정 파일:** `Assets/Scripts/Systems/SaveSystem.cs`
- **참조 파일:** `Assets/Scripts/Entities/PlayerStats.cs` (세이브 데이터 구조)

### 데이터 구조
```csharp
// SaveData wrapper
[Serializable]
public class SaveEnvelope
{
    public int version;
    public JObject data; // raw JSON for migration
}

// Migration registry
public static class SaveMigrations
{
    // version N → N+1 변환 함수 등록
    private static readonly Dictionary<int, Func<JObject, JObject>> _migrations = new()
    {
        { 0, MigrateV0ToV1 },
        // { 1, MigrateV1ToV2 }, // future
    };

    public static JObject Apply(int fromVersion, int toVersion, JObject data);
}
```

### 로직
1. **저장 시:** `SaveEnvelope { version = CURRENT_VERSION, data = ... }` 으로 래핑.
2. **로드 시:**
   a. JSON 파싱 → `version` 필드 확인.
   b. `version` 없으면 → 0으로 취급.
   c. 현재 버전보다 낮으면 → 순차 마이그레이션 적용.
   d. 마이그레이션 완료 후 역직렬화.
   e. 마이그레이션 실패 시 → 백업 생성 + 에러 로그 + 새 게임 시작 안내.
3. **V0→V1 마이그레이션 예시:** (빈 변환 — 버전 체계 도입만)

### 세이브 연동
- 이 기능 자체가 세이브 시스템 개선.
- 기존 세이브 파일 호환성 100% 유지.

## 호출 진입점
- `SaveSystem.Load()` 에서 자동 실행.
- UI: 마이그레이션 발생 시 "세이브 업그레이드 완료" 토스트 (optional).

## 테스트 항목
- [ ] 버전 필드 없는 세이브가 v0으로 인식되는지
- [ ] v0 → v1 마이그레이션이 정상 적용되는지
- [ ] 최신 버전 세이브는 마이그레이션 스킵하는지
- [ ] 마이그레이션 실패 시 백업이 생성되는지
- [ ] 다단계 마이그레이션(v0→v1→v2)이 순차 적용되는지
