# REVIEW-S046-v1: MonsterSpawner Region Transition Cleanup

> **리뷰 일시:** 2026-04-03
> **태스크:** S-046 MonsterSpawner 리전 전환 클린업
> **스펙:** SPEC-S-046
> **커밋:** MonsterSpawner ClearAllMonsters — region transition monster cleanup
> **판정:** ✅ APPROVE

---

## 변경 요약

**변경 파일:** 1개 (`Assets/Scripts/Entities/MonsterSpawner.cs`)

1. **`ClearAllMonsters()` public 메서드 추가** (lines 21-33): `_monsters` 역순 순회 → null 아닌 항목에 `MonsterDespawnEvent` 발행 → `Destroy(m.gameObject)` → `_monsters.Clear()`
2. **`SpawnForRegion()` 진입부에서 `ClearAllMonsters()` 호출** (line 38): null 체크/스폰 로직 전에 기존 몬스터 전부 제거

**GameManager.cs 미변경** — 기존 `HandleRegionTransition()`이 `SpawnForRegion()` 호출하며, 내부에서 클린업 처리. 스펙 요구사항 부합.

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 기존 MonoBehaviour, 새 씬 참조 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### 이전 리전 몬스터 즉시 제거

**호출 체인:** `GameManager.Update()` → `RegionTracker.UpdatePlayerRegion()` → `HandleRegionTransition()` → `SpawnForRegion()` → `ClearAllMonsters()` (line 38, 첫 번째 문장)

- `Destroy(m.gameObject)` 프레임 끝에 파괴. 새 스폰 전 전부 제거. **PASS**

### 새 리전 몬스터 정상 스폰

- `_monsters.Clear()` 후 빈 리스트에 새 몬스터 추가. `mc.Init(def, pos)` 초기화. **PASS**

### 전투 중 어그로 몬스터 정리

- `ClearAllMonsters()`는 AI 상태 무관하게 ALL 항목 순회. Chase/Attack 상태 몬스터도 동일하게 파괴. **PASS**

### null 참조 잔존 없음

- `_monsters.Clear()` (line 32) 루프 후 호출, 리스트 완전 비움. 루프 내 `if (m != null)` 가드. **PASS**

### DespawnRoutine 충돌 없음

- `DespawnRoutine` (lines 99-126) 2초 간격 코루틴. `if (m == null)` 체크 → `RemoveAt`. `ClearAllMonsters()` 후 `_monsters` 비어있거나 새 몬스터만 포함. **PASS**

### 호환성 검증

| 시스템 | 안전성 | 비고 |
|--------|--------|------|
| CombatManager._cachedMonsters | ✅ | 동일 List 참조, Clear 후 비어있음. null/IsDead 가드 존재 |
| CombatRewardHandler.RemoveMonster | ✅ | Clear 후 `Remove()` → false 반환 (무해). 이미 파괴된 오브젝트 Destroy → no-op |
| GameManager.Update 몬스터 순회 | ✅ | HandleRegionTransition보다 먼저 실행 → 리스트 변경 전 순회 완료 |
| MonsterHPBar | ✅ | `_target == null` → `Destroy(gameObject)` 자동 정리 |
| NameLabel | ✅ | 몬스터 자식 오브젝트 → 부모 파괴 시 함께 파괴 |

### 테스트 커버리지

- `ClearAllMonsters()`에 대한 단위 테스트 없음. 메서드가 단순하므로 심각한 갭은 아님.

### 기존 버그 발견 (S-046 범위 외)

`_nightPoolBuffer`가 `isNight` 분기 내부에서만 Clear됨. 이전 호출이 야간이고 현재 호출이 주간이면 stale 야간 몬스터 ID 사용 가능. **별도 이슈로 추적 권장.**

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| Monster HP Bar | ✅ | `_target == null` → 자동 파괴 (MonsterHPBar.LateUpdate) |
| Name Label | ✅ | 몬스터 자식 → 함께 파괴 |
| Minimap 아이콘 | ✅ | `ActiveMonsters` 비어있으면 0.2초 내 아이콘 비활성화 |
| Boss HP Bar | ℹ️ | 보스전 중 리전 전환 시 stale 가능 — 극히 드문 엣지 케이스, S-046 범위 외 |
| DamageText/VFX | ✅ | fire-and-forget, 진행 중 참조 없음 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 일반 리전 전환 (비전투) | 이전 몬스터 즉시 제거 → 새 리전 몬스터 스폰 |
| 전투 중 리전 전환 | 어그로 몬스터 즉시 제거, 추적 중단. 전투 깔끔하게 종료 |
| 빠른 리전 경계 왕복 | 매번 ClearAll → 재스폰. 팝핑 현상 있으나 크래시/누수 없음 |
| 몬스터 없는 리전 진입 | ClearAll 실행 → early return (monsterIds null/empty). 정상 |
| DespawnRoutine 전환 프레임 중 실행 | _monsters 비어있음 → 루프 미실행. 충돌 없음 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
"마을로 갔는데 숲 늑대가 따라오는 거 무서웠다. 이제 리전 넘으면 깨끗하게 사라진다고? 좋다. 다만 몬스터가 그냥 '뿅' 사라지는데, 페이드 아웃 같은 거 있으면 더 좋겠다. 큰 문제는 아님."

### ⚔️ 코어 게이머
"리전 경계 몬스터로 안전지대 견인 후 쉽게 잡는 익스플로잇이 막혔다. 좋은 수정. 다만 50마리 킬 퀘스트 중 마지막 몬스터 잡으려는데 실수로 리전 넘으면 킬 놓치는 건 좀 아쉬울 수 있다. 동작은 맞지만 QoL로 경계 근처 유예 구간이 있으면 좋겠다. 이벤트 발행이 정확해서 향후 어그로 표시기 같은 시스템에도 문제 없을 것."

### 🎨 UX/UI 디자이너
"HP 바, 이름표, 미니맵 아이콘 모두 자동 정리됨. 고아 UI 없음. 미니맵은 0.2초 폴링이라 아이콘이 잠깐 남을 수 있으나 인지 불가 수준. 리전 이름 알림 → 몬스터 소멸 → 새 몬스터 스폰 순서 자연스러움."

### 🔍 QA 엔지니어

| 체크 | 결과 | 비고 |
|------|------|------|
| ClearAllMonsters 메서드 존재 | ✅ | 역순 순회 + null 가드 + 이벤트 + Destroy + Clear |
| SpawnForRegion 진입부 ClearAll 호출 | ✅ | line 38, 첫 번째 문장 |
| GameManager 호출 순서 유지 | ✅ | HandleRegionTransition 미변경 |
| MonsterDespawnEvent 발행 | ✅ | null 가드 내 발행 |
| 역순 순회 정확성 | ✅ | `for (i = Count-1; i >= 0; i--)` |
| null 안전 접근 | ✅ | `if (m != null)` 가드 |
| _monsters.Clear() | ✅ | 루프 후 호출 |
| DespawnRoutine null 안전 | ✅ | `if (m == null) { RemoveAt; continue; }` |
| MonsterHPBar 자동 정리 | ✅ | `_target == null → Destroy` |
| NameLabel 자동 정리 | ✅ | 자식 오브젝트 자동 파괴 |
| 미니맵 아이콘 정리 | ✅ | ActiveMonsters 비어있으면 아이콘 비활성화 |
| CombatManager stale 참조 안전 | ✅ | null/IsDead 가드 |
| 단위 테스트 | ⚠️ | 없음 — 단순 메서드라 심각하지 않음 |
| _nightPoolBuffer stale (기존 버그) | ℹ️ | S-046 범위 외, 별도 추적 필요 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 스펙 전 항목 충족 |
| 기존 호환성 | ✅ 다운스트림 시스템 null 가드 완비 |
| 코드 품질 | ✅ 최소 변경, 역순 순회, 방어적 코딩 |
| 아키텍처 | ✅ SpawnForRegion 내부 처리 — 호출측 변경 불필요 |

**결론:** ✅ **APPROVE** — 스펙에 정확히 부합하는 구현. `ClearAllMonsters()`가 올바르고, 최소한이며, 잘 가드됨. 모든 다운스트림 시스템(HP 바, 이름표, 미니맵, CombatManager, DespawnRoutine)이 기존 null 체크와 Unity 자식 파괴 시맨틱을 통해 안전하게 처리. 회귀 없음. `_nightPoolBuffer` 기존 버그는 별도 이슈로 추적 권장.
