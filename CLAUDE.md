# GenWorld — Phaser RPG to Unity 2D Port

## Project Overview
Phaser 3 TypeScript 2D RPG (testgame2)을 Unity 2D C#으로 포팅하는 프로젝트.
원본: C:\sourcetree\testgame2\ (참고용, 수정 금지)

## Tech Stack
- Unity 6 (6000.3.9f1), URP
- C# 9, Newtonsoft.Json
- Legacy Input System (Input.GetKey) — activeInputHandler: 2 (Both)
- uGUI + TextMeshPro
- Unity Tilemap + Tilemap Extras
- Cinemachine 2D (설치 필요)

## Orchestration Rules
이 프로젝트는 4개 독립 CLI로 병렬 작업합니다.

### 작업 전
- `docs/orchestration/assignments/너의역할.md` 확인
- `docs/orchestration/reference/` 문서를 먼저 읽어라

### 작업 중
- **담당 폴더만 수정.** 다른 역할의 폴더 수정 금지.
- `docs/orchestration/status/compile-status.md` 확인 — 에러 있으면 내 코드가 원인인지 확인
- 막히면 `docs/orchestration/questions/` 에 질문 파일 생성

### 작업 후
- `docs/orchestration/status/너의역할.md` 갱신 (DONE 또는 진행률)
- `git add` + `git commit` (담당 파일만)
- 다음 assignment 확인

### Git Rules
- 커밋 전 `git pull --rebase`
- 자기 폴더 파일만 커밋
- 남의 파일 충돌 → Director에게 보고 (questions/)

## Coding Standards
- PascalCase: 클래스, 메서드, 프로퍼티
- camelCase: 파라미터, 로컬 변수
- _camelCase: private 필드
- 파일당 하나의 클래스/책임
- 파일 크기 300줄 이하 권장
- 한국어는 게임 데이터(JSON)와 UI 표시 텍스트에서만. 코드/주석은 영어.
- Systems: 순수 C# 클래스 (MonoBehaviour 아님). static 또는 new로 생성.
- Entities: MonoBehaviour + [RequireComponent]
- UI: MonoBehaviour, Canvas 자식 오브젝트에 부착
- 테스트: Assets/Tests/EditMode/, NUnit, 하나의 테스트 = 하나의 동작 검증

## Data Files
- JSON 데이터: Assets/StreamingAssets/Data/ 에 배치
- NPC 프로필 (MD): Assets/StreamingAssets/Data/npc-profiles/
- Lore (MD): Assets/StreamingAssets/Data/lore/
- 원본: C:\sourcetree\testgame2\data\ 에서 복사

## Key Constraints
- Y축 반전: Phaser Y↓ → Unity Y↑. 타일 좌표 변환 시 주의.
- 시간 단위: Phaser ms → Unity seconds. 경계에서 변환 (Time.time * 1000f).
- 스프라이트: 32x32 pixel art. Import: PPU=32, Filter=Point, Compression=None.
- Rigidbody2D.linearVelocity (Unity 6 naming).

## Folder Ownership
| Role | Folders |
|------|---------|
| Director | Assets/Scripts/Core/GameManager.cs, docs/orchestration/ |
| Dev-Backend | Assets/Scripts/Systems/, Scripts/AI/, Scripts/Entities/, Tests/EditMode/ |
| Dev-Frontend | Assets/Scripts/UI/, Scripts/Effects/, Prefabs/UI/ |
| Asset/QA | Assets/Art/, StreamingAssets/, Scenes/, Prefabs/ (UI/ 제외) |
| Shared (읽기 전용) | Assets/Scripts/Core/, Assets/Scripts/Data/ |
