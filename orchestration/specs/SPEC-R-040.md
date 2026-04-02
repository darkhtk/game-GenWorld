# SPEC-R-040: SteamPipe 빌드 및 배포 구성

**관련 태스크:** R-040

---

## 개요
Unity 빌드 → SteamPipe 업로드까지의 자동화 파이프라인 구성.

## 상세 설명
Steam에 게임을 배포하려면 SteamPipe(Steamworks의 콘텐츠 배포 시스템)를 통해 빌드를 업로드해야 한다. 앱 설정 VDF, 디포 설정 VDF, 빌드 스크립트를 작성하고, Unity Editor 빌드 스크립트(`BuildScript.cs`)로 Windows 빌드를 자동 생성한 뒤 SteamCMD로 업로드하는 일련의 과정을 자동화한다. 개발(dev), 베타(beta), 릴리즈(default) 브랜치를 분리 운영한다.

## 데이터 구조
```
SteamBuild/
├── scripts/
│   ├── app_build.vdf          # 앱 빌드 설정
│   ├── depot_build_win.vdf    # Windows 디포 설정
│   └── upload.bat/.sh         # SteamCMD 업로드 스크립트
├── output/                    # Unity 빌드 출력 경로
└── ContentBuilder/            # SteamCMD ContentBuilder

// Unity Editor 빌드 스크립트
public static class BuildScript
{
    [MenuItem("Build/Steam Windows")]
    public static void BuildWindows();

    [MenuItem("Build/Steam Windows + Upload")]
    public static void BuildAndUpload();
}
```

## 연동 경로
| From | To | 방식 |
|------|----|------|
| BuildScript | Unity BuildPipeline | `BuildPipeline.BuildPlayer()` |
| upload.sh | SteamCMD | CLI로 `steamcmd +login +run_app_build` 실행 |
| app_build.vdf | Steamworks | 앱/디포/브랜치 설정 참조 |

## UI 와이어프레임
N/A (에디터 메뉴 및 CLI 스크립트)

## 호출 진입점
- **어디서:** Unity 메뉴 `Build > Steam Windows` 또는 터미널에서 `upload.sh` 실행
- **어떻게:** BuildScript → Unity 빌드 → SteamCMD 업로드 → Steamworks 파트너 사이트에서 브랜치에 배포

## 수용 기준
- [ ] `BuildScript.BuildWindows()`로 Windows 64비트 빌드가 `SteamBuild/output/`에 생성됨
- [ ] `app_build.vdf`, `depot_build_win.vdf` 파일이 올바른 앱 ID와 디포 ID를 참조
- [ ] `upload.sh` 실행 시 SteamCMD로 빌드가 정상 업로드됨
- [ ] `steam_appid.txt`가 빌드 출력에 포함되지 않음
- [ ] 빌드 출력에서 디버그 심볼/에디터 전용 에셋이 제외됨
- [ ] 빌드 버전 번호가 `PlayerSettings.bundleVersion`에서 자동 반영됨
