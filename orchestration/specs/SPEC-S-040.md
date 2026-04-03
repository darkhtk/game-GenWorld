# SPEC-S-040: CombatManager 타겟팅 범위 — 화면 밖 몬스터 자동 타겟팅 방지

> **우선순위:** P3
> **방향:** stabilize
> **태그:** 🔧 방어 코드

## 문제

`CombatSystem.FindClosest<T>()`는 range 내 최근접 몬스터를 선택하지만, 카메라 뷰포트 밖에 있는 몬스터도 타겟팅 대상에 포함된다.
플레이어가 보이지 않는 몬스터를 공격하면 직관에 어긋나고, 특히 스킬 사용 시 혼란을 유발한다.

## 현재 구조

| 파일 | 메서드 | 역할 |
|------|--------|------|
| `Assets/Scripts/Systems/CombatSystem.cs` | `FindClosest<T>()` (line 19-54) | 거리 기반 최근접 타겟 선택 |
| `Assets/Scripts/Systems/CombatManager.cs` | `PerformAutoAttack()` (line 39-92) | 호 범위 내 몬스터 공격 |
| `Assets/Scripts/Systems/SkillExecutor.cs` | `HandleSingleTarget()` | `FindClosest()` 호출하여 단일 타겟 선택 |

## 수정 방향

### 옵션 A: FindClosest에 뷰포트 필터 추가 (권장)
- `Camera.main.WorldToViewportPoint()` 로 타겟 위치를 뷰포트 좌표로 변환
- x, y 가 [0, 1] 범위 밖이면 스킵
- 약간의 여유(margin 0.05) 허용 — 화면 가장자리 몬스터도 타겟팅 가능

### 옵션 B: 별도 필터 메서드
- `FindClosestVisible<T>()` 신규 메서드 생성
- 기존 `FindClosest`는 내부용(AoE 등)으로 유지

## 수치

| 항목 | 값 |
|------|-----|
| 뷰포트 여유(margin) | 0.05 (화면 밖 5% 허용) |
| 카메라 참조 | `Camera.main` (캐싱 권장) |

## 연동 경로

```
PlayerController.Update()
  → CombatManager.PerformAutoAttack()
    → CombatSystem.FindClosest() ← 여기에 뷰포트 필터
  → SkillExecutor.HandleSingleTarget()
    → CombatSystem.FindClosest() ← 동일 적용
```

## 세이브 연동
- 없음 (런타임 전용)

## UI 연동
- 없음 (내부 로직)

## 검증 기준
- [ ] 화면 밖 몬스터에 auto-attack 발동 안 됨
- [ ] 화면 밖 몬스터에 단일 타겟 스킬 발동 안 됨
- [ ] AoE 스킬은 기존대로 범위 내 모든 몬스터 타격 (변경 없음)
- [ ] 화면 가장자리 몬스터는 정상 타겟팅
- [ ] 빌드 에러 0
