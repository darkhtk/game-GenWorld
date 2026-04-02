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

17. **R-017 사운드/음악 시스템 기반** — AudioManager 싱글턴, BGM/SFX 재생, 볼륨 설정. 전체 시스템 기반.
18. **R-018 미니맵 UI** — 플레이어 위치 + 주변 POI(NPC/몬스터) 표시. 새 UI 패널.
19. **R-019 NPC 일과 시스템** — NPC가 시간대별로 위치 이동. `VillageNPC.cs` 확장 + 경로 데이터.
20. **R-020 NPC 호감도 이벤트** — 우호도 임계점 도달 시 특별 대화/보상 트리거. `NPCBrain.cs`, `AIManager.cs` 연동.
21. **R-021 주/야간 사이클** — 시간 경과에 따른 조명 변화 + 몬스터 스폰 변경. 새 시스템.
22. **R-022 업적 시스템** — 마일스톤 추적 + UI 배지 표시. 새 시스템 + UI.
23. **R-023 장비 강화 실패/파괴 시스템** — EnhanceUI 기존 로직에 확률적 실패/파괴 추가.
24. **R-024 월드 이벤트 시스템** — 일정 시간 간격으로 특수 몬스터/보상 이벤트 발생.

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
45. **🎨 A-004 몬스터 사망 애니메이션 시트** — 12종 몬스터별 사망 4프레임 스프라이트시트. `Assets/Art/Sprites/`
46. **🎨 A-005 NPC 대화 포트레이트** — 8명 NPC 대화창 얼굴 이미지 (64×64). `Assets/Art/Sprites/Portraits/`
47. **🎨 A-006 스킬트리 배경 + 노드** — 3개 트리(melee/ranged/magic) 배경 + 스킬 노드 아이콘. `Assets/Art/Sprites/UI/SkillTree/`
48. **🎨 A-007 장비 세트 아이콘** — 8개 장비 세트별 세트 효과 아이콘. `Assets/Art/Sprites/Icons/`
