# SPEC-R004: JSON 파싱 실패 시 복구

## 목적
DataManager에서 JSON 데이터 파일이 손상되었을 때 게임이 크래시하지 않고 기본값으로 폴백한다.

## 현재 상태
- `DataManager.cs:108-126` — `LoadJson<T>`가 이미 try-catch + null return 처리됨 (감독관 패치 적용).
- 그러나 **부분 손상**(일부 필드만 깨진 경우)은 처리 안 됨 — 역직렬화 성공하지만 null 필드 발생.
- 상위 Load 메서드에서 null 체크 후 빈 컬렉션 유지는 하지만, **어떤 데이터가 누락인지 사용자에게 알리지 않음**.

## 구현 명세

### 수정 파일
- `Assets/Scripts/Data/DataManager.cs`

### 변경사항

1. **LoadAll()에 검증 단계 추가:**
   ```csharp
   public void LoadAll()
   {
       LoadItems();
       LoadSkills();
       // ... existing ...
       ValidateData(); // NEW
   }
   ```

2. **ValidateData() 메서드:**
   - Items, Skills, Monsters 등 각 Dictionary가 비어 있으면 경고 로그.
   - 개별 아이템의 필수 필드(id, name) null 체크 → 누락 항목 리스트 로그.
   - 결과: `[DataManager] Validation: 2 items missing 'name', 1 monster missing 'id'` 형태.

3. **null 필드 방어적 기본값:**
   - ItemDef: `name ?? "unknown"`, `grade ?? "common"`, `stackLimit <= 0 ? 1 : stackLimit`
   - MonsterDef: `hp <= 0 ? 10 : hp`, `atk <= 0 ? 1 : atk`
   - 각 Def 클래스의 생성자 또는 역직렬화 후처리에서 수행.

### 데이터 구조 변경
없음. 기존 Def 클래스에 기본값 로직만 추가.

### 세이브 연동
없음. 데이터 파일은 읽기 전용.

## 호출 진입점
- `DataManager.LoadAll()` — 게임 시작 시 자동 실행.
- UI 진입점 없음.

## 테스트 항목
- [ ] 빈 JSON 파일 로드 시 빈 Dictionary + 경고 로그
- [ ] 필수 필드 누락 아이템이 기본값으로 폴백되는지
- [ ] 정상 JSON은 영향 없이 로드되는지
- [ ] ValidateData()가 누락 항목 수를 정확히 보고하는지
