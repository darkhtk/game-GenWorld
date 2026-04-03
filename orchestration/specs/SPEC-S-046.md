# SPEC-S-046: MonsterSpawner 리전 전환 클린업

> **우선순위:** P2
> **태그:** 🔧 안정성
> **관련 파일:** MonsterSpawner.cs, GameManager.cs (HandleRegionTransition)

## 문제

리전 전환 시 이전 리전의 몬스터를 명시적으로 제거하지 않음.
- `SpawnForRegion()`: 새 리전 몬스터만 스폰, 기존 몬스터는 건드리지 않음
- `DespawnRoutine()`: 2초 간격으로 거리 > 50 타일인 몬스터만 점진적으로 디스폰
- **결과:** 리전 경계 근처 몬스터는 거리 조건 미충족으로 장시간 잔존 가능

**증상:**
1. 다른 리전 몬스터가 새 리전에서 보임 (세계관 파괴)
2. 몬스터 오브젝트 누적 (메모리/성능 저하)
3. 전투 중이던 몬스터가 리전 전환 후에도 어그로 유지

## 수정 범위

### MonsterSpawner.cs

1. **`ClearAllMonsters()` 메서드 추가:**
   - `_monsters` 리스트 순회 → 전부 Destroy
   - `_monsters.Clear()`
   - MonsterDespawnEvent 발행 (각각)

2. **`SpawnForRegion()` 진입부에서 `ClearAllMonsters()` 호출:**
   - 새 리전 스폰 전 기존 몬스터 전부 제거
   - spawn grace period 리셋

### GameManager.cs (HandleRegionTransition)

3. 기존 호출 순서 유지:
   ```
   HandleRegionTransition() {
     MonsterSpawner.SpawnForRegion(newRegion, ...)  // 내부에서 ClearAll 먼저
   }
   ```
   - 별도 호출 불필요 — SpawnForRegion 내부에서 처리

## 검증 항목

- [ ] 리전 전환 시 이전 리전 몬스터 즉시 제거됨
- [ ] 새 리전 몬스터가 정상적으로 스폰됨
- [ ] 전투 중 리전 전환 시 어그로 몬스터 정리됨
- [ ] _monsters 리스트에 null 참조 잔존 없음
- [ ] DespawnRoutine이 ClearAll과 충돌 안 함 (destroyed 체크)

## 호출 진입점

- GameManager.Update() → RegionTracker.UpdatePlayerRegion() → 리전 변경 감지 → HandleRegionTransition() → MonsterSpawner.SpawnForRegion()

## 주의사항

- `DespawnRoutine()`의 foreach에서 null/destroyed MonsterController 접근 방지 필요
- ClearAll 후 SpawnForRegion 시 `_monsters`가 비어있어야 함 (순서 보장)
