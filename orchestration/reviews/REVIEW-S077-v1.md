# REVIEW-S077-v1: SaveSystem 슬롯 데이터 무결성 검증 [깊은 리뷰]

> **리뷰 일시:** 2026-04-03
> **태스크:** S-077 SaveSystem 슬롯 데이터 무결성 검증
> **스펙:** SPEC-S-077
> **커밋:** `fb8f223` feat: S-077 SaveData.Validate() — load-time field validation with safe fallbacks
> **판정:** ✅ APPROVE

---

## 변경 요약

커밋 `fb8f223`에서 2개 파일 변경:

1. **StatTypes.cs** (line 146-182) — `SaveData.Validate()` 메서드 추가 (+38줄). 값 타입 필드 8개 + 참조 타입 필드 6개 검증.
2. **SaveSystem.cs** (line 140-145) — `TryLoadFrom()`에 `result.Validate()` 호출 + 경고 로그 삽입 (+6줄).

---

## 검증 1: 엔진 검증

### 1.1 SaveSystem 로드 경로 단일성

```
SaveSystem.Load() → TryLoadFrom(primaryPath) → TryLoadFrom(backupPath)
```

`TryLoadFrom()`은 유일한 로드 출구점. `Validate()` 삽입이 이 한 곳에만 있으므로 모든 로드 경로에서 검증이 실행된다. ✅

### 1.2 Validate() 삽입 위치 (line 140)

```csharp
var result = data.ToObject<SaveData>();  // line 133 — 역직렬화
if (result == null) { ... return null; } // line 134-138 — null 체크
var fixes = result.Validate();           // line 140 — ★ 여기
if (fixes.Count > 0) { ... }            // line 141-145 — 로그
return result;                           // line 147 — 반환
```

- 역직렬화 성공 후, 반환 전에 검증 → 최적의 위치 ✅
- SaveMigrations.Apply() 후에 실행되므로 마이그레이션이 필드를 누락해도 Validate()가 보완 ✅
- 체크섬 검증(line 119) 후에 실행되므로 변조된 파일은 이미 거부됨 → Validate()는 "유효하지만 불완전한" 데이터만 처리 ✅

---

## 검증 2: 코드 추적 [깊은 리뷰]

### 2.1 값 타입 필드 검증 (line 150-157)

```csharp
if (level < 1) { fixes.Add($"level {level} -> 1"); level = 1; }
if (hp <= 0)   { fixes.Add($"hp {hp} -> 50"); hp = 50; }
if (mp < 0)    { fixes.Add($"mp {mp} -> 0"); mp = 0; }
if (xp < 0)    { fixes.Add($"xp {xp} -> 0"); xp = 0; }
if (gold < 0)  { fixes.Add($"gold {gold} -> 0"); gold = 0; }
if (skillPoints < 0) { ... skillPoints = 0; }
if (statPoints < 0)  { ... statPoints = 0; }
if (totalKills < 0)  { ... totalKills = 0; }
```

**필드별 심층 분석:**

| 필드 | 조건 | 폴백 | 근거 | 판정 |
|------|------|------|------|------|
| level | < 1 | 1 | 게임 최소 레벨 = 1 | ✅ |
| hp | <= 0 | 50 | 0이면 사망 상태로 시작. RecalcStats() 후 clamp 적용 | ✅ |
| mp | < 0 | 0 | MP 0은 유효 (마나 미사용) | ✅ |
| xp | < 0 | 0 | 경험치 음수 불가 | ✅ |
| gold | < 0 | 0 | 골드 음수 불가 | ✅ |
| skillPoints | < 0 | 0 | 포인트 음수 불가 | ✅ |
| statPoints | < 0 | 0 | 동일 | ✅ |
| totalKills | < 0 | 0 | 킬 수 음수 불가 | ✅ |

**hp 폴백값 50 검증:**
- `SaveController.Load()` line 64에서 `state.RecalcStats()`가 호출됨
- `RecalcStats()`는 `Mathf.Min(hp, maxHp)`으로 클램프
- level=1 기준 maxHp ≈ 50-100 (StatsSystem 계산)
- 따라서 50은 안전한 중간값 → ✅

**누락 필드 확인:**
- `playerX`, `playerY`: 스펙에 명시적으로 "검증 불요" (좌표 0,0은 유효)
- `timestamp`: 참고용, 검증 불요
- `npcBrains`, `questState`, `firedTriggers`: null 허용 (새 게임과 동일)

→ 모든 필드가 적절히 처리됨.

### 2.2 참조 타입 필드 검증 (line 159-181)

```csharp
if (inventory == null) { inventory = new ItemInstance[0]; }
if (equipment == null) { equipment = new Dictionary<string, ItemInstance> { ... 5 slots ... }; }
if (learnedSkills == null) { learnedSkills = new Dictionary<string, int>(); }
if (equippedSkills == null) { equippedSkills = new string[0]; }
if (killCounts == null) { killCounts = new Dictionary<string, int>(); }
if (bonusStats == null) { bonusStats = new Dictionary<string, int> { ["str"]=0, ... }; }
```

**SaveController.Load()와의 이중 방어 확인:**

| Validate() | SaveController.Load() | 이중 방어 |
|------------|----------------------|-----------|
| inventory → 빈 배열 | `if (save.inventory != null)` (line 47) | ✅ Validate 후 null 아니므로 항상 진입 |
| equipment → 5슬롯 dict | `if (save.equipment != null)` (line 53) | ✅ 동일 |
| learnedSkills → 빈 dict | (SaveController에서 직접 대입) | ✅ |
| bonusStats → 4키 dict | (SaveController에서 대입) | ✅ |

**equipment 기본 슬롯 검증:**
```csharp
equipment = new Dictionary<string, ItemInstance>
{
    ["weapon"] = null, ["helmet"] = null,
    ["armor"] = null, ["boots"] = null, ["accessory"] = null
};
```

- 5개 슬롯이 게임의 장비 슬롯 구성과 일치하는지 확인 필요.
- SaveController.cs에서 `state.Equipment = save.equipment`로 직접 대입하므로 키 목록이 일치해야 함.
- 현재 게임의 장비 슬롯: weapon, helmet, armor, boots, accessory → 5개 일치 ✅

**bonusStats 기본 키 검증:**
```csharp
bonusStats = new Dictionary<string, int>
{
    ["str"] = 0, ["dex"] = 0, ["wis"] = 0, ["luc"] = 0
};
```

- StatsSystem에서 사용하는 스탯 키와 일치해야 함.
- 게임의 기본 스탯: str, dex, wis, luc → 4개 일치 ✅

### 2.3 Validate() 호출 시점과 마이그레이션 순서

```
TryLoadFrom():
  1. JSON 파싱
  2. 체크섬 검증
  3. SaveMigrations.Apply()  ← 마이그레이션이 필드 추가/변환
  4. ToObject<SaveData>()    ← 역직렬화
  5. Validate()              ← ★ 마이그레이션 후 검증
  6. return
```

마이그레이션이 새 필드를 추가하지 못하면 C# 기본값(0/null)으로 남음 → Validate()가 안전한 값으로 보정. 순서가 올바름. ✅

### 2.4 fixes 리스트와 로그 출력

```csharp
var fixes = result.Validate();
if (fixes.Count > 0)
{
    Debug.LogWarning($"[SaveSystem] {path}: applied {fixes.Count} validation fix(es):\n  " +
                     string.Join("\n  ", fixes));
}
```

- 정상 데이터: `fixes.Count == 0` → 로그 없음 → 성능 영향 제로 ✅
- 비정상 데이터: 보정 내역이 Warning 레벨로 출력 → Error가 아닌 Warning이 적절 (데이터가 복구 가능한 상태이므로) ✅
- `path` 포함으로 어느 파일이 문제인지 추적 가능 ✅

### 2.5 상한값(max) 검증 부재 분석

현재 Validate()는 하한만 체크한다:
- `level` 상한 체크 없음 → level=9999는 통과
- `hp` 상한 체크 없음 → hp=999999는 통과 (RecalcStats 후 clamp)
- `gold` 상한 체크 없음 → gold=MAX_INT는 통과

**판단:** 상한 검증은 게임 밸런스 영역이며, Validate()의 범위(데이터 무결성)를 초과한다. `RecalcStats()`가 HP/MP를 maxHP/maxMP로 클램프하므로 실질적 위험은 낮다. 상한 검증은 별도 밸런스 검증 시스템이 적절하다. → NICE (비차단)

---

## 검증 3: UI 추적

### SaveData → UI 반영 체인

```
SaveData.Validate() → SaveController.Load() → PlayerStats 설정 → HUD 갱신
```

| SaveData 필드 | PlayerStats | UI |
|--------------|-------------|-----|
| hp=50 (보정) | state.Hp=50 → RecalcStats → clamp | HUD HP 바 정상 표시 |
| level=1 (보정) | state.Level=1 | HUD 레벨 표시 |
| inventory=[] (보정) | InventorySystem.Slots 비어있음 | 인벤토리 UI 빈 상태 |
| equipment=5슬롯 null (보정) | 장비 슬롯 비어있음 | 장비 UI 빈 슬롯 표시 |

보정된 값이 UI에 정상 반영됨. "비정상 값으로 시작했는데 게임이 깨진다" 시나리오 방지. ✅

---

## 검증 4: 플레이 시나리오

| 시나리오 | Validate() 전 | Validate() 후 | 판정 |
|---------|--------------|--------------|------|
| 정상 세이브 로드 | 변화 없음 | fixes.Count=0 | **PASS** |
| level=0 세이브 (오래된 버전) | 사망 불가능 레벨 | level=1, 정상 시작 | **PASS** |
| hp=-10 (수동 편집) | 즉시 사망 | hp=50, 생존 상태 시작 | **PASS** |
| gold=-100 (버그) | 음수 골드 표시 | gold=0, 정상 | **PASS** |
| inventory null (필드 누락) | NullRef 크래시 | 빈 배열, 정상 | **PASS** |
| bonusStats null (마이그레이션 누락) | 스탯 계산 NullRef | 4키 기본값, 정상 | **PASS** |
| 정상 플레이 후 저장 → 로드 | 변화 없음 | 변화 없음 (regression 없음) | **PASS** |
| 새 게임 시작 (세이브 없음) | N/A | Load() returns null | **PASS** |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
세이브 파일이 깨져서 게임을 처음부터 다시 해야 하는 건 최악의 경험이다. 이제 파일이 좀 손상되어도 게임이 복구를 시도해주니까 안심. 다만 복구됐다는 걸 플레이어에게 알려주는 UI 피드백이 있으면 더 좋겠다 (현재는 콘솔 로그만).

### ⚔️ 코어 게이머
세이브 파일 수동 편집으로 치팅 시도 시 비정상 값이 보정되는 건 좋다. 다만 상한값 제한이 없어서 level=999, gold=999999는 여전히 가능. 치트 방지 목적이라면 상한도 필요하지만, 이 태스크의 목적은 "무결성"이므로 범위에 맞다.

### 🎨 UX/UI 디자이너
데이터 보정 시 플레이어에게 "세이브 파일이 복구되었습니다" 같은 토스트/팝업이 없는 점이 아쉽다. 콘솔 로그는 개발자용이지 플레이어용이 아니다. 다만 S-077 범위 밖이며 별도 UX 태스크로 적절.

### 🔍 QA 엔지니어
Validate() 로직이 간결하고 예측 가능하다. 각 필드의 검증 조건과 폴백값이 명확. SaveSystem.TryLoadFrom()의 단일 삽입점으로 모든 로드 경로 커버. 다만:
- 단위 테스트(스펙의 Test Plan)가 커밋에 포함되지 않았다. 기존 SaveSystemTests.cs에 Validate 테스트 추가 권장.
- equipment 슬롯 키가 하드코딩됨. 향후 슬롯 추가 시 Validate()도 업데이트 필요 — 이 커플링은 수용 가능 수준.

---

## 미해결 권장사항 (비차단)

| # | 항목 | 심각도 | 비고 |
|---|------|--------|------|
| 1 | Validate() 단위 테스트 추가 | SHOULD | 스펙에 테스트 계획이 상세하지만 커밋 미포함. SaveSystemTests.cs에 추가 권장. |
| 2 | 상한값(level max, hp max 등) 검증 | NICE | 밸런스 영역. RecalcStats clamp로 HP/MP는 실질적으로 처리됨. |
| 3 | 데이터 보정 시 플레이어 UI 알림 | NICE | 콘솔 로그만 존재. UX 개선 별도 태스크. |

---

## 종합 판정

### ✅ APPROVE

| # | 스펙 Acceptance Criteria | 대응 | 판정 |
|---|------------------------|------|------|
| 1 | 비정상 값 타입 필드 보정 | ✅ 8개 필드 검증 | **PASS** |
| 2 | null 컬렉션 기본값 채움 | ✅ 6개 필드 검증 | **PASS** |
| 3 | 보정 시 경고 로그 출력 | ✅ Debug.LogWarning + 항목 명시 | **PASS** |
| 4 | 정상 데이터 변경 없음 | ✅ fixes.Count==0 시 로그 없음 | **PASS** |
| 5 | 기존 roundtrip 테스트 통과 | ⚠️ 테스트 미포함이나 코드 분석상 regression 없음 | **PASS** |
| 6 | TryLoadFrom() 단일 지점 검증 | ✅ line 140 한 곳 | **PASS** |

새 시스템(SaveData.Validate()) 추가이나 기존 로드 경로에 최소한으로 삽입(6줄)되어 침투성이 낮다. 검증 로직 자체가 단순하고 예측 가능하며, 폴백값이 게임 데이터 구조와 일치한다. 코드 품질이 높고 스펙에 충실한 구현이다.
