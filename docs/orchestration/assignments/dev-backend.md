# Current Assignment: Dev-Backend

## Status: ACTIVE

## !! LOOP RULE !!
**절대 멈추지 마라.** 모든 Phase 끝나면 → Loop로. 블로커 만나면 → 기록하고 다음 작업.
"할 일이 없다", "대기 중", "assignment 확인 필요" 같은 상태는 금지.
항상 할 일이 있다. 아래 Loop 섹션을 봐라.

## Completed
- [x] Phase 2 — 11 Systems + 8 test suites
- [x] Phase 3 — Entities (PlayerController, PlayerStats, MonsterController, MonsterSpawner, VillageNPC, Projectile)

## Task
순서대로 진행. 각 Phase 끝나면 커밋 → status 갱신 → 다음 Phase.

### Phase 4: Combat Runtime + Remaining Handlers
| File | Status | Notes |
|------|--------|-------|
| CombatManager.cs | STUB | Auto-attack loop, monster attacks, ExecuteSkill, damage numbers |
| SkillExecutor.cs | PARTIAL | 5 remaining: projectile_multi, chain, aoe_debuff, place_trap, place_blizzard |
| ActionRunner.cs | PARTIAL | 3 remaining: spawn_projectile, spawn_area, teleport |

### Phase 5: AI
| File | Status | Notes |
|------|--------|-------|
| OllamaClient.cs | STUB | POST to localhost:11434/api/generate |
| PromptBuilder.cs | STUB | SYSTEM_RULES + BuildDialoguePrompt |
| AIManager.cs | MOSTLY STUB | RegisterNpc, GenerateDialogue, UpdateBehavior |
| RegionTracker.cs | STUB | GetRegionAt, UpdatePlayerRegion |

### Bonus (함께 커밋)
- `Assets/Tests/EditMode/EditModeTests.asmdef` — GenWorld 참조 추가 (디스크 적용됨)
- `Assets/Scripts/GenWorld.asmdef` — 새로 생성됨 (디스크 존재)

## How
1. 스텁 파일을 열고 `interface-contracts.md`와 대조
2. public 시그니처 유지, 실제 로직으로 채움
3. 가능한 시스템에 EditMode 테스트 작성
4. Phase 끝나면 바로 커밋하고 다음으로

## Reference
- docs/orchestration/reference/interface-contracts.md
- docs/orchestration/reference/data-schema.md
- docs/orchestration/reference/architecture.md
- Assets/StreamingAssets/Data/ai-rules/ (AI 관련)
- Assets/StreamingAssets/Data/npc-profiles/ (NPC 성격)

## Git (Phase별)
```bash
git add Assets/Scripts/Systems/ Assets/Scripts/AI/ Assets/Scripts/Entities/ Assets/Tests/EditMode/ Assets/Scripts/GenWorld.asmdef
git commit -m "feat: implement Phase N — <설명>"
```
status/dev-backend.md 갱신 후 다음 Phase로.

## Loop (Phase 4-5 모두 끝난 후)
모든 Phase가 끝나도 **멈추지 마라**. 아래를 반복:
1. `compile-status.md` 확인 — 내 코드가 원인인 에러 수정
2. `questions/` 확인 — 나한테 온 질문 답변
3. 기존 코드 리뷰 — 자기가 작성한 Systems/Entities/AI 코드에서 버그/누락 찾기
4. 테스트 보강 — 커버리지 낮은 시스템에 테스트 추가
5. `status/dev-backend.md` 갱신
6. 1번으로 돌아가기
