# SPEC-S-041: NPC 호감도 데이터 저장 — 세이브/로드 시 호감도 누락 확인

> **우선순위:** P3
> **방향:** stabilize
> **태그:** 🔧 검증

## 문제

NPC 호감도(relationships) 데이터가 세이브/로드 사이클에서 정확히 보존되는지 확인 필요.
`NPCBrain.Serialize()` → `SaveData.npcBrains` → `NPCBrain.Restore()` 경로에서 데이터 손실 가능성 검증.

## 현재 구조

| 파일 | 메서드 | 역할 |
|------|--------|------|
| `Assets/Scripts/AI/NPCBrain.cs` | `Serialize()` | NPC 상태 → NPCBrainData 변환 |
| `Assets/Scripts/AI/NPCBrain.cs` | `Restore()` | NPCBrainData → NPC 상태 복원 |
| `Assets/Scripts/AI/AIManager.cs` | `SerializeAllBrains()` | 전체 NPC 직렬화 |
| `Assets/Scripts/AI/AIManager.cs` | `RestoreAllBrains()` | 전체 NPC 복원 |
| `Assets/Scripts/Core/SaveController.cs` | `Save()` / `Load()` | 세이브/로드 오케스트레이션 |
| `Assets/Scripts/Data/StatTypes.cs` | `NPCBrainData` | 직렬화 구조체 |

## 데이터 구조

```
SaveData.npcBrains: Dictionary<string, NPCBrainData>
  NPCBrainData:
    relationships: Dictionary<string, int>  // target_id → 호감도 (-100~100)
    memories: List<MemoryEntry>
    mood: MoodType (Happy/Neutral/Angry/Scared)
    wantToTalk: bool
    talkReason: string
    alertType: string
    triggeredEvents: HashSet<string>
```

## 검증 항목

### 1. 라운드트립 정확성
- [ ] `UpdateRelationship("player", +10)` → 저장 → 로드 → 값 보존 확인
- [ ] 음수 호감도 (-50) → 저장 → 로드 → 값 보존 확인
- [ ] 경계값 (100, -100) → clamp 후 저장/로드 정확성

### 2. 다중 NPC
- [ ] 2+ NPC 각각 다른 호감도 → 저장 → 로드 → 개별 값 보존
- [ ] 존재하지 않는 NPC ID → 로드 시 무시/경고 (크래시 없음)

### 3. 빈 데이터
- [ ] relationships가 빈 Dictionary → 저장/로드 정상 (null 아닌 빈 딕셔너리)
- [ ] memories가 빈 List → 저장/로드 정상
- [ ] triggeredEvents가 빈 Set → 저장/로드 정상

### 4. 마이그레이션 호환
- [ ] 기존 세이브(relationships 필드 없음) → 로드 시 기본값 생성 (크래시 없음)

## 연동 경로

```
게임 플레이 중:
  DialogueSystem → NPCBrain.UpdateRelationship() → relationships 갱신

저장:
  SaveController.Save() → AIManager.SerializeAllBrains() → NPCBrain.Serialize()
    → SaveData.npcBrains에 기록 → JSON 직렬화 → 파일

로드:
  SaveController.Load() → JSON 역직렬화 → AIManager.RestoreAllBrains()
    → NPCBrain.Restore() → relationships 복원
```

## 세이브 연동
- 직접 관련: `SaveData.npcBrains`
- 파일: `rpg_save.json`

## UI 연동
- 없음 (데이터 레이어 검증)

## 수정 방향
- **버그 없음 시:** ✅ 확인 완료로 마감
- **누락 발견 시:** Serialize/Restore에 누락 필드 추가 + null 방어
