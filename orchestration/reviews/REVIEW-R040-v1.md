# REVIEW-R040-v1: SteamPipe (Steam)

> **리뷰 일시:** 2026-04-03
> **태스크:** R-040 SteamPipe
> **스펙:** SPEC-R-040
> **판정:** ❌ NEEDS_WORK

---

## 수용 기준별 검증

| # | 수용 기준 | 결과 | 비고 |
|---|---------|------|------|
| 1 | `BuildScript.BuildWindows()`로 Win64 빌드 생성 | ❌ | BuildScript.cs 파일 자체가 존재하지 않음 |
| 2 | `app_build.vdf`, `depot_build_win.vdf` 올바른 ID 참조 | ❌ | app_build.vdf의 AppID="0", DepotID="0" (플레이스홀더). depot_build_win.vdf 미존재 |
| 3 | `upload.sh` 실행 시 SteamCMD 정상 업로드 | ⚠️ | 스크립트 존재하나, 빌드 경로가 `../Build/Windows`로 하드코딩. AppID=0으로는 실제 업로드 불가 |
| 4 | `steam_appid.txt` 빌드 출력에 미포함 | ⚠️ | 프로젝트 루트에 480으로 존재. VDF FileExclusion에 포함되지 않음. Unity 빌드가 자동 복사할 수 있음 |
| 5 | 디버그 심볼/에디터 전용 에셋 제외 | ✅ | VDF에 `*.pdb` 제외 설정 있음 |
| 6 | 빌드 버전 번호 자동 반영 | ❌ | BuildScript 없어 PlayerSettings.bundleVersion 연동 불가 |

---

## 페르소나별 리뷰

### 🎮 캐주얼 게이머
빌드 파이프라인은 최종 사용자에게 직접 영향은 없으나, 자동화 없이 수동 빌드하면 실수로 디버그 빌드가 배포될 수 있다.

### ⚔️ 코어 게이머
스팀 업데이트가 빠르고 안정적으로 나오려면 자동 빌드 파이프라인이 필수. 현재 상태로는 수동 작업이 많아 배포 지연 우려.

### 🎨 UX/UI 디자이너
해당 없음 (인프라 태스크).

### 🔍 QA 엔지니어
AppID=0인 VDF는 실행 자체가 불가. steam_appid.txt(480)가 빌드에 포함되면 Steam API 초기화에 문제 발생 가능. BuildScript 없이는 일관된 릴리즈 빌드 보장 불가.

---

## 검증 체계

### 검증 1: 엔진 검증
- ❌ BuildScript.cs 미존재 → Unity 메뉴 빌드 불가
- ⚠️ 빌드 출력 경로 미검증 (Build/Windows/ 디렉토리 구조 미확인)

### 검증 2: 코드 추적
- ❌ SPEC 핵심 구현체(BuildScript) 없음
- ⚠️ app_build.vdf ID 값 전부 플레이스홀더
- ❌ depot_build_win.vdf 미존재

### 검증 3: UI 추적
- N/A (에디터 메뉴 / CLI)

### 검증 4: 플레이 시나리오
- N/A

---

## 최종 판정: **❌ NEEDS_WORK**

### 필수 수정사항
1. `Assets/Scripts/Editor/BuildScript.cs` 생성 — `BuildWindows()`, `BuildAndUpload()` 구현
2. `app_build.vdf`에 실제 AppID, DepotID 설정 (또는 설정 방법 문서화)
3. `depot_build_win.vdf` 생성
4. `steam_appid.txt` VDF FileExclusion에 추가
5. 빌드 버전 번호 자동 반영 로직
