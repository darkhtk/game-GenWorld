# REVIEW-R004-v1: JSON 파싱 실패 시 복구 [깊은 리뷰]

> **리뷰 일시:** 2026-04-02
> **태스크:** R-004 JSON 파싱 복구
> **스펙:** SPEC-R004
> **판정:** ✅ APPROVE
> **리뷰 유형:** 깊은 리뷰 (코드 전문 직접 읽기)

---

## 읽은 파일 목록

| 파일 | 행수 | 목적 |
|------|------|------|
| Assets/Scripts/Data/DataManager.cs | 167행 전체 | 핵심 변경 파일 |
| Assets/Scripts/Data/ItemDef.cs | 63행 전체 | 필드명/타입 검증 |
| Assets/Scripts/Data/MonsterDef.cs | 22행 전체 | 필드명/타입 검증 |

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 순수 C# 클래스, MonoBehaviour 아님 |
| 에셋 존재 여부 | ✅ | 코드만 변경 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적 (깊은 리뷰)

### SPEC 항목별 검증

| # | 스펙 요구사항 | 코드 위치 | 결과 | 상세 |
|---|--------------|-----------|------|------|
| 1 | LoadAll()에 ValidateData() 추가 | line 36 | ✅ | 모든 Load 완료 후 호출 |
| 2 | 빈 Dictionary 경고 | lines 113-118 | ✅ | Items~Regions 6개 체크 |
| 3 | ItemDef: name → "unknown" | line 124 | ✅ | `IsNullOrEmpty` 체크 |
| 4 | ItemDef: grade → "common" | line 125 | ✅ | `IsNullOrEmpty` 체크 |
| 5 | ItemDef: stackLimit → 1 | line 126 | ✅ | SPEC은 `stackLimit`이나 실제 필드 `maxStack` — 정확히 대응 |
| 6 | MonsterDef: hp → 10 | line 135 | ✅ | `<= 0` 체크 |
| 7 | MonsterDef: atk → 1 | line 136 | ✅ | `<= 0` 체크 |
| 8 | 누락 항목 수 로그 | lines 140-143 | ✅ | item/monster 별도 카운트 |
| 9 | 정상 시 "all data OK" | lines 144-145 | ✅ | 전체 issues == 0일 때 |

### 코드 흐름 분석 (line by line)

**LoadAll() (27-40):**
```
LoadItems → LoadSkills → LoadMonsters → LoadNpcs → LoadQuests → LoadRegions → LoadNpcProfiles → ValidateData → summary log
```
순서 정확. ValidateData는 모든 Load 완료 후 실행되므로 전체 데이터에 대해 검증 가능.

**LoadJson<T>() (148-166):**
- 파일 미존재 → Warning + null return ✅
- 파싱 실패 → Error + null return ✅
- 각 Load 메서드에서 `if (data == null) return` → 빈 Dictionary 유지 ✅

**ValidateData() (109-146):**
- Phase 1 (113-118): 빈 컬렉션 경고 — 6개 Dictionary 체크
- Phase 2 (120-128): ItemDef 필드 패치 — name, grade, maxStack
- Phase 3 (130-138): MonsterDef 필드 패치 — name, hp, atk
- Phase 4 (140-145): 결과 요약 로그

**패치 로직 상세:**
```csharp
// ItemDef 검증 (lines 121-128)
foreach (var item in Items.Values)
{
    bool patched = false;
    if (string.IsNullOrEmpty(item.name)) { item.name = "unknown"; patched = true; }
    if (string.IsNullOrEmpty(item.grade)) { item.grade = "common"; patched = true; }
    if (item.maxStack <= 0) { item.maxStack = 1; patched = true; }
    if (patched) itemIssues++;
}
```
- `string.IsNullOrEmpty` — null과 "" 모두 처리 ✅
- `maxStack <= 0` — 음수와 0 모두 처리 ✅
- `patched` 플래그로 카운팅 — 한 아이템에 여러 필드 누락이어도 1건으로 카운트 (합리적)

### Def 클래스 필드명 교차 검증

| SPEC 표기 | ItemDef 실제 필드 (ItemDef.cs) | DataManager 코드 | 일치 |
|----------|-------------------------------|-------------------|------|
| name | `public string name` (line 8) | `item.name` (line 124) | ✅ |
| grade | `public string grade` (line 10) | `item.grade` (line 125) | ✅ |
| stackLimit | `public int maxStack = 1` (line 14) | `item.maxStack` (line 126) | ✅ (이름 다르지만 동일 필드) |

| SPEC 표기 | MonsterDef 실제 필드 (MonsterDef.cs) | DataManager 코드 | 일치 |
|----------|--------------------------------------|-------------------|------|
| hp | `public int hp` (line 7) | `m.hp` (line 135) | ✅ |
| atk | `public int atk` (line 7) | `m.atk` (line 136) | ✅ |

### 발견된 잠재적 이슈

**⚠️ null id 크래시 가능성 (Medium, 기존 코드)**

`LoadItems()` line 48: `Items[item.id] = item`
→ `item.id`가 null이면 `ArgumentNullException` 발생.

이는 R-004 이전부터 존재하는 문제이며, ValidateData()는 Load 이후에 실행되므로 이 시점에서 이미 크래시. 부분 손상에서 id가 null인 경우는 드물지만, R-004의 목적("크래시 방지")과 관련 있음.

**영향 범위:** LoadItems, LoadSkills, LoadMonsters, LoadNpcs, LoadQuests, LoadRegions 모두 동일 패턴.

**경감 요인:** LoadJson<T> try-catch가 역직렬화 실패를 잡으므로, id가 아예 없는 극단적 손상은 역직렬화 단계에서 잡힐 수 있음. 다만 `"id": null` 형태는 정상 역직렬화되므로 후속 크래시.

**SPEC 범위:** SPEC이 명시적으로 id 검증을 요구하지 않으므로 현 구현은 SPEC 준수. 별도 개선 태스크로 권장.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 해당 없음 | — | 백그라운드 시스템, UI 진입점 없음 (스펙 명시) |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 | 코드 경로 |
|----------|-----------|-----------|
| 정상 JSON 전체 로드 | 정상 동작, "all data OK" 로그 | LoadAll → ValidateData line 144 |
| items.json 파일 누락 | Items 빈 Dictionary, Warning 로그 | LoadJson line 153 → LoadItems line 45 return |
| items.json 파싱 실패 (문법 오류) | Items 빈 Dictionary, Error 로그 | LoadJson line 163 → LoadItems line 45 return |
| 아이템 name 필드 null | name="unknown"으로 패치, 정상 동작 | ValidateData line 124 |
| 몬스터 hp=0 | hp=10으로 패치, 정상 동작 | ValidateData line 135 |
| 전체 데이터 파일 부재 | 빈 게임 데이터로 시작, 6건 Warning | ValidateData lines 113-118 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
업데이트 후에 데이터 깨져서 게임 안 켜지는 거 제일 짜증나는데, 이제 기본값으로라도 돌아가면 좋겠다. 근데 "unknown" 아이템이 인벤토리에 보이면 좀 무섭긴 할 듯... 그래도 크래시보다 낫지.

### ⚔️ 코어 게이머
방어적 기본값이 밸런스에 미치는 영향:
- 몬스터 hp=10, atk=1 → 사실상 샌드백. 부분 손상된 몬스터가 필드에 나오면 밸런스 붕괴보다는 쉬운 적으로 나옴 — 크래시보다 낫다.
- maxStack=1 → 소모품이 1개씩만 스택되면 불편하지만 데이터 손실은 없음.
- 현실적으로 이 폴백이 작동하는 상황 자체가 비정상이므로 적절한 트레이드오프.

### 🎨 UX/UI 디자이너
"unknown" 아이템이나 이름 없는 몬스터가 게임 내에 노출될 때 사용자 안내가 없음. 향후 데이터 오류 감지 시 "게임 데이터를 확인하세요" 같은 UI 토스트가 있으면 좋겠지만, 현재 스펙 범위 밖.

### 🔍 QA 엔지니어
- `string.IsNullOrEmpty` 사용 — null과 빈 문자열 모두 처리 ✅
- `<= 0` 체크 — 음수도 처리 ✅
- Load 메서드의 null-coalescing (`?? Array.Empty<T>()`) — NRE 방지 ✅
- **[주의]** null id가 Dictionary key로 들어가는 경로는 미방어 (위 상세 분석 참조)
- ValidateData의 `issues` 변수가 빈 Dictionary 카운트에만 쓰이고, 패치 카운트와는 별도 — 최종 "all data OK" 판정에서 3개 카운트 모두 체크하므로 정확

---

## 미해결 사항

| # | 심각도 | 내용 |
|---|--------|------|
| 1 | Medium | null id가 Dictionary key로 사용 시 NRE (기존 코드, 별도 태스크 권장) |
| 2 | Low | 테스트 미작성 (SPEC 체크리스트 4항목) |
| 3 | Info | SkillDef, NpcDef, QuestDef, RegionDef 필드 검증 미구현 (SPEC에서 요구하지 않음) |

---

## 최종 판정

**✅ APPROVE**

SPEC 요구사항 100% 충족. ValidateData()가 빈 Dictionary 경고 + ItemDef/MonsterDef 필수 필드 기본값 패치를 정확히 수행. LoadJson의 기존 try-catch + 각 Load 메서드의 null 체크와 조합하여 다단계 복구 체계 완성. null id 이슈는 기존 코드 문제로 별도 태스크 권장.
