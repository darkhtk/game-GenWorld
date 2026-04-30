# SPEC-S-128 — QuestUI 진행률 바 추가

> **타입:** UI 폴리시 (P2, stabilize 방향: 기존 기능 개선)
> **연관 RESERVE:** S-128 "퀘스트 추적 UI 진행률 바 (현재 숫자만)"
> **선제 작성:** Coordinator 8회차 (2026-04-30)
> **상태:** Draft (미할당)

---

## 1. 목적

`QuestUI.AddActiveEntry`(`Assets/Scripts/UI/QuestUI.cs:129~203`) 의 활성 퀘스트 진행 라인은 현재 **숫자 텍스트만** (`(3/10)`) 표시. 시각적 진행률 인지가 약함 → 한눈에 진행 상태 판단 어려움. 게이지 바를 추가하여 정보 계층 강화.

대상: `requirements`(아이템 수집) + `killRequirements`(몬스터 처치) 라인.

## 2. 비목표 (Out of Scope)

- HUD 추적기(`HUD.cs:626 UpdateQuestTracker`) 진행률 바 — **별도 후속 SPEC**(중복 작업 회피, HUD는 좁은 공간 / 텍스트 위주). 본 SPEC은 메인 QuestUI 패널만.
- 보상 라인 게이지(보상은 진행률 개념 없음).
- 완료 퀘스트 탭(완료 = 100%, 게이지 의미 없음).
- QuestDef/Save 스키마 변경 — 진행률은 기존 `GetKillProgress` / `InventorySystem.GetCount` 파생값.

## 3. 호출 진입점

| 진입점 | 경로 | 트리거 |
|--------|------|--------|
| 퀘스트 패널 열기 (J 키) | `QuestUI.Show()` → `Refresh()` → `RebuildList()` → `AddActiveEntry()` | 사용자 입력 |
| 퀘스트 수락/포기 후 갱신 | `QuestSystem.AcceptQuest`/`AbandonQuest` → 이벤트 → QuestUI 재구성 | 게임 이벤트 |
| 몬스터 처치 / 아이템 획득 후 패널이 열려있을 때 | `QuestUI.Refresh()` 외부 호출 (HUD `UpdateQuestTracker` 와 별개) | (현재 외부 강제 갱신 부재 — Refresh 호출 시점 그대로 유지) |

**입력 키:** `J` (QuestUI 토글) — 기존 InputManager / UIManager 입력 처리 변경 없음.

## 4. UI 와이어프레임

### Before (현재)
```
▸ 슬라임 사냥
  슬라임 5마리를 처치하라
  ✗ 슬라임 (3/5)
  ✓ 약초 (10/10)
```

### After (SPEC-S-128)
```
▸ 슬라임 사냥
  슬라임 5마리를 처치하라
  ✗ 슬라임   ▰▰▰▰▰▰░░░░ (3/5)
  ✓ 약초     ▰▰▰▰▰▰▰▰▰▰ (10/10)
```

### 시각 사양

- **게이지 폭:** 10셀 고정 (TMP Rich Text + 유니코드 블록 글리프 `▰`/`░`).
  - 이유: TMP 단일 텍스트 노드 1개에 모두 들어가도록 — 신규 GameObject(Slider/Image filled) 추가 회피하여 prefab 변경 0건. (구조적 안전성 우선)
- **셀 채움 규칙:** `cells = Mathf.RoundToInt(progress / required * 10)`, `progress >= required` 시 10.
- **색상:**
  - 미달성(progress < required): `<color=#ff9944>▰</color>` 채움 + `<color=#444444>░</color>` 빈칸
  - 달성(progress >= required): `<color=#66ff88>▰</color>` 전체
- **체크 마크 + 카운트:** 기존 `(3/5)` 텍스트 유지 (게이지와 병기 — 정확한 숫자 동시 노출).

### 폭 정렬

- 게이지 직전에 항목명 영역을 `<mspace=8>` 또는 fixed-width 구획화는 시도하지 않음(한국어 폰트 대응 복잡). 단순히 항목명 + 공백 2개 + 게이지로 기록 — 일관 폭 강제는 향후 별도 SPEC.

## 5. 코드 변경 (예상 diff 단위)

### 파일 1: `Assets/Scripts/UI/QuestUI.cs`

`AddActiveEntry` 내 `texts[2].text` 빌더 부분(현재 144~181):

```csharp
// requirements 루프 안에서 (현재):
lines.Add($"  {check} {itemName} <color={countColor}>(<b>{have}</b>/{req.count})</color>");

// 변경 후:
string bar = BuildProgressBar(have, req.count);
lines.Add($"  {check} {itemName} {bar} <color={countColor}>(<b>{have}</b>/{req.count})</color>");
```

killRequirements 루프도 동일 패턴(`kills` / `kr.count`).

### 파일 1: 신규 헬퍼

```csharp
const int ProgressBarCells = 10;
const string ProgressBarFilledChar = "▰";    // ▰
const string ProgressBarEmptyChar  = "░";    // ░
const string ProgressBarMetColor   = "#66ff88";
const string ProgressBarUnmetColor = "#ff9944";
const string ProgressBarBgColor    = "#444444";

static string BuildProgressBar(int progress, int required)
{
    if (required <= 0) return "";
    int filled = Mathf.Clamp(Mathf.RoundToInt(progress / (float)required * ProgressBarCells),
        0, ProgressBarCells);
    bool met = progress >= required;
    string filledColor = met ? ProgressBarMetColor : ProgressBarUnmetColor;
    var sb = new System.Text.StringBuilder(64);
    sb.Append("<color=").Append(filledColor).Append(">");
    for (int i = 0; i < filled; i++) sb.Append(ProgressBarFilledChar);
    sb.Append("</color>");
    if (filled < ProgressBarCells)
    {
        sb.Append("<color=").Append(ProgressBarBgColor).Append(">");
        for (int i = filled; i < ProgressBarCells; i++) sb.Append(ProgressBarEmptyChar);
        sb.Append("</color>");
    }
    return sb.ToString();
}
```

(StringBuilder 한 번 인스턴스화 — 매 라인마다 수십 byte alloc. RebuildList 호출 빈도 낮음(패널 열릴 때만) → 무시 가능. **LINQ 금지 규약 준수**.)

### 파일 2: `Assets/Scripts/Core/GameConfig.cs`

`GameConfig.UI` 클래스에 5상수 추가 (S-122에서 신설된 클래스 재사용):

```csharp
public const int QuestProgressBarCells = 10;
public const string QuestProgressBarMetColor   = "#66ff88";
public const string QuestProgressBarUnmetColor = "#ff9944";
public const string QuestProgressBarBgColor    = "#444444";
public const bool QuestProgressBarEnabled = true;
```

QuestUI는 GameConfig.UI 상수 참조(매직 넘버 회피, S-138 정신 — 매직 넘버 const 분리).

### 변경 요약
| 파일 | 변경 LOC | 신규/수정 |
|------|---------|----------|
| `Assets/Scripts/UI/QuestUI.cs` | +30 / 수정 2라인 | 수정 |
| `Assets/Scripts/Core/GameConfig.cs` | +5 | 수정 (UI 클래스 추가) |
| `Assets/Tests/EditMode/QuestProgressBarTests.cs` | +60 | 신규 |

## 6. 테스트 (EditMode)

`Assets/Tests/EditMode/QuestProgressBarTests.cs` (NUnit, 5건):

1. `BuildProgressBar_Zero_ReturnsAllEmpty` — `progress=0, required=10` → 채움 0셀, BG 색 wrap 확인.
2. `BuildProgressBar_Half_RoundsCorrectly` — `progress=3, required=5` → filled = 6셀(0.6×10). met 색상.
3. `BuildProgressBar_Met_AllFilled_GreenColor` — `progress=10, required=10` → 10셀 met 색, BG wrap 미존재.
4. `BuildProgressBar_OverMet_ClampsToMax` — `progress=15, required=5` → 10셀 met 색.
5. `BuildProgressBar_RequiredZero_ReturnsEmpty` — `required=0` → 빈 문자열(div-by-zero 가드 검증).

테스트는 `BuildProgressBar` 를 `internal static` 으로 노출 + `[InternalsVisibleTo("EditModeTests")]` 또는 별도 `QuestProgressBarBuilder` static 클래스로 추출.

## 7. 데이터 구조 / 세이브 연동

- **신규 데이터 없음.** 진행률은 모두 파생(기존 `QuestSystem.GetKillProgress` + `InventorySystem.GetCount`).
- `QuestsData` / Save Json 스키마 변경 없음.
- 기존 `QuestSystem.Serialize/Restore` 호환.

## 8. 리스크 / 트레이드오프

| 리스크 | 완화 |
|--------|------|
| 폰트 폴백: 일부 TMP 폰트가 `▰`/`░` 글리프 부재 시 □ 박스 표시 | 기본 NotoSansKR / DungGeunMo 등 main 폰트가 두 글리프 모두 포함 검증 필요 (PlayMode 수동, DoD §6) |
| TMP Rich Text 색상 wrap 비용 | 고정 폭 게이지 = 라인당 색상 wrap 2회. 부담 미미 |
| 정렬 깨짐(가변 폭 폰트) | 게이지 좌측 항목명 폭 가변 → 게이지 시작 위치 들쭉날쭉. 본 SPEC은 허용(별도 SPEC에서 mspace 처리) |
| RebuildList GC alloc | StringBuilder + 임시 string 결합 있음. 호출 빈도 낮음(패널 open + 외부 Refresh) → 무시 |
| HUD QuestTracker 비대응 | HUD는 본 SPEC 비목표. **별도 후속 SPEC-S-128b 또는 RESERVE 신규 등재 권장** (감독관/Developer 픽업 시 본 SPEC §2 비목표 인지) |

## 9. DoD (Definition of Done)

1. ✅ `QuestUI.AddActiveEntry` 의 requirements/killRequirements 라인에 게이지 바 표시.
2. ✅ 미달성/달성 색상 분리(주황 / 초록).
3. ✅ EditMode 테스트 5건 통과.
4. ✅ 기존 텍스트(체크 마크 + `(have/required)`) 병기 유지(시각 + 정확 정보 동시).
5. ✅ HUD QuestTracker, 완료 탭, 빈 자리 placeholder 텍스트 영향 없음(회귀 0).
6. ⚠️ PlayMode 수동: 메인 폰트로 `▰`/`░` 글리프 정상 렌더 확인(Asset/QA 또는 사용자).
7. ⚠️ PlayMode 수동: 활성 퀘스트 진행 시(슬라임 처치 1마리 등) 패널 닫고 다시 열면 게이지 1셀 증가 시각 확인(QuestUI.Refresh 빈도 = 패널 open 시점).

## 10. 호출 흐름

```
[J 키 입력]
  → InputManager / UIManager.HandleQuestKey
  → QuestUI.Toggle()
  → QuestUI.Show()
  → QuestUI.Refresh()
  → QuestUI.RebuildList()
  → QuestUI.AddActiveEntry(quest) (각 활성 퀘스트마다)
    → texts[2].text = "  {check} {name} {BuildProgressBar(have, count)} (have/count)"
        ↑ 신규 게이지 삽입 지점
```

## 11. 관련 참조

- `Assets/Scripts/UI/QuestUI.cs:129~203` — AddActiveEntry (수정 대상)
- `Assets/Scripts/Systems/QuestSystem.cs:98 GetKillProgress` (소비)
- `Assets/Scripts/Core/GameConfig.cs` UI 클래스 (S-122에서 신설, 본 SPEC에서 5상수 추가)
- `Assets/Scripts/UI/HUD.cs:626 UpdateQuestTracker` (영향 0, 후속 SPEC 후보)
- RESERVE 비고: "3/10 처치 → 게이지 바 추가" (P2, UI)

## 12. 후속 후보 (BACKLOG)

- **HUD QuestTracker 진행률 바** (별도 SPEC). 좁은 공간 → 5셀 단축 게이지 또는 가로 fill bar(Image filled) 검토.
- **mspace 정렬** — 항목명 폭 통일을 위한 monospace 영역 처리.
- **퀘스트 완료 직전 펄스 효과** — 게이지 9/10 도달 시 미세 깜빡임(수아 페르소나 폴리시).
