# SPEC-R020: NPC 호감도 이벤트

## 목적
NPC와의 우호도가 특정 임계점에 도달하면 특별 대화, 보상, 퀘스트가 트리거된다.

## 현재 상태
- `NPCBrain.cs` — `_relationships` Dictionary (targetId → int, -100~100). `UpdateRelationship()` 존재.
- `NpcDef.cs:19` — `NpcTrigger` 구조 존재: `{ target, threshold, relationship, wantToTalk, memory, talkReason, eventType }`.
- 트리거 시스템이 데이터에는 정의되어 있으나 **런타임 평가 로직이 미구현**.

## 구현 명세

### 수정 파일
- `Assets/Scripts/AI/AIManager.cs` — 트리거 평가 로직 추가
- `Assets/Scripts/AI/NPCBrain.cs` — 트리거 달성 추적

### 데이터 구조

```csharp
// NPCBrain.cs에 추가
HashSet<string> _triggeredEvents = new(); // 이미 발동된 트리거 ID

public bool HasTriggered(string triggerId) => _triggeredEvents.Contains(triggerId);
public void MarkTriggered(string triggerId) => _triggeredEvents.Add(triggerId);
```

### 로직

1. **트리거 평가 (AIManager.UpdateNPCBehavior):**
   ```csharp
   foreach (var trigger in npcDef.triggers)
   {
       string triggerId = $"{npc.NpcId}_{trigger.eventType}_{trigger.threshold}";
       if (brain.HasTriggered(triggerId)) continue;
       
       int rel = brain.GetRelationship(trigger.target ?? "player");
       if (rel < trigger.threshold) continue;
       
       // 트리거 발동!
       brain.MarkTriggered(triggerId);
       ExecuteTrigger(brain, trigger);
   }
   ```

2. **트리거 실행:**
   - `wantToTalk = true` → NPC 머리 위 말풍선 아이콘 표시.
   - `memory` → NPC 기억에 추가 (`brain.AddMemory`).
   - `talkReason` → 다음 대화 시 특별 인사말.
   - `eventType` 별 특수 처리:
     - `"gift"` → NPC가 아이템 선물 (giftItemId, giftCount 참조).
     - `"quest"` → 특별 퀘스트 제안.
     - `"unlock_shop"` → 상점에 숨겨진 아이템 추가.
     - `"story"` → 특별 대화 시퀀스.

3. **호감도 변경 경로:**
   - 퀘스트 완료 → +10~20
   - 대화 → +1~3
   - 아이템 선물 → +5~15 (아이템 가치 비례)
   - 전투에서 NPC 근처 몬스터 처치 → +2

### 세이브 연동
- `NPCBrainData`에 `_triggeredEvents` 추가.
- Serialize/Restore에 포함.

## 호출 진입점
- AIManager.UpdateNPCBehavior() — 자동 평가.
- NPC 머리 위 아이콘: 플레이어가 클릭하여 대화 시작.

## 테스��� 항목
- [ ] 호감도 임계점 도달 시 트리거 발동
- [ ] 이미 발동된 트리거가 재발동되지 않는지
- [ ] gift 이벤트에서 아이템이 인벤토리에 추가되는지
- [ ] wantToTalk 시 NPC 말풍선 아이콘 표시
- [ ] _triggeredEvents가 세이브/로드 후 유지되는지
- [ ] 호감도 -100~100 범위가 유지되는지
