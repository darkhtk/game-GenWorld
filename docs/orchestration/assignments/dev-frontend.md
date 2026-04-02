# Current Assignment: Dev-Frontend

## Status: ACTIVE

## !! LOOP RULE !!
**절대 멈추지 마라.** 모든 Step 끝나면 → Loop로. 블로커 만나면 → 기록하고 다음 작업.
"할 일이 없다", "대기 중", "assignment 확인 필요" 같은 상태는 금지.
항상 할 일이 있다. 아래 Loop 섹션을 봐라.

## Completed
- [x] Phase 6 — 12 UI panels + SkillVFX (uncommitted)

## Task
순서대로 진행.

### Step 1: 미커밋 작업 커밋
```bash
git add Assets/Scripts/UI/ Assets/Scripts/Effects/
git commit -m "feat: implement Phase 6 — UI panels (12 files) + SkillVFX"
```

### Step 2: UI 코드 품질 점검
기존 UI 코드를 열어서 확인:
- compile 에러/경고 없는지 확인
- 시스템 API 호출이 interface-contracts.md와 일치하는지 검증
- null 참조 가능성 있는 곳에 방어 코드 추가
- 핫키 충돌 없는지 확인 (I/K/J/Esc/E/1-6/R/T)

### Step 3: UI 연동 준비
GameManager가 아직 스텁이므로, 각 UI 패널이 독립적으로 Show/Hide/Refresh 동작하는지 확인.
시스템 의존성이 있는 Refresh 메서드에서 null 체크 보강:
- InventoryUI.Refresh → InventorySystem null 가능
- ShopUI.Refresh → DataManager null 가능
- QuestUI.Refresh → QuestSystem null 가능
- SkillTreeUI.Refresh → SkillSystem null 가능

### Step 4: Visual Polish
- DamageNumber 표시 로직 확인 (CombatManager.ShowDamageNumber 연동 준비)
- SkillVFX 색상/페이드 타이밍 조정
- UI 애니메이션/트랜지션 개선 (있으면)

## Reference
- docs/orchestration/reference/interface-contracts.md (UI 섹션 + 시스템 API)
- docs/orchestration/reference/phaser-unity-mapping.md
- docs/orchestration/roles/dev-frontend.md

## Git (Step별)
```bash
git add Assets/Scripts/UI/ Assets/Scripts/Effects/ Assets/Prefabs/UI/
git commit -m "fix: <설명>"
```
status/dev-frontend.md 갱신.

## Loop (Step 1-4 모두 끝난 후)
모든 Step이 끝나도 **멈추지 마라**. 아래를 반복:
1. `compile-status.md` 확인 — UI/Effects 관련 에러 수정
2. `questions/` 확인 — 나한테 온 질문 답변
3. `status/dev-backend.md` 읽기 — Backend가 새로 구현한 시스템이 있으면 UI 연동 코드 업데이트
4. 자기가 작성한 UI 코드에서 버그/누락/개선점 찾아 수정
5. interface-contracts.md와 UI 코드 재대조 — API 변경 있으면 반영
6. `status/dev-frontend.md` 갱신
7. 1번으로 돌아가기
