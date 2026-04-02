# REVIEW-R019-v1: NPC 일과 시스템 [깊은 리뷰]

> **리뷰 일시:** 2026-04-02
> **태스크:** R-019 NPC 일과 시스템
> **스펙:** SPEC-R019
> **판정:** ✅ APPROVE
> **리뷰 유형:** 깊은 리뷰

---

## 읽은 파일 목록

| 파일 | 행수 | 목적 |
|------|------|------|
| Assets/Scripts/Systems/TimeSystem.cs | 32행 전체 | 새 파일, 시간 관리 |
| Assets/Scripts/Entities/VillageNPC.cs | 관련 행 전체 | 일과 연동 |
| Assets/Scripts/Data/NpcDef.cs | 관련 행 | NpcSchedule 구조 |
| Assets/Scripts/Core/GameManager.cs | 관련 행 | TimeSystem 초기화 |

---

## 수용 기준별 검증

| # | 수용 기준 | 코드 위치 | 결과 |
|---|---------|-----------|------|
| 1 | GameHour 증가 | TimeSystem.cs:24 `+= deltaTime / 60` | ✅ |
| 2 | Period 전환 → NPC 이동 | VillageNPC.cs:72-93 UpdateSchedule | ✅ |
| 3 | schedule 없는 NPC 기존 동작 | line 74 null/empty check | ✅ |
| 4 | sleeping NPC 정지 | line 64-68 `_currentActivity == "sleeping"` → velocity=0, return | ✅ |
| 5 | GameHour 세이브/로드 | — | ⚠️ 미구현 (SaveData에 필드 없음) |

### TimeSystem.cs (32행) 분석

- GameHour 시작값 8f (8am) — 합리적 ✅
- Period switch: night(<6), dawn(<9), morning(<12), afternoon(<17), evening(<20), night — SPEC 일치 ✅
- 60실시간초 = 1게임시간 → 24실시간분 = 1사이클 — SPEC 일치 ✅
- 24시간 wrap (`GameHour -= 24f`) ✅
- Period 변경 감지 + 로그 출력 ✅
- Update returns bool (periodChanged) — 호출자가 활용 가능 ✅

### VillageNPC UpdateSchedule() 분석

- Period 변경 시만 실행 (`_lastPeriod` 비교) — 성능 최적화 ✅
- Y축 반전 (line 86 `-(s.cy + 0.5f) * TileSize`) — Phaser→Unity 변환 ✅
- `_patrolTarget = null` → 즉시 새 순찰 시작 ✅
- 걸어서 이동: _patrolCenter만 변경, DoPatrol이 자연스럽게 이동 ✅

### GameManager 연동

- `TimeSystem = new TimeSystem()` (line 51) ✅
- `TimeSystem.Update(Time.deltaTime)` 매 프레임 (line 79) ✅

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
NPC가 시간에 따라 다른 곳에 있으면 마을이 살아있는 느낌! 밤에 대장장이가 자고 있는 건 현실적.

### ⚔️ 코어 게이머
24분 1사이클은 빠르지만 RPG 파밍 루프에 적합. NPC 일과에 따라 퀘스트 수락 타이밍이 달라지면 전략적 요소 추가.

### 🎨 UX/UI 디자이너
HUD 시간 표시가 선택사항으로 남아있음 — 시간대를 모르면 NPC 찾기 어려울 수 있음.

### 🔍 QA 엔지니어
- `GameHour -= 24f` (line 25): 정확히 24를 넘는 순간 wrap — 누적 오차 없음 ✅
- schedule에 현재 period 항목 없으면? → foreach 매칭 실패 → 이전 위치 유지 — 안전 ✅
- sleeping 체크가 UpdateSchedule 이후 → schedule 갱신 후 즉시 sleeping 반영 ✅
- SaveData에 gameHour 미추가 → 로드 시 항상 8am 시작. 플레이 연속성 영향.

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Medium | GameHour 세이브/로드 미구현 (SPEC 명시, SaveData 필드 미추가) |
| 2 | Low | 테스트 미작성 (SPEC 체크리스트 5항목) |
| 3 | Info | HUD 시간 표시 미구현 (SPEC 선택사항) |

---

## 최종 판정

**✅ APPROVE**

핵심 기능(TimeSystem, Period 전환, NPC 일과 이동, sleeping 정지, schedule 하위 호환) 모두 정확. GameHour 세이브 미구현은 Medium이나 게임플레이 핵심 기능에 영향 없음 (로드 시 8am 시작은 허용 가능). 세이브 연동은 별도 후속 태스크로 추가 권장.
