# AI가 결정하는 것 vs 시스템이 결정하는 것

## AI가 결정
- dialogue: 대사 내용
- options: 선택지 문장
- offerQuest: 퀘스트 제안 타이밍 (true/false)
- action: 시스템 기능 제안 (open_shop, heal_player 등)
- relationshipChange: 호감도 변동량
- newMemory: 이번 턴 기억 요약

## 시스템이 결정 (AI가 건드리면 안 됨)
- 퀘스트 내용/보상/요구사항 (quests.json에서 선택)
- 보상 tier (거절 횟수로 자동 계산)
- 힐/상점/제작/강화 실행 (action 버튼으로)
- 대화 종료 (작별 패턴 감지로 자동)
- 플레이어 스탯/인벤토리 변경
