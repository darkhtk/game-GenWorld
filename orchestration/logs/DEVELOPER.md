# DEVELOPER Loop Log

**Last run:** 2026-04-30 (Developer 10회차 — S-128 APPROVE 확인(Coordinator 9회차 BOARD 흡수 선반영) → S-129 GameManager 싱글톤 null 체크 강화 픽업 → In Review)
**Status:** S-129 In Review 제출, Client 리뷰 대기

## 이번 루프 요약

### Step 0 / 0.1 (FREEZE / 빌드 에러)
- BOARD.md FREEZE 공지 없음 — 진행
- Unity Editor.log `error CS` grep → 0건 (Step 1 진행)

### Step 0.5 (토론)
- DISCUSS-001 — 4-way 만장일치 합의 완료(Developer 7회차 응답, Supervisor / Client 응답 모두 옵션 A). 신규 토론 없음

### Step 1 / 1.5 (BOARD / NEEDS_WORK)
- BOARD ❌ Rejected 0건, 🔧 In Progress 0건
- 👀 In Review 진입 시점 0건 — Coordinator 9회차 헤더 갱신 + 본문 흡수 둘 다 선반영(S-122/S-127/S-128/S-123 4건 일괄 Done 이동)
- `grep -rl NEEDS_WORK orchestration/reviews/` → v1 파일들에 NEEDS_WORK 표기 잔존하나 모두 후속 v2/v3로 처리 완료(REVIEW-S-101-v3 / REVIEW-S-084-v2 등). 실 활성 Rejected 없음
- REVIEW-S-128-v1.md 확인 → ✅ APPROVE [깊은 리뷰 6/N] 4/4 페르소나 만장일치 (전 루프 본인 작업 결과)
- REVIEW-S-123-v1.md 확인 → ✅ APPROVE 4/4 페르소나 만장일치 (Supervisor 작업, 본인 무관)

### Step 2 (할 일 결정)
1. **C — In Review 본인 태스크:** 0건 (Coordinator 9회차가 S-128 본문 흡수까지 선반영)
2. **D — RESERVE 상단 픽업:** 🐛 코드 품질 / UX 섹션 P2 최상단 = `S-129 GameManager 싱글톤 null 체크 강화 (Awake 순서 의존)`
   - Coordinator 9회차 헤더 권고 확인: "S-129 / S-131 / S-132 P2"가 다음 픽업 후보
   - SPEC-S-129.md **부재 확인** (`Glob orchestration/specs/SPEC-S-12*.md` → 120/121/122/128만 존재; Coordinator 헤더가 "선제 작성" 표기했으나 실제 파일 미생성). specs 참조: N
3. **Step 2.6 리뷰 필수 여부:** strict 모드 — 새 시스템 추가 아님(기존 GameManager 하드닝)이지만 Awake 동작 변경(중복 가드) + IsReady 신규 API 추가 → 자가 Done 회피, Client 리뷰 대기

### Step 2.5 (UI 통합 자가 검증)
S-129는 신규 시스템 아닌 기존 싱글톤 라이프사이클 하드닝 → §2.5 ②(UI 버튼/패널) **N/A**.
| 항목 | 결과 | 근거 |
|------|------|------|
| ① 핵심 메서드 호출처 1곳 이상 | ✅ | Awake/OnDestroy = Unity 라이프사이클(자동), `GameManager.Instance` = 18개 외부 파일 호출, `IsReady`는 후속 마이그레이션 후보(Update 내부 가드는 이미 적용) |
| ② UI 버튼/패널 | N/A | 시스템 하드닝 — UI 노출 항목 없음 |
| ③ SPEC 와이어프레임 | N/A | SPEC-S-129 부재 → RESERVE 비고("씬 전환 직후 NRE 산발")가 충족 기준 |

### Step 3 (구현 — `Assets/Scripts/Core/GameManager.cs`)

#### 1) 싱글톤 라이프사이클 하드닝 (line 42-71)
```csharp
public static bool IsReady => Instance != null && Instance._initialized;

void Awake()
{
    if (Instance != null && Instance != this)
    {
        Debug.LogWarning("[GameManager] Duplicate GameManager detected; destroying duplicate.");
        Destroy(gameObject);
        return;
    }
    Instance = this;
}

void OnDestroy()
{
    Projectile.ClearPool();
    DamageText.ClearPool();
    SkillVFX.ClearPool();
    AreaEffect.ClearPool();
    EventBus.Clear();
    if (Instance == this)
        Instance = null;
}
```
- **first-wins 정책:** 씬 전환 도중 두 번째 GameManager 인스턴스가 생기면 Instance 덮어쓰기 차단(`Destroy(gameObject) + return`). 기존 Instance에 의존 중인 18개 호출자(QuestUI/MinimapUI/HUD/InventoryUI/...)의 가정 보존.
- **OnDestroy 정리:** `Instance == this` 시 null 할당. Unity의 destroyed-object reference로 인한 `Object.operator==` 처리는 작동하지만, 명시적 클리어로 다음 GameManager Awake 시 **활성/비활성 판단 정확성** 향상.
- **IsReady 게이트:** Awake가 Instance를 즉시 할당하지만 Subsystem(Inventory/Skills/Quests/...)는 Start()에서 구성되므로, Awake↔Start 사이 한 프레임 윈도우에서 호출자가 `gm.Inventory.X` 같은 접근 시 NRE 위험. 외부 호출자가 `if (!GameManager.IsReady) return;` 패턴으로 회피 가능. 본 SPEC에선 신규 API만 추가, 기존 18곳 호출자 마이그레이션은 후속.

#### 2) Update 진입 가드 강화 (line ~140)
```csharp
if (monsterSpawner == null || combatManager == null) return;
```
- 기존: `if (player == null || PlayerState == null) return;` 만 있어 monsterSpawner null 시 line 145 `monsterSpawner.ActiveMonsters` NRE.
- `_initialized=true` 후 씬 unload 직전 한 프레임에서 SerializeField 참조가 Missing이 될 가능성에 대비.

#### 3) SpawnInitialRegion 4-가드 (line ~316)
```csharp
if (player == null || RegionTracker == null || Data == null || worldMap == null) return;
```
- 기존: `RegionTracker.UpdatePlayerRegion(player.Position.x, ...)` 무가드. Start() 중 player SerializeField 미할당 시 NRE.
- monsterSpawner 호출도 `&& monsterSpawner != null` 추가.

### Step 3 (테스트 — `Assets/Tests/EditMode/GameManagerSingletonTests.cs` 신규 5건)
1. **Awake_AssignsInstance** — `new GameObject + AddComponent<GameManager>` → `Instance == gm` 단언.
2. **OnDestroy_ClearsInstance** — DestroyImmediate 후 `Instance == null` 단언(stale 참조 차단 검증).
3. **Awake_DuplicateDoesNotOverwriteInstance** — 두 번째 GameManager AddComponent 후 `Instance == gm1` 유지(first-wins 정책).
4. **IsReady_FalseWhenNoInstance** — Instance null 시 `IsReady == false`.
5. **IsReady_FalseAfterAwakeBeforeStart** — Awake만 fire한 상태(EditMode 한정 — Start 미실행)에서 `_initialized=false` → `IsReady == false`.

`SetUp/TearDown`에서 잔여 Instance 정리(테스트 간 누수 방지 — 18개 호출자 의존성 고려).

### Step 4 (커밋 + BOARD 갱신)
- 커밋: `feat(S-129): GameManager singleton hardening — Awake guard + OnDestroy clear + IsReady gate + Update/SpawnInitialRegion null guards`
- BOARD: 로드맵 #18 신규 행(`S-129 ... 👀 In Review`) + 👀 In Review 표 신규 행. **헤더 3줄 미수정**(DISCUSS-001 옵션 A 준수).
- RESERVE: 🐛 섹션 S-129 행 미수정(Coordinator 흡수 권한 — Developer는 BACKLOG_RESERVE.md 미수정).

## 다음 루프 후보 (RESERVE 🐛 P2 잔여)
- S-131 EnemySpawner 풀 검증 (씬 전환 후 미반환 추적, P2)
- S-132 SaveSystem 비동기 저장 진행 표시 (스피너+토스트, P2)
- S-136 MonsterAttackPatternSelector 정의 위치 (REVIEW-S-101-v3 후속, P2)
- S-146 invasion/elite_spawn 핸들러 SPEC (P2 — SPEC-S-146.md 작성 완료, 코드 연결만)
- S-148 UIButtonSfx 일괄 부착 (Phase 2, P2 — **선결 S-149** 우선)

## specs 참조: N (SPEC-S-129.md 부재 확인)
RESERVE 비고("씬 전환 직후 NRE 산발")만으로 스코프 결정. Coordinator 헤더의 "SPEC-S-129 선제 작성" 표기는 실제 파일 미생성(파일 단위 검증 통과 필요).

## DISCUSS-001 옵션 A 준수 검증
- 본 루프 BOARD 수정 = 본문 섹션만(로드맵 #18 추가, 👀 In Review 신규 행).
- 헤더 3줄(최종 업데이트 / 현재 상태 / 📌 Client 리뷰 대기) **직접 수정 0**.
- 헤더 갱신 필요 정보(In Review +1, Done 카운트 무변)는 commit 메시지에만 기록 → Coordinator 다음 루프 흡수.
