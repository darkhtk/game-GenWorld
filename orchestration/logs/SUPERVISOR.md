# SUPERVISOR Loop Log

> **최종 실행:** 2026-04-03 (루프 #27)
> **모드:** 성능 최적화 — HIGH 이슈 7건 수정

## 이번 루프 수행 내용

### 성능 최적화 — 5개 파일 7건 HIGH 이슈 수정

**감사 범위:** PlayerController, MonsterController, CombatManager, TimeSystem, HUD, MinimapUI, DamageText, GameManager, MonsterSpawner (10개 파일 전수 감사)

#### MinimapUI — FindFirstObjectByType/GetComponentInChildren 제거 (2 HIGH)
- **문제:** UpdateMapView()/UpdateEntityIcons()에서 매 0.2초마다 FindFirstObjectByType<PlayerController>, GetComponentInChildren<MonsterSpawner>, FindObjectsByType<VillageNPC> 호출
- **수정:** `_cachedPlayer`, `_cachedSpawner` null 시에만 조회. VillageNPC는 1초마다 캐시 갱신.
- **효과:** 프레임당 3회 씬 검색 → 0회 (캐시 히트 시)

#### HUD — GetEquippedSkills() 배열 할당 제거 (1 HIGH)
- **문제:** UpdateBuffDurations()에서 매 프레임 `Skills.GetEquippedSkills()` 호출 → `string[].Clone()` 할당
- **수정:** 기존 `GetEquippedSkill(int slot)` 인덱스 접근 메서드 사용으로 교체
- **효과:** 매 프레임 string[] GC 할당 제거
- **참고:** GetCooldowns()는 이미 `_cooldownBuffer` 사전 할당 패턴 사용 — 문제 없음 확인

#### GameManager — Camera.main 캐싱 (1 HIGH)
- **문제:** LateUpdate()에서 매 프레임 `Camera.main` 조회 (내부적으로 FindGameObjectsWithTag)
- **수정:** `_cachedCam` 필드 추가, null 시에만 재조회
- **효과:** 매 프레임 태그 검색 제거

#### MonsterController — GetComponent<Animator> 캐싱 (1 HIGH)
- **문제:** PlayAnimation()에서 매 호출마다 `GetComponent<Animator>()` (UpdateAI에서 호출됨)
- **수정:** `_cachedAnimator` + `_animatorChecked` 플래그로 1회만 조회
- **효과:** 몬스터 수 × 프레임당 GetComponent 호출 제거

#### CombatManager — 델리게이트 할당 캐싱 (2 HIGH)
- **문제:** BuildSkillContext/BuildActionContext에서 매 스킬 사용마다 6개 클로저 람다 할당
- **수정:** Init()에서 `_dealDmgDel`, `_showDmgDel`, `_showEffectDel`, `_onKillDel`, `_shakeCamDel`, `_applyEffectDel` 사전 생성. `recalcStats`는 static 람다.
- **효과:** 스킬 사용당 6개 클로저 할당 → 0개 (healPlayer만 stats 캡처로 per-call 유지)
- **추가:** `_pendingKills` Init()에서 사전 할당

#### 추가: RESERVE S-038 동기화
- BOARD에서 ✅ Done인 S-038이 RESERVE에서 ⬜ — ✅로 동기화

## 누적 현황 (루프 #1~#27)
| 루프 | 행동 | 결과 |
|------|------|------|
| #1 | 에셋 + AI 대화 수정 | 치명 버그 8건, 에셋 5종 |
| #2 | 성능 최적화 | HUD 더티플래그, playerPos, 스폰 버퍼 |
| #3 | UX 피드백 | SFX 5건 |
| #4 | 에러 + 에셋 선제 | Clean, HUD 아이콘 6종 |
| #5 | 코드 감사 | 퀘스트 치명 버그 2건 |
| #6 | 성능 감사 | 전체 정상 |
| #7 | UX + HUD 최적화 | 포션/퀘스트 스로틀링 |
| #8 | 에러 점검 | Clean |
| #9 | 에셋 선제 생성 | 아이템 아이콘 25종 |
| #10 | 코드 감사 + 최적화 | 쿨다운 버퍼 재사용 |
| #11 | 성능 잔여 스캔 | 할당 0건, Clean |
| #12 | UX/방어 코드 감사 | 전체 시스템 완전 감사 완료 |
| #13 | EventVFX 캐싱 + 리크 수정 | FindFirstObjectByType 캐시, OnDisable 추가 |
| #14~18 | 순찰 (안정) | 빌드 Clean, 변경 없음 |
| #19 | 코드 감사 + RESERVE 재건 | 버그 2건 수정, 25건 태스크 보충 |
| #20 | 🎨 에셋 4건 + 코드 감사 3건 | S-009 버그수정, S-002/003 완료확인, 에셋 4종 |
| #21 | 코드 품질 감사 3건 | S-005 LINQ제거, S-007 stale ref(버그!), S-010 null방어 |
| #22 | 코드 품질 감사 4건 + RESERVE 보충 | S-013 풀 누수(버그!), S-015 null방어, S-016 검증, ObjectPool 가드 |
| #23 | 🎨 에셋 점검 2건 + 코드 감사 5건 | S-031/S-035 누락 없음, 5파일 버그 0건, RESERVE +2 |
| #24 | 🎨 S-039 UI SFX 누락 수정 | 8개 UI에 PlaySFX 22건 추가 |
| #25 | 🎨 S-057/S-058 점검 + 코드 감사 | 스킬아이콘 27셀 슬라이스, 몬스터 누락 0건, 시간단위 치명버그 수정 |
| #26 | 코드 품질 감사 3건 | S-042 저장 잠금(CRITICAL), S-043 보상 방어(MEDIUM), S-044 정상확인 |
| #27 | 성능 최적화 7건 | MinimapUI 캐싱, HUD 배열 제거, Camera.main 캐싱, Animator 캐싱, 델리게이트 캐싱 |

## 총 기여 요약
- **치명 버그 수정**: 18건
- **성능 최적화**: 14건 (+7: MinimapUI 2, HUD 1, GameManager 1, MonsterController 1, CombatManager 2)
- **방어 코드 강화**: 4건
- **UX SFX 추가**: 28건
- **에셋 생성/수정**: 47종
- **에셋 점검 완료**: 4건
- **RESERVE 태스크 보충**: 42건 (누적)
- **감사 시스템**: 49개 클래스 (+5: MinimapUI, HUD, GameManager, MonsterController, CombatManager 재감사)

## 수정 파일 (이번 루프)
- `Assets/Scripts/UI/MinimapUI.cs` (캐싱: Player, MonsterSpawner, VillageNPC)
- `Assets/Scripts/UI/HUD.cs` (GetEquippedSkill 인덱스 접근)
- `Assets/Scripts/Core/GameManager.cs` (Camera.main 캐싱)
- `Assets/Scripts/Entities/MonsterController.cs` (Animator 캐싱)
- `Assets/Scripts/Systems/CombatManager.cs` (델리게이트 6종 캐싱 + _pendingKills 사전할당)
- `orchestration/BACKLOG_RESERVE.md` (S-038 동기화)
- `orchestration/logs/SUPERVISOR.md`

## 다음 루프 예정
- Step 2-4 UX 개선 (누락 피드백 추가)
- RESERVE 항목 순차 진행
