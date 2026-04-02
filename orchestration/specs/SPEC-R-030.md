# SPEC-R-030: MonsterController 애니메이션 검증 및 연동

**관련 태스크:** R-030

---

## 개요
MonsterDef에 AnimationDef 참조를 추가하고, MonsterController의 상태 전이(idle·walk·attack·hit·die)를 검증한다.

## 상세 설명
MonsterDef SO에 `AnimationDef animationDef` 필드를 추가한다. MonsterController에서 AI 상태(AIManager 연동)에 따라 올바른 애니메이션이 재생되는지 검증한다. MonsterSpawner로 생성 시 Animator Controller가 정상 할당되는지, 피격(CombatManager) 시 hit 애니메이션이 재생되는지, 사망 시 DeathMarker와 die 애니메이션 타이밍이 맞는지 확인한다.

## 데이터 구조
```csharp
// MonsterDef.cs에 추가
public AnimationDef animationDef;  // 해당 몬스터의 애니메이션 정의
```

## 연동 경로
| From | To | 방식 |
|------|-----|------|
| AIManager | MonsterController | AI 상태 → 애니메이션 상태 매핑 |
| CombatManager | MonsterController | 피격 시 hit 트리거 |
| MonsterSpawner | MonsterController | 생성 시 Animator 초기화 |
| DeathMarker | MonsterController | 사망 애니메이션 완료 콜백 |
| MonsterDef | AnimationDef (R-027) | SO 참조 |

## UI 와이어프레임
N/A

## 호출 진입점
- **어디서:** MonsterController.cs
- **어떻게:** 상태 변경 시 `PlayAnimation(string stateName)` 호출, AnimationDef 기반 검증

## 수용 기준
- [ ] MonsterDef에 animationDef 필드 추가, Inspector에서 할당 가능
- [ ] idle ↔ walk 전이 정상 (AIManager patrol 상태)
- [ ] attack 애니메이션 재생 및 데미지 타이밍 일치 (CombatManager 연동)
- [ ] hit 애니메이션 재생 시 넉백과 동기화
- [ ] die 애니메이션 완료 후 DeathMarker 생성·LootDropVFX 재생
- [ ] MonsterSpawner 생성 시 Animator 누락 경고 로그 출력
