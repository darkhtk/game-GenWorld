using UnityEngine;

public static class GameConfig
{
    public const int TileSizePx = 32; // pixel size (for import settings only)
    public const int TileSize = 1;   // world units per tile (PPU=32 → 32px = 1 unit)
    public const int MapWidthTiles = 200, MapHeightTiles = 200;
    public const float MapWidthUnits = MapWidthTiles * TileSize, MapHeightUnits = MapHeightTiles * TileSize;
    public const int GameWidth = 1024, GameHeight = 768;
    public const float StrAtkBonus = 1.5f, StrHpBonus = 3f;
    public const float DexSpdBonus = 1f, DexDefBonus = 1.5f;
    public const float WisMpBonus = 4f, WisMpRegenBonus = 0.5f;
    public const float LucCritBonus = 1f, LucCritDmgBonus = 0.02f;
    public const int BaseHp = 100, BaseMp = 60, BaseAtk = 10, BaseDef = 5, BaseSpd = 3, BaseCrit = 3;
    public const int HpPerLevel = 12, MpPerLevel = 6;
    public const int SkillPointsPerLevel = 2, StatPointsPerLevel = 3;
    public static int XpForLevel(int level) => Mathf.FloorToInt(50 * Mathf.Pow(1.3f, level - 1));
    public const int EnhanceBonusPerLevel = 2;
    public const float AutoAttackBaseRange = 1.75f, AutoAttackRangePerLevel = 0.06f;
    public const float AutoAttackCooldown = 1000f, AutoAttackDmgPerLevel = 0.05f;
    public const float AutoAttackArcHalf = Mathf.PI / 3f;
    public const float DefaultDetectRange = 5f, ChaseRangeMult = 2.5f, MaxSpawnDistance = 25f;
    public const float DropTimeout = 300000f, DeathGoldPenalty = 0.1f;
    public const float ProjectileSpeedScale = 0.35f; // 전역 발사체 속도 배율 — 여기서 조정
    public static readonly string[] EquipSlots = { "weapon", "helmet", "armor", "boots", "accessory" };
    public const int SkillSlotCount = 6, SkillMaxLevel = 5;
    public const float HpRegenPerSecond = 2f;
    public static readonly Color GradeCommon = Color.white;
    public static readonly Color GradeUncommon = new(0.4f, 1f, 0.533f);   // #66ff88
    public static readonly Color GradeRare = new(0.4f, 0.533f, 1f);       // #6688ff
    public static readonly Color GradeLegendary = new(1f, 0.6f, 0.267f);  // #ff9944
    public static Color GetGradeColor(ItemGrade g) => g switch
    {
        ItemGrade.Uncommon => GradeUncommon, ItemGrade.Rare => GradeRare,
        ItemGrade.Legendary => GradeLegendary, _ => GradeCommon
    };

    // S-120: Single source of truth for BGM transition timing + boss-region routing.
    // Both GameManager.PlayRegionBGM (fadeTime branch) and AudioManager.CrossfadeBGM
    // MUST read from here.
    public static class Audio
    {
        public const float BgmTransitionDefault   = 1.0f;
        public const float BgmTransitionBossEnter = 1.5f;
        public const float BgmTransitionBossExit  = 1.0f;
        public const bool  BgmCrossfadeDualSource = true;
        public static readonly string[] BossRegionIds = { "volcano", "dragon_lair" };

        public static bool IsBossRegion(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return false;
            for (int i = 0; i < BossRegionIds.Length; i++)
                if (BossRegionIds[i] == regionId) return true;
            return false;
        }

        public static float BgmFadeTimeFor(string regionId) =>
            IsBossRegion(regionId) ? BgmTransitionBossEnter : BgmTransitionDefault;

        // S-121: NPC dialogue start/end SFX (UI feedback, separate from per-line sfx_speech).
        public const string DialogueOpenSfxName    = "sfx_dialogue_open";
        public const string DialogueCloseSfxName   = "sfx_dialogue_close";
        public const float  DialogueOpenSfxVolume  = 0.85f;
        public const float  DialogueCloseSfxVolume = 0.70f;
        public const bool   DialogueSfxEnabled     = true;

        // S-140: Boss death feedback — CinemachineImpulse + victory chord SFX + BGM duck.
        // Reuses existing sfx_boss_defeat.wav (~1.5s) to avoid asset bloat. Duck is held
        // longer/deeper than item-pickup default (-6dB / 0.4s) so the chord rings clean.
        public const string BossDeathSfxName              = "sfx_boss_defeat";
        public const float  BossDeathSfxVolume            = 0.95f;
        public const float  BossDeathCameraShakeMs        = 600f;
        public const float  BossDeathCameraShakeIntensity = 0.022f;
        public const float  BossDeathDuckBgmDropDb        = -8f;
        public const float  BossDeathDuckBgmDuration      = 1.2f;
        public const bool   BossDeathEnabled              = true;
    }

    // S-122: Common UI button SFX (hover/click). UIButtonSfx component reads these
    // unless per-instance overrides are set. Hover volume kept low (0.55) since
    // hover SFX fires on every cursor sweep — must not fatigue.
    public static class UI
    {
        public const string ButtonHoverSfxName   = "sfx_ui_hover";
        public const string ButtonClickSfxName   = "sfx_click";
        public const float  ButtonHoverSfxVolume = 0.55f;
        public const float  ButtonClickSfxVolume = 0.85f;
        public const bool   ButtonSfxEnabled     = true;

        // S-128: QuestUI requirement/killRequirement progress bar — fixed-width
        // glyph bar built into the same TMP node (no prefab change). Cells fill
        // proportionally; met (>= required) flips the color from unmet→met.
        public const int    QuestProgressBarCells      = 10;
        public const string QuestProgressBarMetColor   = "#66ff88";
        public const string QuestProgressBarUnmetColor = "#ff9944";
        public const string QuestProgressBarBgColor    = "#444444";
        public const bool   QuestProgressBarEnabled    = true;

        // S-123: Inventory slot bg alpha — empty slots dim to give a visible
        // "available drop target" affordance without hiding the grid structure.
        // Filled stays opaque. Same 0.3 value also tracks the drag-ghost convention
        // (InventorySlotUI.SetDragGhost / S-138 follow-up).
        public const float InventorySlotEmptyAlpha  = 0.3f;
        public const float InventorySlotFilledAlpha = 1.0f;
    }

    // Single source of truth for monster aggression / engagement timing windows.
    // Both MonsterController.UpdateAI (Return-state guard) and CombatManager
    // (IsInCombat, dodge-aggro sync) MUST read from here — see SPEC-S-101 §3-3.
    public static class MonsterAggro
    {
        // Window after a player hit during which the monster will not give up the
        // chase and slip into Return state. Same value also gates IsInCombat.
        public const float RecentHitWindow = 2f;
        // Same window applied from the combat-side perspective (drives IsInCombat).
        public const float IsInCombatWindow = RecentHitWindow;
        // Multiplier on monster.Def.attackRange used when refreshing aggro while
        // the player is dodging. Within (attackRange × this) we keep the monster
        // engaged so a single 0.2s i-frame can't drop it into Return.
        public const float DodgeAggroSyncRangeMult = 1.3f;
    }
}
