using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] TextMeshProUGUI statsText;
    [SerializeField] Image gradeBar;

    static TooltipUI _instance;
    RectTransform _panelRect;

    void Awake()
    {
        _instance = this;
        if (panel != null)
        {
            panel.SetActive(false);
            _panelRect = panel.GetComponent<RectTransform>();
        }
    }

    public static void ShowItem(ItemDef item, Vector2 screenPos)
    {
        if (_instance == null || _instance.panel == null || item == null) return;

        _instance.panel.SetActive(true);

        if (_instance.titleText != null)
        {
            _instance.titleText.text = item.name ?? "unknown";
            _instance.titleText.color = GameConfig.GetGradeColor(item.GradeEnum);
            _instance.titleText.fontStyle = TMPro.FontStyles.Bold;
        }

        if (_instance.descText != null)
        {
            _instance.descText.text = item.description ?? "";
            _instance.descText.color = new Color(0.75f, 0.75f, 0.75f);
            _instance.descText.fontStyle = TMPro.FontStyles.Italic;
        }

        if (_instance.statsText != null)
        {
            var lines = new List<string>();
            var s = item.stats;
            if (s != null)
            {
                if (s.atk > 0) lines.Add($"<color=#ffaa55>ATK +{s.atk}</color>");
                if (s.def > 0) lines.Add($"<color=#55aaff>DEF +{s.def}</color>");
                if (s.maxHp > 0) lines.Add($"<color=#ff6666>HP +{s.maxHp}</color>");
                if (s.maxMp > 0) lines.Add($"<color=#6688ff>MP +{s.maxMp}</color>");
                if (s.spd > 0) lines.Add($"<color=#66ff88>SPD +{s.spd}</color>");
                if (s.crit > 0) lines.Add($"<color=#ffdd44>CRIT +{s.crit}</color>");
            }
            if (item.healHp > 0) lines.Add($"<color=#ff9999>Heal HP {item.healHp}</color>");
            if (item.healMp > 0) lines.Add($"<color=#9999ff>Heal MP {item.healMp}</color>");
            if (item.shopPrice > 0) lines.Add($"<color=#ffd900>Value: {item.shopPrice}G</color>");
            lines.Add($"Grade: <color=#{ColorUtility.ToHtmlStringRGB(GameConfig.GetGradeColor(item.GradeEnum))}>{item.grade ?? "common"}</color>");
            if (item.stackable) lines.Add($"<color=#888888>Stack: {item.maxStack}</color>");
            _instance.statsText.text = string.Join("  ", lines);
        }

        if (_instance.gradeBar != null)
            _instance.gradeBar.color = GameConfig.GetGradeColor(item.GradeEnum);

        PositionTooltip(screenPos);
    }

    public static void ShowSkill(SkillDef skill, int level, Vector2 screenPos)
    {
        if (_instance == null || _instance.panel == null || skill == null) return;

        _instance.panel.SetActive(true);

        if (_instance.titleText != null)
        {
            Color treeColor = skill.tree switch
            {
                "ranged" => new Color(0.3f, 0.9f, 0.3f),
                "magic"  => new Color(0.3f, 0.5f, 1f),
                _        => new Color(0.9f, 0.4f, 0.4f)
            };
            _instance.titleText.text = skill.name ?? skill.id;
            _instance.titleText.color = treeColor;
            _instance.titleText.fontStyle = TMPro.FontStyles.Bold;
        }

        if (_instance.descText != null)
        {
            _instance.descText.text = skill.description ?? "";
            _instance.descText.color = new Color(0.75f, 0.75f, 0.75f);
            _instance.descText.fontStyle = TMPro.FontStyles.Italic;
        }

        if (_instance.statsText != null)
        {
            var lines = new List<string>();
            lines.Add($"Level: <color=#aaffaa>{level}/{GameConfig.SkillMaxLevel}</color>");
            if (skill.damage > 0) lines.Add($"<color=#ffaa55>Damage: {skill.damage}</color>");
            if (skill.mpCost > 0) lines.Add($"<color=#6688ff>MP Cost: {skill.mpCost}</color>");
            if (skill.cooldown > 0) lines.Add($"<color=#cccccc>CD: {skill.cooldown / 1000f:F1}s</color>");
            if (skill.range > 0) lines.Add($"<color=#aaddff>Range: {skill.range:F0}</color>");
            if (skill.aoe > 0) lines.Add($"<color=#ffcc44>AoE: {skill.aoe:F0}</color>");
            _instance.statsText.text = string.Join("\n", lines);
        }

        if (_instance.gradeBar != null)
            _instance.gradeBar.color = new Color(0.4f, 0.6f, 1f);

        PositionTooltip(screenPos);
    }

    public static void Hide()
    {
        if (_instance != null && _instance.panel != null)
            _instance.panel.SetActive(false);
    }

    static void PositionTooltip(Vector2 screenPos)
    {
        if (_instance._panelRect == null) return;
        var rt = _instance._panelRect;

        Vector2 size = rt.sizeDelta;
        float pad = 10f * (Screen.height / 1080f);
        float x = screenPos.x + pad;
        float y = screenPos.y - pad;

        if (x + size.x > Screen.width) x = screenPos.x - size.x - pad;
        if (y - size.y < 0) y = screenPos.y + size.y + pad;

        rt.position = new Vector3(x, y, 0);
    }
}
