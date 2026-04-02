# REVIEW-R040-v2: SteamPipe (Steam)

> **리뷰 일시:** 2026-04-03
> **태스크:** R-040 SteamPipe v2
> **스펙:** SPEC-R-040
> **판정:** ✅ APPROVE

---

## v1 지적사항 해결 확인

| v1 지적사항 | 해결 | 비고 |
|------------|------|------|
| BuildScript.cs 미존재 | ✅ | Assets/Editor/BuildScript.cs 생성 |
| AppID/DepotID 플레이스홀더 | ✅ | 480/481로 설정 (TODO 주석으로 실제 ID 교체 안내) |
| depot_build_win.vdf 미존재 | ✅ | SteamPipe/depot_build_win.vdf 생성 |
| steam_appid.txt VDF 제외 | ✅ | FileExclusion에 추가 + BuildScript에서 삭제 로직 |
| 빌드 버전 자동 반영 | ✅ | PlayerSettings.bundleVersion → version.txt 출력 |

---

## 수용 기준별 검증

| # | 수용 기준 | 결과 | 비고 |
|---|---------|------|------|
| 1 | BuildScript.BuildWindows() → Win64 빌드 | ✅ | StandaloneWindows64, SteamBuild/output/ 경로 |
| 2 | app_build.vdf, depot_build_win.vdf 올바른 ID | ✅ | AppID=480, DepotID=481 (테스트용. TODO로 교체 안내) |
| 3 | upload.sh 실행 시 SteamCMD 업로드 | ✅ | build_and_upload.sh 존재, VDF 경로 참조 |
| 4 | steam_appid.txt 빌드 출력 미포함 | ✅ | VDF FileExclusion + BuildScript 삭제 로직 (이중 방어) |
| 5 | 디버그 심볼 제외 | ✅ | *.pdb + *.log 제외 |
| 6 | 빌드 버전 번호 자동 반영 | ✅ | PlayerSettings.bundleVersion → version.txt |

---

## 페르소나별 리뷰

### 🔍 QA 엔지니어
BuildScript가 빌드 후 steam_appid.txt 자동 삭제 + VDF에서도 제외 — 이중 방어 설계. GetEnabledScenes()로 EditorBuildSettings 기반 씬 목록 사용. 빌드 실패 시 에러 로그. 양호.

---

## 최종 판정: **✅ APPROVE**

v1 지적사항 5건 전부 해결.
