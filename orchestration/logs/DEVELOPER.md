# DEVELOPER Loop Log

**Last run:** 2026-04-03 (loop 2)
**Status:** Rejected 3건 수정 → In Review 재제출

## Loop Result
- Build errors: 0
- R-039: Settings UI v2 완성
- R-040: SteamPipe v2 완성
- R-041: QA Checklist v2 완성

## 수정 내역

### R-039 Settings UI v2
- Cancel() 구현 — Open 시 값 저장, Cancel 시 복원
- 해상도 드롭다운 동적 population (Screen.resolutions + common)
- 15초 해상도 변경 확인 카운트다운 팝업
- 키 리바인딩 UI (12개 액션, 클릭→키감지→중복체크)
- SettingsManager에 KeyBindings API 추가
- MainMenuController에 Settings 버튼 연동
- **파일:** SettingsUI.cs (전면 재작성), SettingsManager.cs, MainMenuController.cs

### R-040 SteamPipe v2
- BuildScript.cs 생성 (Build > Steam Windows 메뉴)
- depot_build_win.vdf 생성
- app_build.vdf 수정 (AppID/DepotID 설정, steam_appid.txt FileExclusion 추가)
- build_and_upload.sh 개선 (경로, 버전 표시, 검증 강화)
- BuildScript에서 빌드 후 steam_appid.txt 삭제 + version.txt 생성
- **파일:** Assets/Editor/BuildScript.cs, SteamPipe/*.vdf, SteamPipe/*.sh

### R-041 QA Checklist v2
- 클라우드 충돌 해소 UI 테스트 항목 추가
- 업적 5개 이상 + 리셋 테스트 명시
- 메모리 누수 테스트 (1시간, <100MB)
- 릴리즈 빌드 디버그 로그/UI 비노출 확인
- steam_appid.txt 빌드 제외 확인
- Store/Legal 섹션 추가 (스토어 페이지, EULA)
- **파일:** SteamPipe/QA_CHECKLIST.md

## specs 참조: Y (SPEC-R-039, SPEC-R-040, SPEC-R-041)
