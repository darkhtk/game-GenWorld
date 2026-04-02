# SPEC-B-002: NPC 제자리 흔들림 수정

> **우선순위:** P0 (사용자 버그 리포트)
> **증상:** NPC가 이동하지 않고 제자리에서 흔들리는 것처럼 보임.

---

## 진단 포인트

### 1. 도착 임계값 vs 순찰 반경 불일치 (가장 유력)
- **파일:** `Assets/Scripts/Entities/VillageNPC.cs:112`
- `DoPatrol()` 내 도착 판정: `sqrMagnitude < 16f` → 거리 4.0 유닛 이내면 "도착"
- 기본 순찰 반경: `radius = r * GameConfig.TileSize` (r=2 기본)
- **문제:** `GameConfig.TileSize`가 1이면, 순찰 반경(2.0) < 도착 임계값(4.0). NPC는 항상 즉시 "도착" 판정 → 매 프레임 새 목표 선정 → 흔들림.
- **수정:** 도착 임계값을 `0.1f` (sqrMagnitude `0.01f`) 정도로 줄이거나, `GameConfig.TileSize` 확인하여 적절히 조정.

### 2. 속도 관련
- `_speed = 1f` 고정 — 별도 이상 없으나, 도착 임계값 수정 후 이동이 자연스러운지 확인.

### 3. 일과 시스템 (`UpdateSchedule`)
- **파일:** `VillageNPC.cs:85-106`
- `TimeSystem.Period` 변경 시 `_patrolCenter` 재설정 + `_patrolTarget = null`.
- Period가 빈번히 변경되면 매번 목표 초기화 → 추가 흔들림 유발 가능.
- `_lastPeriod` 캐싱은 정상.

### 4. Rigidbody2D 설정
- `gravityScale = 0`, `freezeRotation = true` — 정상.
- Drag 값 확인 필요 (Inspector에서 Linear Drag > 0이면 이동 감쇄).

## 수정 방향
1. `DoPatrol()` 도착 임계값을 `0.05f` (거리 ~0.22 유닛) 정도로 줄임.
2. 도착 후 랜덤 대기 시간 (1~3초) 추가하여 자연스러운 배회 구현.
3. `GameConfig.TileSize` 값 확인 후 순찰 반경이 합리적인지 점검.

## 검증
- NPC가 순찰 경로를 따라 자연스럽게 이동하는지 확인.
- 시간대 전환(morning→afternoon) 시 새 위치로 이동하는지 확인.
- 플레이어가 없는 리전에서 NPC가 비활성화되더라도 복귀 시 정상 동작.
