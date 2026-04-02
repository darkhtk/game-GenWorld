# SUPERVISOR Loop Log

> **최종 실행:** 2026-04-03 (루프 #2)
> **모드:** 성능 최적화

## 이번 루프 수행 내용

### 성능 최적화 3건 ✅

1. **RefreshHud 스킵 최적화** (GameManager.cs)
   - 매 프레임 HP/MP 바 갱신 → 값 변경 시에만 갱신
   - `_lastHudHp`, `_lastHudMp` 더티 플래그 도입
   - 영향: 대부분의 프레임에서 UI 업데이트 호출 제거

2. **playerPos 이중 접근 제거** (GameManager.cs)
   - `player.Position` 프로퍼티를 Update()에서 2회→1회 접근
   - `RegionTracker.UpdatePlayerRegion`에서 캐시된 playerPos 재사용

3. **야간 몬스터 풀 할당 제거** (MonsterSpawner.cs)
   - 야간 스폰 시 `new List<string>()` + `ToArray()` 매번 할당
   - `_nightPoolBuffer` 재사용 버퍼로 교체, GC 압력 감소

## 다음 루프 예정
- UX 개선 (#4) 또는 에러 점검 (#5)
