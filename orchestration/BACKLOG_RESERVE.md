# Backlog Reserve — 상시 개선 태스크 풀

> **용도:** BOARD의 Backlog가 0건일 때 개발자가 자가 배정하는 예비 목록.
> 위에서부터 순서대로 선택. 완료 후 이 파일에서 삭제 + BOARD Done에 추가.
> 감독관/Coordinator가 수시로 항목을 보충한다.

## 규칙
- 개발자는 BOARD Backlog가 0건이면 이 파일에서 최상단 항목을 가져가 BOARD에 등록 + 구현.
- 한 번에 1건만 가져간다.
- 가져간 항목은 이 파일에서 삭제하고 BOARD 로드맵에 추가한다.
- tasks/TASK-XXX.md 파일이 없으면 여기 설명으로 충분 — 별도 생성 불필요.
- **🎨 태그 태스크는 감독관 전용** — 개발자는 스킵하고 다음 항목을 가져간다.
- **specs/SPEC-XXX.md 존재 시 기획서 따라 구현.**

---

## 예비 태스크 (위에서부터 선택)

> ⚠️ 방향: project.config.md "개발 방향/우선순위" 참조.

### 안정성 (Stability) — 최우선

(all completed: R-001~R-006)

### 게임플레이 개선 (Existing Feature Enhancement) — 높음

(all gameplay enhancement completed: R-007~R-016)

### 🎨 에셋 (감독관 전용) — 높음

25. ~~**🎨 R-025 UI 공통 스프라이트**~~ ✅ 완료 (14종 생성: panel, button×3, slot×2, tooltip, frame, bars×4, dialog, separator)
26. ~~**🎨 R-026 상태이상 아이콘 세트**~~ ✅ 완료 (6종: poison, burn, slow, stun, bleed, mana_shield — 16×16 pixel art)
27. ~~**🎨 R-027 스킬 이펙트 VFX 스프라이트**~~ ✅ 완료 (6종 스프라이트시트: slash, fireball, ice_bolt, heal, hit_impact, lightning + SkillVFX.cs 개선)
28. ~~**🎨 R-028 몬스터 피격/공격 이펙트**~~ ✅ 완료 (3종: melee_hit, ranged_hit, magic_hit + CombatManager VFX 연동)
29. ~~**🎨 R-029 아이템 드롭 이펙트**~~ ✅ 완료 (loot_glow 8프레임 + loot_pickup 6프레임 + LootDropVFX.cs 바운스/글로우 컴포넌트)
30. ~~**🎨 R-030 지역 전용 타일셋 보충**~~ ✅ 완료 (9종: swamp×3, volcano×3, dragon_lair×3 — tileset.png 확장 7→16타일 + 9 TileAsset)
31. ~~**🎨 R-031 플레이어 애니메이션 컨트롤러**~~ ✅ 완료 (PlayerAnimator.cs + player.png 4방향×4프레임 재슬라이싱 + PlayerController 연동)
32. ~~**🎨 R-032 미니맵 아이콘**~~ ✅ 완료 (5종: player 녹색다이아, NPC 파란원, monster 빨간삼각, portal 보라소용돌이, quest 금색느낌표 — 16×16)

### 신규 기능 (New Features) — 보통

~~R-019 NPC 일과 시스템~~ → BOARD로 이동
~~R-020 NPC 호감도 이벤트~~ → BOARD로 이동
~~R-021 주/야간 사이클~~ → BOARD로 이동
~~R-022 업적 시스템~~ → BOARD로 이동
~~R-023 장비 강화 실패/파괴~~ → BOARD로 이동
~~R-024 월드 이벤트 시스템~~ → BOARD로 이동

### --- [2026-04-02] 플레이어, 몬스터, npc, 스킬 등의 animation확인 ---


### 게임플레이 개선 (Existing Feature Enhancement) — 높음

~~R-027: AnimationDef~~ → BOARD로 이동

~~R-028: AnimationPreviewUI~~ → BOARD로 이동

~~R-029: PlayerAnimator 검증~~ → BOARD로 이동

~~R-030: MonsterController 애니메이션~~ → BOARD로 이동

~~R-031, R-032~~ → BOARD로 이동

### 🎨 에셋 (감독관 전용) — 높음

### ~~🎨 A-001: 누락 애니메이션 클립 제작/확보~~ ✅ 완료
VFX 오버레이 3종 생성: vfx_monster_death (8프레임 dissolve), vfx_npc_react (6프레임 느낌표 팝업), vfx_npc_talk (4프레임 말풍선)

---

### 폴리시 / 완성도 (Polish) — 보통

33. **R-033 로딩 화면 진행 바** — BootScene에서 데이터 로딩 시 진행률 표시. `BootSceneController.cs` 개선.
34. **R-034 설정 메뉴 (볼륨/해상도)** — PauseMenuUI에 오디오 볼륨, 화면 해상도, 전체화면 토글 추가.
35. **R-035 키 바인딩 표시** — 설정 또는 HUD에 현재 조작키 안내 표시. 새 UI 패널.
36. **R-036 사망 화면 + 부활 옵션** — 플레이어 사망 시 결과 화면 + 마을 부활/제자리 부활 선택. 새 UI.
37. **R-037 경험치 획득 플로팅 텍스트** — 몬스터 처치 시 +XP 텍스트 표시. `DamageText.SpawnText` 활용.
38. **R-038 골드 변동 플로팅 텍스트** — 골드 획득/소비 시 +/-G 텍스트 표시.
39. **R-039 자동 포션 사용** — HP가 일정 % 이하일 때 자동으로 HP 포션 사용. 설정 토글.
40. **R-040 NPC 대화 시 카메라 줌인** — 대화 시작 시 Cinemachine 줌인, 종료 시 복원.
41. **R-041 지역 진입 알림** — 새 지역 진입 시 화면 중앙에 지역명 페이드인/아웃 표시.
42. **R-042 게임 통계 화면** — 플레이 시간, 처치 몬스터 수, 사용 스킬 횟수 등 통계 UI.

### 🎨 에셋 2차 (감독관 전용)

43. ~~**🎨 A-002 등급별 아이템 프레임**~~ ✅ 완료 (5종: common 회색, uncommon 초록, rare 파랑, epic 보라, legendary 금색 — 32×32 9-slice)
44. ~~**🎨 A-003 지역 진입 배너 스프라이트**~~ ✅ 완료 (8종: village/forest/cave/deep_cave/swamp/dark_swamp/volcano/dragon_lair — 128×32 그라디언트)
45. ~~**🎨 A-004 몬스터 사망 애니메이션 시트**~~ ✅ 완료 (12종×4프레임: flash→red tint→dissolve→particles — 원본 스프라이트 기반)
46. ~~**🎨 A-005 NPC 대화 포트레이트**~~ ✅ 완료 (8명: blacksmith/elder/guard/herbalist/hunter/innkeeper/merchant/scholar — 64×64 원형 프레임)
47. ~~**🎨 A-006 스킬트리 배경 + 노드**~~ ✅ 완료 (3배경 melee/ranged/magic + 3노드 locked/available/learned + 3연결선 = 9종)
48. ~~**🎨 A-007 장비 세트 아이콘**~~ ✅ 완료 (8종: wolf/treant/golem/crystal/venom/scale/dragon/legend — 16×16 실드 엠블럼, items.json setBonuses 기반)

### --- [2026-04-02] 스팀에 배포할 거야 ---


### --- [2026-04-02] 스팀 배포 준비 ---

### 안정성 (Stability) — 최우선

### R-035: Steamworks SDK 통합 및 초기화
Steamworks.NET 패키지 설치, `SteamManager` 싱글턴 생성, `BootSceneController`에서 Steam 초기화 처리. specs/SPEC-R-035.md 참조.

### R-036: Steam 클라우드 저장 연동
`DataManager`의 저장/불러오기를 Steam Remote Storage API와 연동하여 클라우드 세이브 지원. specs/SPEC-R-036.md 참조.

### 게임플레이 개선 (Existing Feature Enhancement) — 높음

### R-037: Steam 업적(Achievement) 시스템
`EventBus` 이벤트를 감지하여 Steam 업적을 해제하는 `SteamAchievementManager` 구현. specs/SPEC-R-037.md 참조.

### R-038: 게임 설정 시스템 (SettingsManager)
해상도·전체화면·그래픽 품질·오디오 볼륨·키 바인딩을 관리하는 `SettingsManager` 데이터 계층 구현. specs/SPEC-R-038.md 참조.

### R-039: 설정 UI (그래픽·오디오·조작 탭)
`SettingsManager`와 연동되는 탭 형식 설정 UI 구현. `MainMenuController` 및 인게임 Pause 메뉴에서 접근 가능. specs/SPEC-R-039.md 참조.

### 폴리시 / 완성도 (Polish) — 보통

### R-040: SteamPipe 빌드 및 배포 구성
Steam 앱/디포 VDF 설정, 빌드 스크립트, SteamCMD 업로드 자동화 파이프라인 구성. specs/SPEC-R-040.md 참조.

### R-041: 출시 전 통합 QA 체크리스트
Steam API 연동 검증, 오프라인 모드 폴백, 성능 프로파일링, 빌드 사이즈 최적화 수행. specs/SPEC-R-041.md 참조.

### 🎨 에셋 (감독관 전용) — 높음

### ~~🎨 A-008: 스팀 스토어 페이지 에셋~~ ✅ 완료
7종 placeholder (정확한 Steam 규격): capsule_header 460×215, capsule_small 231×87, capsule_large 616×353, library_hero 3840×1240, library_logo 1280×720, community_icon 32×32+184×184. JPG 캡슐 포함. ⚠️ 실제 스크린샷/마케팅 아트로 교체 필요.

### ~~🎨 A-009: 스팀 업적 아이콘 세트~~ ✅ 완료
10종 업적 아이콘 쌍 (locked/unlocked 64×64 JPG): first_blood, level_10, level_25, quest_master, dragon_slayer, full_set, combo_king, explorer, hoarder, survivor.

---

### 완성도 / 미관 (Visual Polish) — 높음

49. **V-001 HUD 바 스타일링** — HP/MP/XP 바에 배경 패널, 라벨 아이콘, 그라디언트 효과 적용. 현재 단색 사각형 → 게임 느낌 UI.
50. **V-002 인벤토리 그리드 미관** — 슬롯 배경, 등급 프레임(grade_frame_*), 아이템 아이콘 배치, 호버 하이라이트.
51. **V-003 스킬바 아이콘 표시** — 스킬 슬롯에 실제 스킬 아이콘 스프라이트 표시 (현재 빈 검정).
52. **V-004 대화 UI 포트레이트 표시** — DialogueUI에 NPC 포트레이트 이미지 영역 + npcPortraitImage 연결.
53. **V-005 메인 메뉴 배경** — MainMenuScene에 배경 이미지 또는 타일맵 프리뷰 + 타이틀 스타일링.
54. **V-006 부트 스플래시 로고** — BootScene에 게임 로고 이미지 표시 (현재 텍스트만).
55. **V-007 몬스터/NPC 이름표** — 머리 위에 이름 TextMeshPro 표시 (World Space Canvas).
56. **V-008 파티클 시스템 연동** — 힐/레벨업/아이템 획득 시 Unity ParticleSystem 이펙트.
57. **V-009 UI 패널 전환 애니메이션** — 인벤토리/스킬트리 등 패널 열기/닫기 시 슬라이드/페이드 효과.
58. **V-010 카메라 줌 조절** — 마우스 휠로 카메라 줌 인/아웃 (orthographicSize 조절).
59. **V-011 데미지 넘버 색상 분류** — 물리 데미지 흰색, 마법 파랑, 크리티컬 노랑, 회복 초록 구분.
60. **V-012 타일맵 경계 블렌딩** — 리전 경계에서 타일 자연스러운 전환 (오토타일 또는 블렌딩).

### 🎨 에셋 — 미관 (감독관 전용)

61. **🎨 A-010 UI 패널 배경 리디자인** — panel_bg를 더 정교한 RPG 스타일로 재생성 (모서리 장식, 내부 텍스처).
62. **🎨 A-011 HUD 바 프레임 스프라이트** — HP/MP/XP/Dodge 바 전용 프레임 + 채움 그라디언트 스프라이트.
63. **🎨 A-012 메인 메뉴 배경 이미지** — 1920×1080 타이틀 화면 배경 (게임 아트 스타일).
64. **🎨 A-013 부트 로고 이미지** — 512×256 게임 로고 (GenWorld 타이틀 아트).
65. **🎨 A-014 커서/포인터 스프라이트** — 게임 전용 마우스 커서 32×32.
