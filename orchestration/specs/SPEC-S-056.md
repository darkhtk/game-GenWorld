# SPEC-S-056: GameManager 초기화 순서 보장

> **우선순위:** P2
> **태그:** 🔧 안정성
> **방향:** stabilize — 경합 조건 방지

---

## 개요

GameManager가 분할 리팩토링(S-006) 이후 4개 클래스로 분리되었다. Awake/Start 호출 순서가 보장되지 않으면 의존 시스템 간 초기화 경합이 발생할 수 있다. 초기화 순서를 명시적으로 관리한다.

## 현재 동작

- **GameManager.cs** (~308줄): Core 시스템 초기화 담당
- 분리된 클래스: `GameUIWiring`, `CombatRewardHandler`, `SaveController`, `GameInitializer` (확인 필요)
- Unity Awake/Start 호출 순서: 동일 프레임 내 비결정적

## 수정 범위

### 1. 초기화 순서 감사

**파일:** `Assets/Scripts/Core/GameManager.cs` 및 분리 클래스들
- Awake에서 초기화하는 시스템 목록 정리
- Start에서 초기화하는 시스템 목록 정리
- 의존 그래프 작성: A가 B에 의존 → A.Init()은 B.Init() 후에 호출

### 2. 명시적 초기화 체인

선택지:
- **A) Script Execution Order** — Project Settings에서 설정 (간단하지만 취약)
- **B) 단일 진입점** — GameManager.Awake()에서 모든 하위 시스템을 순서대로 Init() 호출 (권장)

### 3. null 방어 보강

- 초기화 미완료 상태에서 접근 시 `Debug.LogWarning` + 안전 기본값 반환
- 이미 개별 시스템에 null 체크 존재 시 추가 불필요

## 호출 진입점

- **트리거:** 씬 로드 → GameManager.Awake()
- **UI 개입 없음** — 시스템 부트스트랩

## 데이터 구조

변경 없음.

## 세이브 연동

SaveSystem 초기화 순서 확인 — Load()가 DataManager 초기화 이후에 호출되는지 보장.

## 검증 항목

1. 콜드 스타트: 씬 로드 → 모든 시스템 정상 초기화 (null 에러 0건)
2. 씬 전환 후 재초기화 시 경합 없음
3. 기존 동작 변경 없음 (회귀 테스트)
