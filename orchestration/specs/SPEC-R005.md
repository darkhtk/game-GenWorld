# SPEC-R005: CombatManager null 참조 방어

## 목적
전투 중 몬스터나 플레이어가 파괴될 때 발생할 수 있는 NullReferenceException을 방지한다.

## 현재 상태
- `CombatManager.cs` — 일부 null 체크 존재 (`m == null || m.IsDead`, `PlayerState == null`).
- 그러나 **비동기 타이밍 이슈** 미처리:
  - `PerformAutoAttack` 루프 중 다른 몬스터 사망으로 리스트 변경 가능.
  - `FireMonsterProjectile`의 `OnArrive` 콜백 시점에 `_player`가 null일 수 있음.
  - `ExecuteSkill`에서 `_cachedMonsters`가 stale 참조일 수 있음.

## 구현 명세

### 수정 파일
- `Assets/Scripts/Systems/CombatManager.cs`

### 변경사항

1. **PerformAutoAttack — 역순 순회:**
   ```csharp
   // 현재: for (int i = 0; i < monsters.Count; i++)
   // 변경: for (int i = monsters.Count - 1; i >= 0; i--)
   // 이유: 루프 중 리스트 변경 시 인덱스 안전
   ```

2. **HandleMonsterAttacks — 동일하게 역순 순회.**

3. **FireMonsterProjectile.OnArrive — 방어 체크 강화:**
   ```csharp
   proj.OnArrive = arrivePos =>
   {
       if (_player == null || PlayerState == null) return; // ADD
       if (_player.Invincible || _player.IsDodging) return;
       // ...
       if (m == null || m.IsDead) return; // ADD: 발사 후 몬스터 사망 시
       // ...
   };
   ```

4. **DealDamageToMonster — 이미 `m == null || m.IsDead` 체크 있음. 유지.**

5. **ExecuteSkill — _cachedMonsters 갱신 보장:**
   ```csharp
   public void ExecuteSkill(int slot)
   {
       if (Skills == null || PlayerState == null || _player == null) return; // ADD _player
       // ...
   }
   ```

### 세이브 연동
없음. 런타임 방어 로직만.

## 호출 진입점
- 기존 호출 경로 변경 없음. CombatManager의 내부 방어 강화.

## 테스트 항목
- [ ] 몬스터 리스트에 null 항목이 있을 때 크래시하지 않는지
- [ ] 스킬 실행 중 플레이어 사망 시 크래시하지 않는지
- [ ] 프로젝트 도착 시 발사 몬스터가 이미 죽었을 때 크래시하지 않는지
- [ ] 역순 순회 후에도 전투 결과가 동일한지
