# REVIEW-S011-v1: DataManager 로드 실패 폴백

> **리뷰 일시:** 2026-04-03
> **태스크:** S-011 DataManager 로드 실패 폴백
> **스펙:** 없음
> **판정:** ✅ APPROVE

---

## 검증 1: 엔진 검증

| 항목 | 결과 | 비고 |
|------|------|------|
| 씬/레벨 구조 | ✅ | 변경 없음 |
| 컴포넌트/노드 참조 | ✅ | 변경 없음 |
| 에셋 존재 여부 | ✅ | 신규 에셋 없음 |
| 빌드 세팅 | ✅ | 변경 없음 |

## 검증 2: 코드 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| TASK 명세 부합 | ✅ | Array.Empty 기본값 초기화 — 모든 배열 프로퍼티에 적용 |
| 기존 코드 호환 | ✅ | 외부 타입/시그니처 변경 없음 |
| 아키텍처 패턴 | ✅ | null-safe 기본값 패턴 — .NET 표준 |
| 테스트 커버리지 | ⚠️ | 미작성 |

### 변경 분석

**프로퍼티 초기화 (DataManager.cs:16-23):**
```csharp
public RecipeDef[] Recipes { get; private set; } = Array.Empty<RecipeDef>();
public ItemDef[] ItemList { get; private set; } = Array.Empty<ItemDef>();
public SkillDef[] SkillList { get; private set; } = Array.Empty<SkillDef>();
public MonsterDef[] MonsterList { get; private set; } = Array.Empty<MonsterDef>();
public NpcDef[] NpcList { get; private set; } = Array.Empty<NpcDef>();
public QuestDef[] QuestList { get; private set; } = Array.Empty<QuestDef>();
public RegionDef[] RegionList { get; private set; } = Array.Empty<RegionDef>();
```

**Load 메서드 null coalescing (lines 46, 50, 57, 66, 75, 84, 93):**
```csharp
ItemList = data.items ?? System.Array.Empty<ItemDef>();
Recipes = data.recipes ?? System.Array.Empty<RecipeDef>();
// ... 동일 패턴 반복
```

**방어 체인:**
1. `LoadJson<T>()` 실패 → `null` 반환
2. `if (data == null) return` → 조기 반환
3. 프로퍼티는 이미 `Array.Empty` 기본값 → null 아님
4. JSON 파싱 성공하되 내부 배열 null → `?? Array.Empty` 폴백

모든 경우에 배열 프로퍼티가 null이 되지 않음.

## 검증 3: UI 추적

| 항목 | 결과 | 비고 |
|------|------|------|
| 데이터 바인딩 | ✅ | UI가 ItemList, SkillList 등을 foreach로 순회 — 빈 배열은 안전 |

## 검증 4: 플레이 시나리오

| 시나리오 | 예상 결과 |
|----------|-----------|
| JSON 파일 정상 | 기존과 동일 |
| items.json 누락 | LogWarning + Items=empty dict, ItemList=empty array → 게임 시작은 가능 |
| JSON 파싱 에러 | LogError + 해당 데이터 빈 상태로 진행 |
| JSON 내부 배열 null | ?? Array.Empty 폴백 → NRE 없이 빈 데이터 처리 |

---

## 페르소나 리뷰

### 🎮 캐주얼 게이머
데이터 로드 실패하면 게임이 아예 안 켜지는 것보다는 빈 세계라도 들어가서 "뭔가 잘못됐다"는 걸 아는 게 낫다. 사용자 입장에서 큰 차이.

### ⚔️ 코어 게이머
`Array.Empty<T>()`는 .NET에서 타입별 싱글턴 빈 배열을 반환 — `new T[0]` 대비 GC 할당 없음. 7개 배열 프로퍼티 전부에 적용하여 일관성 확보. Dictionary 프로퍼티는 이미 `new()`로 초기화되어 있어 함께 안전.

### 🎨 UX/UI 디자이너
로드 실패 시 빈 리스트 → UI에 아이템/스킬/몬스터가 없는 상태로 표시. 에러 메시지가 사용자에게 표시되진 않으나 LogWarning이 있으므로 개발 중 디버깅 가능.

### 🔍 QA 엔지니어

| 체크 | 결과 |
|------|------|
| null 배열 방지 | ✅ 초기값 + 로드 시 폴백 이중 방어 |
| 에러 로깅 | ✅ LogWarning(파일 누락) + LogError(파싱 실패) |
| 검증 로직 | ✅ ValidateData()가 빈 데이터를 LogWarning |
| 할당 최적화 | ✅ Array.Empty 싱글턴 사용 |

---

## 종합 판정

| 항목 | 판정 |
|------|------|
| 기능 완성도 | ✅ 모든 배열 프로퍼티에 null-safe 기본값 적용 |
| 기존 호환성 | ✅ 정상 데이터 경로 변경 없음 |
| 코드 품질 | ✅ Array.Empty 표준 패턴, 일관된 적용 |

**결론:** ✅ **APPROVE** — DataManager의 모든 배열 프로퍼티에 `Array.Empty<T>()` 기본값을 적용하여 JSON 로드 실패 시에도 null 참조를 방지. 이중 방어(초기값 + 로드 시 폴백)로 견고한 구현.
