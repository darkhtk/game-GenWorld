# SPEC-S-054: AutoSave 전투 중 저장 방지

> **우선순위:** P2
> **태그:** 🔧 안정성
> **방향:** stabilize — 데이터 일관성

---

## 개요

전투 중 자동 저장이 실행되면 불완전한 전투 상태(중간 HP, 미처리 보상 등)가 저장될 수 있다. 전투 상태에서 자동 저장을 스킵하고, 전투 종료 직후 보류된 저장을 실행한다.

## 현재 동작

- **GameUIWiring.cs** line 241-250: `RegionVisitEvent` 발생 시 30초 쿨다운 체크 후 `SaveEvent` 발행
- **SaveSystem.cs**: 동기 실행, 별도 전투 상태 체크 없음
- 전투 중 리전 전환 시 자동 저장 실행 가능

## 수정 범위

### 1. 전투 상태 감지

**파일:** `Assets/Scripts/Systems/CombatManager.cs`
- `IsInCombat` 프로퍼티 추가 (또는 기존 존재 시 활용)
- 조건: `_cachedMonsters`에 어그로 상태 몬스터 존재 시 true

### 2. 자동 저장 가드

**파일:** `Assets/Scripts/UI/GameUIWiring.cs` (line ~246)
- RegionVisitEvent 핸들러에서 `CombatManager.IsInCombat` 체크
- true 시 `_pendingSave = true` 플래그 설정, 저장 스킵
- 전투 종료 이벤트 수신 시 `_pendingSave` 확인 → 저장 실행

### 3. 전투 종료 후 지연 저장

- `CombatManager`에서 마지막 어그로 몬스터 처리 후 이벤트 발행 (또는 기존 이벤트 활용)
- GameUIWiring에서 해당 이벤트 수신 → `_pendingSave` true이면 저장

## 호출 진입점

- **트리거:** `RegionVisitEvent` (리전 전환 시 자동 발생)
- **UI 개입 없음** — 시스템 내부 로직

## 데이터 구조

추가 필드 없음. 기존 SaveData 구조 유지.

## 세이브 연동

기존 SaveSystem.Save() 호출 시점만 지연. 저장 내용 변경 없음.

## 검증 항목

1. 전투 중 리전 전환 → 자동 저장 스킵 확인
2. 전투 종료 후 보류 저장 실행 확인
3. 비전투 시 기존 동작(30초 쿨다운) 유지 확인
4. 수동 저장(설정 메뉴)은 전투 가드 미적용 (선택사항)
