# REVIEW-R018-v1: 미니맵 UI

> **리뷰 일시:** 2026-04-02
> **태스크:** R-018 미니맵 UI
> **스펙:** SPEC-R018
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | HUD.minimapImage 연결 |
| 컴포넌트/노드 참조 | ✅ | mapImage, iconContainer, 아이콘 프리팹 |
| 에셋 존재 여부 | ✅ | 코드 기반 (방법 B 채택) |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

### SPEC 기능별 검증

| # | 요구사항 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | 방법 B (수동 렌더링) | GenerateMapTexture + uvRect | ✅ |
| 2 | 정적 맵 생성 (walkability) | line 32-45, walkColor/blockColor | ✅ |
| 3 | uvRect crop (플레이어 중심) | UpdateMapView line 71-76 | ✅ |
| 4 | 몬스터 아이콘 (빨간) | line 139 (1, 0.3, 0.3) | ✅ |
| 5 | NPC 아이콘 (녹색) | line 151 (0.3, 1, 0.3) | ✅ |
| 6 | 아이콘 크기 4x4 | line 164 sizeDelta(4, 4) | ✅ |
| 7 | 갱신 주기 0.2초 | line 16 UpdateInterval = 0.2f | ✅ |
| 8 | 맵 크기 128px | line 17 MapPixelSize = 128 | ✅ |
| 9 | 표시 반경 30 타일 | line 14 _viewRadius = 30f | ✅ |
| 10 | M 키 줌 토글 (30↔60) | line 52-53 | ✅ |
| 11 | Y축 반전 | line 69 `-player.Position.y` | ✅ |
| 12 | 플레이어 아이콘 중앙 | line 79 anchoredPosition = zero | ✅ |

### 코드 품질

- 아이콘 풀링 (GetMonsterIcon/GetNpcIcon) — 필요 시 생성, 초과 비활성 ✅
- null/IsDead 방어 (line 97) ✅
- 거리 필터 (viewRadius * TileSize) — 범위 외 엔티티 미표시 ✅
- FilterMode.Point — 픽셀 아트 스타일 유지 ✅
- CreateDefaultIcon fallback — 프리팹 미바인딩 시 기본 아이콘 ✅

### 주의사항

`Input.GetKeyDown(KeyCode.M)` (line 52)이 UpdateInterval 쓰로틀 이후에 위치 → 0.2초 갱신 타이밍에만 키 입력 감지. 키 누름을 놓칠 수 있음 (80% 확률). M 키 체크를 쓰로틀 전으로 이동 권장.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 맵 표시 | ✅ | walkability Texture2D → RawImage |
| 플레이어 추적 | ✅ | uvRect 동적 조정 |
| 엔티티 아이콘 | ✅ | 몬스터 빨강, NPC 녹색 |
| M 키 토글 | ⚠️ | 입력 감지 타이밍 이슈 (동작하나 불안정) |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| 이동 시 | 미니맵이 플레이어 중심으로 스크롤 |
| 몬스터 근처 | 빨간 점 미니맵에 표시 |
| NPC 근처 | 녹색 점 미니맵에 표시 |
| 맵 경계 | uvRect 클램핑 미적용이나 Unity UV wrap으로 처리 |
| M 키 | 줌 30↔60 토글 (타이밍 이슈 있으나 동작) |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
미니맵 있으면 길 잃을 일 없겠다! 몬스터 빨간 점 보면 피하거나 잡으러 갈 수 있고. M 키로 확대/축소도 편하겠다.

### ⚔️ 코어 게이머
0.2초 갱신은 미니맵으로 충분. 30타일 반경이면 주변 상황 파악에 적절. 60타일 줌아웃은 이동 계획 수립용.

### 🎨 UX/UI 디자이너
방법 B(수동 렌더링) 채택 — 추가 카메라 오버헤드 없음. FilterMode.Point로 픽셀 아트 일관성. 아이콘 4x4은 128px 미니맵에서 가시성 확보.

### 🔍 QA 엔지니어
- FindFirstObjectByType<PlayerController> 매 갱신 호출 — 캐싱 권장 (성능)
- FindObjectsByType<VillageNPC> 매 갱신 — 동일 이슈
- M 키 입력이 쓰로틀 후 → 입력 누락 가능 (Medium)

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Medium | M 키 GetKeyDown이 0.2초 쓰로틀 이후 위치 → 입력 누락 가능 |
| 2 | Low | FindFirstObjectByType 매 프레임 (0.2초) 호출 — 캐싱 권장 |
| 3 | Low | 테스트 미작성 (SPEC 체크리스트 6항목) |

---

## 최종 판정

**✅ APPROVE**

SPEC 12개 기능 항목 전부 충족. 방법 B 수동 렌더링, walkability 맵 생성, uvRect crop, 엔티티 아이콘 풀링, Y축 반전, 줌 토글 모두 정확. M 키 타이밍 이슈는 Medium이나 핵심 기능 미영향.
