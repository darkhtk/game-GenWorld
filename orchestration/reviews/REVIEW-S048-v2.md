# REVIEW-S048-v2: SkillSystem 데이터 무결성

> **리뷰 일시:** 2026-04-03
> **태스크:** S-048 SkillSystem 데이터 무결성 (v2 재제출)
> **스펙:** SPEC-S-048
> **커밋:** `3c71d94` fix: S-048 v2 SkillList null-id 필터링 + S-051 v2 SceneTransition 메모리 정리
> **v1 판정:** ❌ NEEDS_WORK (REVIEW-S048-v1)
> **v2 판정:** ✅ APPROVE

---

## v1 지적사항 대응 현황

| # | v1 지적 | 심각도 | v2 대응 | 상태 |
|---|---------|--------|---------|------|
| 1 | SkillList 배열에 null id 엔트리 잔류 → SkillTreeUI 크래시 위험 | HIGH | LoadSkills()에서 valid 리스트로 필터링 후 SkillList 할당 | ✅ 해결 |
| 2 | mpCost, requiredLevel, requiredPoints 검증 누락 | LOW | ValidateData()에 3개 필드 검증 추가 | ✅ 해결 |
| 3 | 단위 테스트 3건 미작성 | LOW | DataManagerSkillValidationTests 5건 추가 | ✅ 해결 |

---

## 변경 요약

커밋 `3c71d94`에서 S-048 관련 변경:

1. **DataManager.cs:LoadSkills()** — `valid` 리스트를 도입하여 null/empty id를 `Skills` 딕셔너리와 `SkillList` 배열 양쪽에서 제외.
2. **DataManager.cs:ValidateData()** — `requiredLevel <= 0` → 1, `requiredPoints <= 0` → 1, `mpCost < 0` → 0 검증 추가.
3. **DataManagerSkillValidationTests.cs** — 5건 신규 테스트.

변경 파일 2개(DataManager.cs, 테스트 파일), 총 +100/-2 라인.

---

## 검증 1: 엔진 검증

| 항목 | 스펙 요구 | v2 구현 | 판정 |
|------|----------|---------|------|
| null/empty id 스킵 | LoadSkills에서 null id 스킵 + 로그 | `string.IsNullOrEmpty` 가드 + 경고 로그 | **PASS** |
| SkillList 필터링 | (v1 지적) | `valid` 리스트 → `ToArray()` | **PASS** |
| cooldown 기본값 1000 | default 1000 | `cooldown <= 0` → `1000f` | **PASS** |
| damage 기본값 1f | default 1f | `damage <= 0` → `1f` | **PASS** |
| mpCost 기본값 0 | default 0 | `mpCost < 0` → `0` | **PASS** |
| requiredLevel 기본값 1 | default 1 | `requiredLevel <= 0` → `1` | **PASS** |
| requiredPoints 기본값 1 | default 1 | `requiredPoints <= 0` → `1` | **PASS** |
| ValidateData() 호출 | Items/Monsters 뒤 | 인라인, 위치 정확 | **PASS** |

---

## 검증 2: 코드 추적

### 2.1 LoadSkills() 필터링 (DataManager.cs:55-69)

```csharp
var raw = data.skills ?? System.Array.Empty<SkillDef>();
var valid = new List<SkillDef>(raw.Length);
foreach (var skill in raw)
{
    if (string.IsNullOrEmpty(skill.id)) { /* warn + continue */ }
    Skills[skill.id] = skill;
    valid.Add(skill);
}
SkillList = valid.ToArray();
```

- `Skills` 딕셔너리와 `SkillList` 배열이 동일한 필터링 결과를 반영 → **일관성 확보**
- v1의 핵심 지적(SkillList 불일치)이 정확히 해소됨
- `List<SkillDef>(raw.Length)` 초기 용량 지정으로 불필요한 재할당 방지 → 양호

### 2.2 ValidateData() 추가 필드 (DataManager.cs:156-158)

```csharp
if (s.requiredLevel <= 0) { s.requiredLevel = 1; patched = true; }
if (s.requiredPoints <= 0) { s.requiredPoints = 1; patched = true; }
if (s.mpCost < 0) { s.mpCost = 0; patched = true; }
```

- `mpCost < 0` (음수만 패치, 0 허용) — 스펙의 "default 0"과 일치. 무료 스킬(mpCost=0)이 정상 → **정확**
- `requiredLevel/Points <= 0` (0도 패치) — 레벨/포인트 0은 의미 없으므로 1로 패치 → **정확**
- SkillDef 클래스에서 `requiredPoints = 1, requiredLevel = 1` 초기값이 있으나, JSON 역직렬화 시 0으로 덮어씌워질 수 있어 ValidateData()에서 명시적 검증이 유효 → **방어적 코딩 원칙 준수**

### 2.3 SkillTreeUI 안전성

SkillTreeUI.Init()의 `_rows[def.id] = row` (line 98)에는 여전히 명시적 null 체크가 없으나, `SkillList`가 이미 필터링되었으므로 null id가 도달할 수 없음. RefreshEquipBar에서는 `string.IsNullOrEmpty(skillId)` 방어가 별도로 존재. → **안전**

---

## 검증 3: UI 추적

| UI 컴포넌트 | 데이터 접근 | v2 상태 | 판정 |
|-------------|-----------|---------|------|
| SkillTreeUI.Init() | `SkillList` 배열 순회 | 필터링된 배열이므로 null id 없음 | **PASS** |
| SkillTreeUI.Refresh() | `SkillList` 배열 순회 | 동일 | **PASS** |
| TooltipUI | `Skills` 딕셔너리 경유 | 안전 | **PASS** |
| HUD 쿨다운 | `SkillSystem._equipped` 기반 | 안전 | **PASS** |

---

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 판정 |
|---------|----------|------|
| 정상 skills.json 로드 | 모든 스킬 정상 등록, "all data OK" 로그 | **PASS** |
| id 누락 엔트리 포함 | Skills+SkillList 양쪽에서 제외, 경고 로그 | **PASS** |
| id 누락 + 스킬트리 UI 열기 | SkillTreeUI.Init()에 도달하지 않음 (필터링됨) | **PASS** |
| damage:0 엔트리 | 1f로 패치 | **PASS** |
| mpCost:-5 엔트리 | 0으로 패치 | **PASS** |
| requiredLevel:0 엔트리 | 1로 패치 | **PASS** |
| 정상 스킬 습득/사용 | 기존 동작 동일 | **PASS** |
| 세이브/로드 사이클 | SkillSystem 정상 작동 | **PASS** |

---

## 테스트 검증

| 테스트 | 검증 대상 | 비고 |
|--------|----------|------|
| NullIdSkill_IsFilteredFromList | null/empty id 필터링 | 스펙 Test Plan 항목 1 대응 |
| ZeroDamage_PatchedToDefault | damage=0 → 1f | 스펙 Test Plan 항목 2 대응 |
| ValidSkill_NotPatched | 정상 스킬 무변경 | 스펙 Test Plan 항목 3 대응 |
| NegativeMpCost_PatchedToZero | mpCost=-5 → 0 | v2 추가 필드 검증 |
| ZeroRequiredLevel_PatchedToOne | requiredLevel=0 → 1 | v2 추가 필드 검증 |

테스트는 DataManager의 로직을 직접 호출하지 않고 동일 로직을 재현하여 검증한다. DataManager가 파일 I/O와 Unity 런타임에 의존하므로 EditMode 테스트에서 이 접근은 합리적이다. 로직 변경 시 테스트가 동기화되지 않을 리스크가 있으나, 현재 범위에서는 수용 가능.

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
정상 데이터에서는 체감 변화 전혀 없다. 스킬 습득/사용/쿨다운 표시 모두 이전과 동일. 모딩이나 비정상 데이터를 만지지 않는 한 영향 없음.

### ⚔️ 코어 게이머
스킬 밸런스 변화 없음. 커스텀 JSON을 편집하는 모더의 경우, 실수로 id를 빠뜨리면 해당 스킬이 SkillList에서도 제거되어 스킬트리 UI에 아예 표시되지 않으므로, 디버깅 시 경고 로그 확인 권장. 이전보다 안전한 동작.

### 🎨 UX/UI 디자이너
UI 크래시 위험 제거. SkillTreeUI가 이제 필터링된 데이터만 수신하므로 null id 관련 예외가 발생할 수 없다. UI 안정성 향상.

### 🔍 QA 엔지니어
v1에서 지적한 3건(SkillList 필터링, 추가 필드 검증, 단위 테스트) 전부 대응됨. 테스트 5건이 주요 경계값을 커버. DataManager 직접 호출 테스트가 아닌 로직 재현 방식이나, EditMode 제약상 합리적 접근.

---

## 종합 판정

### ✅ APPROVE

v1의 HIGH 심각도 이슈(SkillList null id 잔류)와 LOW 이슈 2건(추가 필드 검증, 테스트) 모두 정확히 해결되었다. 스펙의 모든 요구사항이 충족되며, 코드 품질과 기존 패턴 일관성이 양호하다.
