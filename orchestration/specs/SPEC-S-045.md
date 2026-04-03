# SPEC-S-045: QuestSystem 진행률 저장

> **우선순위:** P2
> **태그:** 🔧 안정성
> **관련 파일:** QuestSystem.cs, SaveController.cs, SaveSystem.cs

## 문제

QuestSystem의 `_killProgress` (Dictionary<string, Dictionary<string, int>>)가 Serialize/Restore에 포함되지 않음.
- `Serialize()`: active IDs + completed IDs만 반환
- `Restore()`: _active, _completed만 복원, _killProgress는 빈 dict로 초기화

**결과:** 게임 종료/크래시 후 재시작하면 퀘스트 킬 카운트가 0으로 리셋됨.
예: "늑대 5마리 처치" 퀘스트에서 3마리 처치 후 저장→로드하면 0마리로 복귀.

## 수정 범위

### QuestSystem.cs

1. **Serialize 확장:**
```
현재: (string[] active, string[] completed)
변경: (string[] active, string[] completed, Dictionary<string, Dictionary<string, int>> killProgress)
```

2. **Restore 확장:**
- killProgress 파라미터 추가
- null/빈 값 시 빈 dict로 폴백 (기존 세이브 호환)

### SaveController.cs

3. **Save():** QuestSystem.Serialize() 반환값에서 killProgress 추출 → SaveData에 저장
4. **Load():** SaveData에서 killProgress 읽어 Restore()에 전달

### SaveData (Data 클래스)

5. killProgress 필드 추가 (JSON 직렬화 가능 구조)
   - 기존 세이브 파일에 없을 경우 null → 빈 dict 폴백

## 검증 항목

- [ ] 킬 퀘스트 진행 중 저장→로드 후 카운트 유지
- [ ] 기존 세이브(killProgress 없음)에서 로드 시 크래시 없음
- [ ] 완료된 퀘스트는 killProgress에서 자동 제거 (메모리 누수 방지)

## 호출 진입점

- 자동 저장: GameManager._lastAutoSaveTime 기반 (리전 전환 시)
- 수동 저장: PauseMenu → SaveButton → SaveController.Save()
- 로드: 게임 시작 시 SaveSystem.HasSave() → SaveController.Load()

## 세이브 호환

SaveSystem 버전 마이그레이션 불필요 — killProgress 필드가 null이면 빈 dict로 폴백하므로 기존 세이브와 하위 호환됨.
