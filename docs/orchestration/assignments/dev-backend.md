# Current Assignment: Dev-Backend

## Status: ACTIVE (Phase 7)

## !! LOOP RULE !!
**절대 멈추지 마라.** 모든 Phase 끝나면 → Loop로. 블로커 만나면 → 기록하고 다음 작업.

## Completed
- [x] Phase 2 — 11 Systems + 8 test suites
- [x] Phase 3 — 6 Entities
- [x] Phase 4 — CombatManager, SkillExecutor, ActionRunner, AreaEffect
- [x] Phase 5 — OllamaClient, PromptBuilder, AIManager + 3 AI test suites

## Task: Phase 7 — Integration Hardening

### Step 1: QuestSystem EventBus 연동
QuestSystem이 MonsterKillEvent를 수신해서 킬 조건 퀘스트를 자동 진행하도록 연결.
- `QuestSystem`에 EventBus.On<MonsterKillEvent> 핸들러 추가
- 퀘스트 완료 시 EventBus.Emit(QuestCompleteEvent) 발행
- EditMode 테스트 작성

### Step 2: SkillSystem cooldown 연동
CombatManager.ExecuteSkill()에서 스킬 쿨다운 상태를 HUD에 반영할 수 있도록:
- SkillSystem.GetCooldowns() 메서드 추가 (float[] 반환)
- 이미 있으면 무시

### Step 3: 테스트 커버리지 보강
현재 테스트가 없거나 약한 시스템:
- CombatManager (iteration fix는 있지만 전체 로직 테스트 부족)
- ActionRunner (spawn_projectile, spawn_area, teleport)
- EffectSystem (stun/slow/dot 만료 테스트)
- SaveSystem (round-trip 테스트)

### Step 4: 방어 코드 보강
- MonsterSpawner.SpawnForRegion(): null 리전 방어
- CombatManager: 몬스터 리스트 비어있을 때 예외 방지 확인
- AIManager: OllamaClient 연결 실패 시 graceful fallback 확인

## How
1. interface-contracts.md 확인 후 시그니처 유지
2. 수정 후 EditMode 테스트 작성/갱신
3. Step 끝나면 커밋 → status 갱신

## Reference
- docs/orchestration/reference/interface-contracts.md
- docs/orchestration/reference/data-schema.md
- Assets/Scripts/Core/GameManager.cs (Director가 EventBus→UI 와이어링 완료)

## Git
```bash
git add Assets/Scripts/Systems/ Assets/Scripts/AI/ Assets/Scripts/Entities/ Assets/Tests/EditMode/
git commit -m "feat: Phase 7 Step N — <설명>"
```

## Loop (Phase 7 끝난 후)
1. `compile-status.md` 확인 — 에러 수정
2. `questions/` 확인 — 답변
3. 코드 리뷰 — 자기 코드 버그/누락 찾기
4. 테스트 보강
5. `status/dev-backend.md` 갱신
6. 1번으로
