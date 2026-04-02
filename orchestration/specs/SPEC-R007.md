# SPEC-R007: 몬스터 어그로/리쉬 시스템 개선

## 목적
기존 MonsterController의 Return 상태를 개선하여 어그로 메커니즘을 더 견고하고 플레이어 친화적으로 만든다.

## 현재 상태
- `MonsterController.cs:63-96` — AI 상태 머신에 Patrol/Chase/Attack/Return 4상태 이미 존재.
- Return 조건: `distToPlayer > detectRange * ChaseRangeMult` 또는 `Distance(pos, spawnPos) > MaxSpawnDistance`.
- Return 중 HP 완전 회복 (line 90), 플레이어 근접 시 재추격 (line 93-94).
- **문제:** Return 중에도 공격당함 → 영원히 리쉬 불가. 시각적 피드백 없음.

## 구현 명세

### 수정 파일
- `Assets/Scripts/Entities/MonsterController.cs`

### 변경사항

1. **Return 중 피해 감소 (80%):**
   ```csharp
   public bool IsReturning => AIState == MonsterAIState.Return;
   
   // TakeDamage 수정:
   public bool TakeDamage(int dmg)
   {
       if (IsReturning) dmg = Mathf.Max(1, dmg / 5); // 80% 감소
       Hp -= dmg;
       return Hp <= 0;
   }
   ```

2. **Return 중 이동 속도 1.5배:**
   ```csharp
   case MonsterAIState.Return:
       MoveToward(_spawnPos, speed * 1.5f); // 기존 speed → speed * 1.5f
   ```

3. **리쉬 타이머 추가 — Return 5초 후 강제 복귀(텔레포트):**
   ```csharp
   float _returnStartTime;
   const float RETURN_FORCE_TELEPORT = 5f;
   
   // Return 상태 진입 시:
   _returnStartTime = Time.time;
   
   // Return 업데이트에서:
   if (Time.time - _returnStartTime > RETURN_FORCE_TELEPORT)
   {
       transform.position = _spawnPos;
       Hp = Def.hp;
       AIState = MonsterAIState.Patrol;
   }
   ```

4. **Return 중 재추격 조건 강화:**
   ```csharp
   // 기존: if (distToPlayer <= Def.detectRange) → 재추격
   // 변경: Return 중에는 detectRange * 0.5f 이내에서만 재추격
   if (distToPlayer <= Def.detectRange * 0.5f)
       AIState = MonsterAIState.Chase;
   ```

5. **어그로 테이블 (선택사항):**
   - `LastHitByPlayerTime` 필드 이미 존재. Return 판단에 활용:
   - 최근 2초 이내 피격 → Return 진입 불가 (전투 지속).

### 수치
| 파라미터 | 값 | 비고 |
|---------|-----|------|
| Return 피해 감소 | 80% | 최소 1 |
| Return 속도 배율 | 1.5x | |
| 강제 텔레포트 시간 | 5초 | Return 진입 후 |
| 재추격 범위 배율 | 0.5x | detectRange 기준 |
| 피격 후 Return 불가 시간 | 2초 | LastHitByPlayerTime 기준 |

### 세이브 연동
없음. AI 상태는 런타임 전용.

## 호출 진입점
- 자동. MonsterController.UpdateAI() 내부 로직. UI 진입점 없음.

## 테스트 항목
- [ ] 리쉬 거리 초과 시 Return 상태 진입
- [ ] Return 중 피해가 80% 감소하는지
- [ ] Return 5초 후 스폰 지점에 텔레포트하는지
- [ ] Return 중 재추격 범위가 축소되는지
- [ ] 피격 직후(2초 이내) Return 진입이 불가한지
- [ ] 복귀 후 HP가 완전 회복되는지
