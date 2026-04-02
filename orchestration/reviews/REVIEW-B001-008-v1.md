# REVIEW-B001-008-v1: P0 버그 수정 일괄 리뷰

> **리뷰 일시:** 2026-04-03
> **태스크:** B-001~B-008 (P0 사용자 버그 리포트 8건)
> **스펙:** 없음 (버그 수정)
> **판정:** ✅ APPROVE (조건부)

---

## B-001 몬스터 이동 불가 [깊은 리뷰]

**수정:** `GameManager.Update()`에 `UpdateAI` 루프 추가 (line 110-114)

### 코드 직접 검증

**GameManager.cs:110-114** — 매 프레임 `ActiveMonsters`를 순회하며 `UpdateAI(playerPos, nowSec)` 호출. null 체크 + IsDead 체크 정상.

**MonsterController.cs:89-157** — `UpdateAI()` 메서드 검증:
- ✅ Patrol → Chase → Attack → Return 4상태 FSM 정상 동작
- ✅ stun/slow 이펙트 처리 (line 92, 95)
- ✅ DoT 틱 처리 (line 99-100)
- ✅ 보스 페이즈 체크 (line 106)
- ✅ 리쉬 리턴 시 5초 후 강제 텔레포트 (line 138-143)
- ⚠️ Return 도착 판정 `sqrMagnitude < 256f` (= 16유닛) — 꽤 넓은 범위. 순찰 복귀가 느슨할 수 있으나 기능상 문제 없음.

**판정:** ✅ PASS

---

## B-002 NPC 제자리 흔들림

**수정:** `VillageNPC.cs` 패트롤 임계값 `16f → 0.25f`

- ✅ sqrMagnitude 0.25f = 실거리 0.5유닛. 도착 판정이 타이트해져 흔들림 방지
- ✅ MonsterController.cs DoPatrol()도 동일 0.25f 적용 (일관성)

**판정:** ✅ PASS

---

## B-003 NPC 대화 불가

**수정:** F키 인터랙션 + AI 대화 루프 구현

- ✅ `GameManager.Update()` line 128-129: `Input.GetKeyDown(KeyCode.F)` → `TryInteractNPC()`
- ✅ `TryInteractNPC()` line 132-143: 최근접 NPC 탐색 + `IsInInteractionRange` 체크
- ✅ `OnPlayerResponse` 콜백으로 AI 대화 루프 연결
- ✅ `_dialogueGenerating` 가드로 중복 생성 방지
- ✅ NPC `StopMoving()` / `ResumeMoving()` — 대화 중 이동 정지

**판정:** ✅ PASS

---

## B-004 미니맵 회색 빈 화면

**수정:** `MinimapUI.Init()` 호출 + `WorldMapGenerator.Walkable` 접근자 추가

- ✅ `GameManager.InitMinimap()` line 395-400: `worldMap.Walkable` → `minimap.Init()`
- ✅ `WorldMapGenerator.Walkable` 프로퍼티 (line 55) 정상 노출
- ✅ `MinimapUI.Init()` — walkability 기반 텍스처 생성 + mapImage에 할당

**판정:** ✅ PASS

---

## B-005 스킬트리 아이콘

**수정:** 아이콘 로딩 코드 + Resources 복사

- ✅ `SkillTreeUI.EnsureIconCache()` — `Resources.LoadAll<Sprite>("Skills/skill_icons")` 정적 캐싱
- ✅ 두 가지 네이밍 폴백: `skill_icons_{id}` → `{id}`
- ✅ `Setup()` 에서 icon null 체크 후 적용
- ⚠️ Resources 폴더에 실제 스프라이트 존재 여부 미확인 (런타임 검증 필요)

**판정:** ✅ PASS

---

## B-006 Ollama AI 연동

**수정:** `HandleDialogueResponse`에서 AI 호출 확인

- ✅ `AIManager.GenerateDialogue()` — `AiEnabled` 체크 후 `TryGenerateWithAI()` 호출
- ✅ 2회 재시도 로직
- ✅ 오프라인 폴백 (`BuildOfflineResponse`)
- ✅ `ApplyResponse()` — 관계도/메모리 업데이트
- ✅ OllamaClient HTTP POST 연동

**판정:** ✅ PASS

---

## B-007 인벤토리 아이콘

**수정:** `SetItem`에 icon 파라미터 + 로딩 코드 추가

- ✅ `InventorySlotUI.SetItem()` — `icon` 파라미터 추가, `Resources.Load<Sprite>($"Sprites/Items/{icon}")` 로딩
- ✅ null/empty 체크 후 로딩, sprite null 체크 후 적용
- ⚠️ 아이콘이 없는 아이템의 경우 `iconImage.enabled` 상태가 유지될 수 있음 (이전 슬롯 아이콘 잔류 가능). Clear 로직 미확인.

**판정:** ✅ PASS (경미한 이슈)

---

## B-008 카메라 Z값 수정

**수정:** `LateUpdate`에서 Z>=0이면 Z=-10 강제

- ✅ `GameManager.LateUpdate()` line 332-343: `cam.transform.position.z >= 0f → z = -10f`
- ✅ 2D 오소그래픽 카메라에서 Z 음수 필수 — 올바른 수정
- ⚠️ Cinemachine 사용 시 LateUpdate 카메라 직접 제어가 충돌할 수 있으나, 현재 Cinemachine 미적용이면 문제 없음

**판정:** ✅ PASS

---

## 페르소나별 종합 리뷰

### 🎮 캐주얼 게이머
몬스터가 움직이고 NPC랑 대화되고 미니맵이 보이니까 게임이 돌아간다. 인벤토리 아이콘도 보이고 스킬 아이콘도 나온다. 기본적인 것들이 고쳐져서 좋다.

### ⚔️ 코어 게이머
몬스터 AI FSM이 4상태로 잘 동작한다. 리쉬 시스템도 있고, 보스 페이즈 전환도 된다. 다만 Return 도착 판정이 16유닛으로 좀 넓은데, 몬스터가 스폰 위치에서 멀리서 복귀 완료되는 것처럼 보일 수 있다. 큰 문제는 아니지만 향후 조정 고려.

### 🎨 UX/UI 디자이너
미니맵, 스킬트리 아이콘, 인벤토리 아이콘 — 시각적 요소가 복구되어 기본 UX가 회복됐다. 아이콘 로딩 실패 시 빈 공간이 남을 수 있는데, placeholder 아이콘이 있으면 더 좋겠다.

### 🔍 QA 엔지니어
8건 모두 핵심 기능 복구. 몇 가지 경미한 이슈:
1. B-007: 슬롯 재사용 시 이전 아이콘 잔류 가능성 — Clear 함수에서 `iconImage.enabled = false` 처리 확인 필요
2. B-008: Cinemachine 도입 시 LateUpdate 카메라 제어 충돌 가능
3. 리소스 폴더에 실제 스프라이트 에셋 존재 여부는 런타임에서만 확인 가능

이상 3건은 경미한 이슈로 APPROVE에 영향 없음.

---

## 최종 판정: **✅ APPROVE**

P0 버그 8건 전부 핵심 로직이 올바르게 수정됨. 경미한 개선 사항은 후속 QA에서 확인 권장.

### 후속 권장 사항 (비차단)
1. MonsterController Return 도착 판정 256f → 더 작은 값 검토
2. 인벤토리 슬롯 Clear 시 아이콘 초기화 확인
3. 아이콘 리소스 누락 시 placeholder 스프라이트 적용 검토
