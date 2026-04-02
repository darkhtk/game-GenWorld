# SPEC-R008: 조건부 대화 분기

## 목적
NPC 대화가 플레이어의 퀘스트 진행 상태, 인벤토리 보유 아이템, 호감도에 따라 달라진다.

## 현재 상태
- `DialogueUI.cs` — NPC 대화 표시, 옵션 선택, 퀘스트 제안 UI 존재.
- `VillageNPC.cs` — NPC 정의 + 기본 패트롤. `Brain` (NPCBrain) 연결.
- `AIManager.cs` — Ollama LLM 기반 대화 생성. 이미 mood/relationship을 프롬프트에 반영.
- **문제:** 프로그래밍적 조건 분기(특정 아이템 보유 → 특수 대화) 없음. LLM 의존.

## 구현 명세

### 수정 파일
- `Assets/Scripts/Entities/VillageNPC.cs` — 조건 평가 로직 추가
- `Assets/Scripts/UI/DialogueUI.cs` — 조건부 옵션 표시
- `Assets/Scripts/Data/NpcDef.cs` — 조건 대화 데이터 구조 추가

### 데이터 구조

1. **NpcDef에 조건 대화 필드 추가:**
   ```csharp
   // NpcDef.cs에 추가
   public ConditionalDialogue[] conditionalDialogues;
   ```

2. **ConditionalDialogue 클래스 (새 파일 또는 NpcDef에 포함):**
   ```csharp
   [Serializable]
   public class ConditionalDialogue
   {
       public string id;
       public string condition;      // "quest_active:q001", "has_item:herb_3", "relationship>=50"
       public string greeting;       // 조건 충족 시 인사말
       public string[] options;      // 조건 충족 시 대화 옵션
       public int priority;          // 높을수록 우선 (여러 조건 충족 시)
   }
   ```

3. **JSON 데이터 예시 (npcs.json):**
   ```json
   {
     "conditionalDialogues": [
       {
         "id": "herb_quest_ready",
         "condition": "has_item:healing_herb:3",
         "greeting": "Oh! You gathered the herbs I needed!",
         "options": ["Turn in herbs", "Not yet"],
         "priority": 10
       }
     ]
   }
   ```

### 로직

1. **대화 시작 시 (VillageNPC 또는 GameManager):**
   - `conditionalDialogues` 순회 → 조건 평가 → priority 최고 항목 선택.
   - 조건 충족 없으면 기존 LLM 대화 로직 사용 (폴백).

2. **조건 파서:**
   ```csharp
   public static class DialogueConditionParser
   {
       public static bool Evaluate(string condition, QuestSystem quests, 
                                    InventorySystem inventory, NPCBrain brain)
       {
           // "quest_active:q001" → quests.IsActive("q001")
           // "quest_done:q001" → quests.IsCompleted("q001")
           // "has_item:herb:3" → inventory.CountItem("herb") >= 3
           // "relationship>=50" → brain.Relationship >= 50
       }
   }
   ```

3. **DialogueUI에 조건부 옵션 표시:**
   - 기존 옵션 생성 로직에 조건 대화의 `options[]` 삽입.
   - 선택 시 `OnAction` 콜백으로 처리.

### 세이브 연동
없음. 조건은 런타임에 평가 (퀘스트/인벤토리 상태는 이미 세이브됨).

## 호출 진입점
- 플레이어가 NPC 상호작용 범위 진입 → 대화 시작 시 자동 평가.
- UI: DialogueUI 패널 열림 시 조건부 인사말/옵션 표시.

## 테스트 항목
- [ ] "has_item" 조건이 인벤토리 수량을 정확히 체크하는지
- [ ] "quest_active" 조건이 진행 중 퀘스트를 인식하는지
- [ ] "relationship>=N" 조건이 호감도 임계값을 체크하는지
- [ ] 조건 미충족 시 기존 LLM 대화로 폴백하는지
- [ ] 여러 조건 충족 시 priority 최고 항목이 선택되는지
- [ ] JSON에 conditionalDialogues 없는 NPC도 정상 동작하는지
