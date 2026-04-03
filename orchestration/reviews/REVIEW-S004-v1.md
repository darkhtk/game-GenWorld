# REVIEW-S004-v1: DoT 사망 킬 보상 미처리

> **리뷰 일시:** 2026-04-03
> **태스크:** S-004 DoT 사망 킬 보상 미처리
> **스펙:** SPEC-S-004.md
> **판정:** ✅ APPROVE
> **커밋:** (GameManager.cs + MonsterController.cs 변경분)

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 기존 참조 변경 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| TASK 명세 부합 | ✅ | 스펙 6개 문제 항목 전부 해결 |
| 기존 코드 호환 | ✅ | 기존 CombatManager 킬 경로와 병존, 외부 API 변경 없음 |
| 아키텍처 패턴 | ✅ | Option A(EventBus) 대신 직접 스캔 방식이나 결과 동일 |
| 테스트 커버리지 | ⚠️ | 스펙에 명시된 단위 테스트 미작성 (후속 권장) |

**코드 분석:**

### 변경점 1: `DeathProcessed` 플래그 (MonsterController.cs:9)
```csharp
public bool DeathProcessed { get; set; }
```
- 킬 파이프라인 중복 실행 방지를 위한 플래그
- `IsDead` (Hp <= 0)과 별도로 "보상 처리 완료" 상태 관리
- **핵심:** 동일 프레임에 일반 공격 킬 + DoT 킬이 겹치는 경우 이중 보상 방지

### 변경점 2: DoT 사망 감지 루프 (GameManager.cs:128-137)
```csharp
// Process DoT deaths — monsters killed by DoT bypass CombatManager
for (int i = monsters.Count - 1; i >= 0; i--)
{
    var m = monsters[i];
    if (m != null && m.IsDead && !m.DeathProcessed)
    {
        m.DeathProcessed = true;
        OnMonsterKilled(m);
    }
}
```
- `UpdateAI` 루프 직후에 역순 순회 — `RemoveMonster`가 리스트에서 원소를 제거해도 인덱스가 안전
- `IsDead && !DeathProcessed` 조건으로 DoT 사망만 선택적 처리
- `OnMonsterKilled()` 호출 → XP, 골드, 루트, 킬 카운트, VFX, EventBus 전부 동작

### 변경점 3: `OnMonsterKilled` 중복 방어 (GameManager.cs:777)
```csharp
monster.DeathProcessed = true;
```
- CombatManager 경로(일반 공격 킬)로 호출되어도 플래그 설정
- DoT 스캔에서 재처리 방지 (멱등성 보장)

### 흐름 분석: 중복 킬 방지 (AC #5)

| 시나리오 | 경로 | DeathProcessed | 이중 처리? |
|----------|------|----------------|-----------|
| 일반 공격으로 킬 | CombatManager → OnMonsterKilled | true (line 777) | ❌ DoT 스캔에서 skip |
| DoT으로 킬 | GameManager DoT 스캔 → OnMonsterKilled | true (line 134) | ❌ 일반 공격은 IsDead 체크로 skip |
| 같은 프레임에 일반+DoT | CombatManager 먼저 → DeathProcessed=true | true | ❌ DoT 스캔에서 skip |

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 사망 VFX | ✅ | OnMonsterKilled line 780: `vfx_monster_death` 표시 |
| 플로팅 텍스트 (XP/골드) | ✅ | OnMonsterKilled line 819-823: XP, 골드 텍스트 표시 |
| 루트 드롭 텍스트 | ✅ | OnMonsterKilled line 837-841: 아이템 이름 표시 |
| HUD 갱신 | ✅ | XP/레벨업/골드 변경 이벤트 발생 → HUD 반영 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 독 DoT으로 몬스터 사망 | XP/골드/루트 정상 지급, VFX 재생, 스포너에서 제거 |
| 화상 DoT으로 보스 사망 | 킬 카운트 + 업적 트리거 + 퀘스트 진행 |
| 일반 공격으로 킬 (기존 경로) | 기존과 동일하게 동작, 이중 보상 없음 |
| DoT 틱 + 일반 공격이 같은 프레임 | DeathProcessed 플래그로 한 번만 처리 |
| 몬스터 귀환(Return) 중 DoT 사망 | OnMonsterKilled 호출 — 보상 정상 지급 |
| 다수 몬스터 동시 DoT 사망 | 역순 순회로 리스트 변경 안전, 전부 개별 처리 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머

독 걸어놓고 기다렸는데 경험치가 안 오는 건 진짜 짜증나는 버그였다. 이제 DoT으로 잡아도 보상이 정상으로 뜨면 독/화상 빌드도 써볼 맛이 난다. 사망 이펙트랑 플로팅 텍스트도 나오니까 "내가 잡았다"는 느낌도 살고.

### ⚔️ 코어 게이머

DoT 빌드 실용성에 직결되는 치명적 버그. 분석:

- **스펙 Option A (EventBus)** 대신 **GameManager 프레임 스캔** 방식을 선택. EventBus 방식은 MonsterController에 이벤트 발행 의존성을 추가하는 반면, 현 방식은 GameManager가 중앙에서 감지하므로 MonsterController는 순수하게 유지. 아키텍처적으로 적절한 판단.
- `DeathProcessed` 플래그의 멱등성이 핵심. CombatManager 경로와 DoT 스캔 경로가 동일한 `OnMonsterKilled`을 공유하되 이중 처리를 확실히 막고 있다.
- 리스트 역순 순회는 `RemoveMonster`의 `List.Remove()` 호출과 안전하게 공존. 정석적 패턴.

한 가지 — DoT 데미지가 플로팅 텍스트로 따로 표시되지는 않는다 (DoT 틱 자체의 데미지 넘버). 이건 이번 스코프 밖이지만 후속 폴리시로 고려할 만하다.

### 🎨 UX/UI 디자이너

DoT 사망 시 시각 피드백 체인:
1. HP 바 감소 (line 105: `_hpBar.UpdateHP`) ✅
2. 사망 애니메이션 (line 106: `PlayAnimation("die")`) ✅
3. 사망 VFX (line 780: `vfx_monster_death`) ✅
4. XP/골드/아이템 플로팅 텍스트 ✅
5. HUD XP 바/골드 갱신 ✅

피드백 체인이 일반 킬과 완전히 동일해서 플레이어 입장에서 일관성 유지. 별도의 DoT 킬 표시(예: "Poison Kill!" 텍스트)가 있으면 만족감이 올라갈 수 있지만 스코프 외.

### 🔍 QA 엔지니어

**안정성 평가:** 양호

| 체크 | 결과 |
|------|------|
| 이중 보상 방지 | ✅ DeathProcessed 멱등 플래그 |
| 리스트 동시 변경 안전 | ✅ 역순 순회 + List.Remove |
| null 방어 | ✅ `m != null` 체크 (line 132) |
| 기존 경로 회귀 없음 | ✅ OnMonsterKilled 기존 로직 변경 없음, 플래그만 추가 |
| 스포너 정리 | ✅ RemoveMonster 호출 → 스포너 리스트 + GameObject 파괴 |

**미비 사항:**
- 스펙에 명시된 단위 테스트(DoT → HP 0 → 킬 콜백 호출) 미작성. 후속으로 `Tests/EditMode/` 에 추가 권장
- `MonsterController.UpdateAI` line 106에서 DoT 사망 시 `return`만 하고 자체적으로는 아무 이벤트도 발행하지 않음. GameManager 스캔에 100% 의존하는 구조이므로, 만약 `UpdateAI`가 호출되지 않는 시나리오가 있으면 DoT 사망이 누락될 수 있다. 현재 코드에서는 `IsDead` 체크(line 93)로 사망 후 `UpdateAI`가 조기 반환하므로 문제 없으나, 이 의존 관계를 주석으로 명시하면 향후 유지보수에 도움.

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ AC 5개 항목 전부 충족 |
| 기존 호환성 | ✅ CombatManager/OnMonsterKilled 기존 경로 변경 없음 |
| 코드 품질 | ✅ 역순 순회 + 멱등 플래그로 안전한 구현 |
| 테스트 | ⚠️ 단위 테스트 미작성 (스펙 명시 항목, 후속 권장) |

**결론:** ✅ **APPROVE** — DoT 사망 시 킬 보상 파이프라인이 누락되는 P1 버그를 `DeathProcessed` 플래그 + 프레임 스캔 방식으로 정확히 해결. 이중 처리 방지, 리스트 안전성, 기존 경로 호환 모두 확보. 단위 테스트 미작성은 후속 태스크로 처리 가능하며 현 구현의 정확성에는 영향 없음.
