# REVIEW-R031-v1: NPC 애니메이션 검증 및 연동

> **리뷰 일시:** 2026-04-02
> **태스크:** R-031 NPC 애니메이션 검증
> **스펙:** SPEC-R-031
> **판정:** ✅ APPROVE

---

## 수용 기준별 검증

| # | 수용 기준 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | NpcDef animationDef 필드 | NpcDef.cs:14 (`[JsonIgnore] AnimationDef animationDef`) | ✅ |
| 2 | idle 루프 정상 | ResumeMoving line 99 `SetAnimationState("idle")` | ✅ |
| 3 | react 애니메이션 | ValidateAnimations에서 검증 (line 36 "react") — 호출부는 향후 proximity trigger | ✅ |
| 4 | DialogueUI 진입 → talk | StopMoving line 93 `SetAnimationState("talk")` | ✅ |
| 5 | DialogueUI 종료 → idle | ResumeMoving line 99 `SetAnimationState("idle")` | ✅ |
| 6 | animationDef 미할당 경고 | ValidateAnimations line 30-34 `LogWarning` | ✅ |

### 코드 품질

- SetAnimationState: animationDef/entry/clip 3중 null 체크 — MonsterController.PlayAnimation과 동일 패턴 ✅
- ValidateAnimations: idle/talk/react 3개 필수 상태 검증 ✅
- Init에서 자동 호출 — 스폰 시 즉시 검증 ✅
- DoPatrol sqrMagnitude 최적화 (line 67 `< 16f` = Distance < 4) ✅

---

## 최종 판정

**✅ APPROVE**

수용 기준 6개 전부 충족. AnimationDef 연결, ValidateAnimations 자동 검증, talk/idle 전환, 미할당 경고 모두 정확.
