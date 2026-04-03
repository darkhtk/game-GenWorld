# REVIEW-S048-v1: SkillSystem 데이터 무결성

> **리뷰 일시:** 2026-04-03
> **태스크:** S-048 SkillSystem 데이터 무결성
> **스펙:** SPEC-S-048
> **커밋:** `dac1753` fix: S-048 SkillSystem 데이터 무결성 — ValidateSkills + null id 스킵 방어 로딩
> **판정:** ❌ NEEDS_WORK

---

## 변경 요약

`DataManager.cs` 1개 파일, +23/-2 줄 변경.

1. **LoadSkills()** — `foreach` 루프에 `string.IsNullOrEmpty(skill.id)` 가드 추가. null/empty id인 스킬은 `Skills` 딕셔너리에 등록하지 않고 경고 로그 출력 후 `continue`.
2. **ValidateData()** — 기존 Items/Monsters 검증 블록 뒤에 Skills 검증 블록 추가. `name` 누락 시 `id`로 패치, `cooldown <= 0` 시 1000f, `damage <= 0` 시 1f로 패치. 패치된 건수를 경고 로그로 출력.
3. 종합 검증 결과 로그의 조건식에 `skillIssues` 추가.

---

## 검증 1: 엔진 검증

| 항목 | 스펙 요구 | 구현 | 판정 |
|------|----------|------|------|
| ValidateSkills() 메서드 분리 | "Add ValidateSkills() in DataManager" | 별도 메서드 대신 ValidateData() 내부에 인라인 | **주의** (기능 동일, 구조 차이) |
| null/empty id 스킵 | LoadSkills에서 null id 스킵 + 로그 | `string.IsNullOrEmpty` + `Debug.LogWarning` | **PASS** |
| cooldown 기본값 | default 1000 (ms) | `cooldown <= 0` -> `1000f` | **PASS** |
| damage 기본값 | default 1f | `damage <= 0` -> `1f` | **PASS** |
| name 기본값 | (미명시) | `name` 누락 시 `s.id`로 패치 | **PASS** (보너스) |
| mpCost 기본값 | default 0 | **미구현** | **MISS** |
| requiredLevel 기본값 | default 1 | **미구현** | **MISS** |
| requiredPoints 기본값 | default 1 | **미구현** | **MISS** |
| ValidateData()에서 호출 | Items/Monsters 뒤에 호출 | 인라인이나 위치는 정확 | **PASS** |

---

## 검증 2: 코드 추적

### 2.1 LoadSkills() null id 방어 (DataManager.cs:58-66)

```
foreach (var skill in SkillList)
{
    if (string.IsNullOrEmpty(skill.id))
    {
        Debug.LogWarning("[DataManager] Skipping skill with null/empty id");
        continue;
    }
    Skills[skill.id] = skill;
}
```

**분석:**
- `Skills` 딕셔너리에는 null id 엔트리가 들어가지 않음 -> **정상**
- 그러나 `SkillList` 배열에는 null id 엔트리가 그대로 남아 있음 -> **문제**

### 2.2 SkillList 잔류 문제 (발견된 이슈)

`SkillList`는 `data.skills` 배열 전체를 그대로 보유한다. null id 스킬은 `Skills` 딕셔너리에서만 제외되고 `SkillList`에는 잔류한다.

**영향 경로:**
- `SkillTreeUI.Init()` (line 71-98): `foreach (var def in skillDefs)` -> `_rows[def.id] = row` (line 98)
  - `def.id`가 null이면 `Dictionary<string, SkillRowUI>`에 null 키 삽입 -> **ArgumentNullException**
- `SkillTreeUI.Refresh()` (line 132-142): `foreach (var def in skillDefs)` -> `_rows.TryGetValue(def.id, ...)`
  - `def.id`가 null이면 **ArgumentNullException**

이 경로는 현재 `skills.json`에 null id 엔트리가 없으므로 즉시 발현하지 않지만, 방어적 로딩의 취지에 어긋난다. `SkillList`도 필터링하거나, UI 쪽에서 null id를 건너뛰어야 한다.

### 2.3 ValidateData() 스킬 검증 (DataManager.cs:147-155)

```
int skillIssues = 0;
foreach (var s in Skills.Values)
{
    bool patched = false;
    if (string.IsNullOrEmpty(s.name)) { s.name = s.id; patched = true; }
    if (s.cooldown <= 0) { s.cooldown = 1000f; patched = true; }
    if (s.damage <= 0) { s.damage = 1f; patched = true; }
    if (patched) skillIssues++;
}
```

**분석:**
- Items/Monsters 검증과 동일한 패턴 사용 -> **일관성 양호**
- 스펙에서 요구한 `mpCost`, `requiredLevel`, `requiredPoints` 기본값 검증 누락
  - `mpCost`는 SkillDef에서 `int mpCost`로 선언되어 기본값 0 -> JSON에서 누락 시 0이 되므로 큰 문제는 아님
  - `requiredLevel`, `requiredPoints`는 기본값 1로 SkillDef 필드 초기화됨 -> 역시 실질적 문제 적음
  - 그러나 스펙 준수 관점에서는 명시적 검증이 있어야 함

### 2.4 SkillSystem 생성자 (SkillSystem.cs:14-19)

```
public SkillSystem(Dictionary<string, SkillDef> defs)
{
    _defs = defs;
    ...
}
```

- `SkillSystem`은 이미 필터링된 `Data.Skills` 딕셔너리를 받으므로 null id 문제 없음
- 스펙의 "SkillSystem LoadSkills should skip entries with null id" 요구는 `DataManager.LoadSkills()`에서 구현됨. SkillSystem 자체에는 변경 없으나, 실질적으로 같은 효과 -> **PASS**

---

## 검증 3: UI 추적

| UI 컴포넌트 | 스킬 데이터 접근 방식 | null id 영향 | 판정 |
|-------------|---------------------|-------------|------|
| SkillTreeUI | `gm.Data.SkillList` (배열) 순회 | null id 잔류 시 NullRef 위험 | **WARN** |
| TooltipUI | `SkillDef` 참조 표시 | `Skills` 딕셔너리 경유이므로 안전 | PASS |
| HUD 쿨다운 | `SkillSystem.GetCooldowns()` | `_equipped` 기반이므로 안전 | PASS |

**핵심 이슈:** `SkillTreeUI.Init()`과 `SkillTreeUI.Refresh()`는 `SkillList` 배열을 직접 순회하므로, null id 엔트리가 존재하면 예외가 발생한다.

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 검증 |
|---------|----------|------|
| 정상 skills.json 로드 | 모든 스킬 정상 등록, 검증 로그 "all data OK" | PASS |
| skills.json에 id 누락 엔트리 포함 | Skills 딕셔너리에서 제외, 경고 로그 출력 | PASS |
| skills.json에 id 누락 + 스킬트리 UI 열기 | SkillTreeUI.Init()에서 null key 예외 가능 | **FAIL** |
| skills.json에 damage:0 엔트리 포함 | damage=1f로 자동 패치 | PASS |
| skills.json에 cooldown 누락 엔트리 포함 | cooldown=1000f로 자동 패치 | PASS |
| 정상 스킬 습득 및 사용 | 기존 동작과 동일 | PASS |
| 세이브/로드 사이클 | SkillSystem.RestoreLearnedSkills 정상 작동 | PASS |
| 스킬 리셋 후 재습득 | 기존 동작과 동일 | PASS |

---

## 페르소나 리뷰

### 캐주얼 게이머
- 정상 데이터에서는 체감 변화 전혀 없음. 스킬 습득, 사용, 쿨다운 표시 모두 이전과 동일.
- 잘못된 모드 데이터를 사용하지 않는 한 영향 없음.

### 코어 게이머
- 스킬 밸런스에 영향 없음. damage/cooldown 기본값 패치는 원래 0이나 음수인 비정상 엔트리에만 적용됨.
- 커스텀 스킬 JSON을 편집하는 모더의 경우, 실수로 id를 빠뜨리면 해당 스킬이 조용히 무시됨. 경고 로그가 콘솔에 출력되므로 디버깅 가능.

### UX/UI 디자이너
- UI 변경 없음. 다만 SkillList에 null id 잔류 시 SkillTreeUI 크래시 위험이 잔존하므로, UI 안정성 관점에서 추가 방어 필요.

### QA 엔지니어
- **테스트 커버리지 부족.** 스펙에서 요구한 3가지 단위 테스트(null id 스킵, damage null 패치, 정상 로드)가 추가되지 않았음. 기존 `SkillSystemTests.cs`는 SkillSystem 로직만 테스트하며 DataManager 검증 로직은 미검증.
- `SkillList` 배열과 `Skills` 딕셔너리 간 불일치 상태가 테스트로 커버되지 않음.

---

## 종합 판정

### ❌ NEEDS_WORK

**구현 품질:** 핵심 방어 로직(null id 스킵, numeric 기본값 패치)은 정상 동작하며, 기존 코드 패턴과 일관성을 유지한다. 정상적인 `skills.json`에서는 문제가 발생하지 않는다.

**수정 필요 사항:**

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | **HIGH** | `SkillList` 배열에 null id 엔트리 잔류. `SkillTreeUI.Init()`/`Refresh()`에서 `def.id`가 null일 때 `ArgumentNullException` 발생 가능. `LoadSkills()`에서 `SkillList`도 필터링하거나, 최소한 `SkillTreeUI`에서 null id 스킵 가드 추가 필요. |
| 2 | **LOW** | 스펙에서 요구한 `mpCost`, `requiredLevel`, `requiredPoints` 검증 누락. SkillDef 필드 초기화 값으로 대체되므로 실질적 위험은 낮으나, 명시적 검증이 방어적 코딩 원칙에 부합함. |
| 3 | **LOW** | 스펙에서 요구한 단위 테스트 3건 미작성. DataManager의 검증 로직은 통합 테스트로만 확인 가능한 상태. |

**권장 수정:**
```
// LoadSkills()에서 SkillList도 필터링
SkillList = SkillList.Where(s => !string.IsNullOrEmpty(s.id)).ToArray();
```
또는
```
// SkillTreeUI.Init()에서 null id 방어
foreach (var def in skillDefs)
{
    if (string.IsNullOrEmpty(def.id)) continue;
    ...
}
```
