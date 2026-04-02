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
    public const int BaseHp = 100, BaseMp = 60, BaseAtk = 10, BaseDef = 5, BaseSpd = 80, BaseCrit = 3;
    public const int HpPerLevel = 12, MpPerLevel = 6;
    public const int SkillPointsPerLevel = 2, StatPointsPerLevel = 3;
    public static int XpForLevel(int level) => Mathf.FloorToInt(50 * Mathf.Pow(1.3f, level - 1));
    public static int EnhanceCost(int level) => 100 + 150 * level;
    public const int EnhanceBonusPerLevel = 2;
    public const float AutoAttackBaseRange = 56f, AutoAttackRangePerLevel = 2f;
    public const float AutoAttackCooldown = 1000f, AutoAttackDmgPerLevel = 0.05f;
    public const float AutoAttackArcHalf = Mathf.PI / 3f;
    public const float DefaultDetectRange = 150f, ChaseRangeMult = 2.5f, MaxSpawnDistance = 800f;
    public const float DropTimeout = 300000f, DeathGoldPenalty = 0.1f;
    public static readonly string[] EquipSlots = { "weapon", "helmet", "armor", "boots", "accessory" };
    public const int SkillSlotCount = 6, SkillMaxLevel = 5;
    public const float HpRegenPerSecond = 2f;
    public static readonly Color GradeCommon = Color.white;
    public static readonly Color GradeUncommon = new(0.4f, 1f, 0.4f);
    public static readonly Color GradeRare = new(0.4f, 0.6f, 1f);
    public static readonly Color GradeLegendary = new(1f, 0.6f, 0.2f);
    public static Color GetGradeColor(ItemGrade g) => g switch
    {
        ItemGrade.Uncommon => GradeUncommon, ItemGrade.Rare => GradeRare,
        ItemGrade.Legendary => GradeLegendary, _ => GradeCommon
    };
}
