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

    void Awake()
    {
        _instance = this;
        if (panel != null) panel.SetActive(false);
    }

    public static void ShowItem(ItemDef item, Vector2 screenPos)
    {
        if (_instance == null || _instance.panel == null || item == null) return;

        _instance.panel.SetActive(true);

        if (_instance.titleText != null)
        {
            _instance.titleText.text = item.name ?? "unknown";
            _instance.titleText.color = GameConfig.GetGradeColor(item.GradeEnum);
        }

        if (_instance.descText != null)
            _instance.descText.text = item.description ?? "";

        if (_instance.statsText != null)
        {
            var lines = new List<string>();
            var s = item.stats;
            if (s != null)
            {
                if (s.atk > 0) lines.Add($"ATK +{s.atk}");
                if (s.def > 0) lines.Add($"DEF +{s.def}");
                if (s.maxHp > 0) lines.Add($"HP +{s.maxHp}");
                if (s.maxMp > 0) lines.Add($"MP +{s.maxMp}");
                if (s.spd > 0) lines.Add($"SPD +{s.spd}");
                if (s.crit > 0) lines.Add($"CRIT +{s.crit}");
            }
            if (item.healHp > 0) lines.Add($"Heal HP {item.healHp}");
            if (item.healMp > 0) lines.Add($"Heal MP {item.healMp}");
            if (item.shopPrice > 0) lines.Add($"Value: {item.shopPrice}g");
            lines.Add($"Grade: {item.grade ?? "common"}");
            if (item.stackable) lines.Add($"Stack: max {item.maxStack}");
            _instance.statsText.text = string.Join("\n", lines);
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
            _instance.titleText.text = skill.name ?? skill.id;

        if (_instance.descText != null)
            _instance.descText.text = skill.description ?? "";

        if (_instance.statsText != null)
        {
            var lines = new List<string>();
            lines.Add($"Level: {level}/{GameConfig.SkillMaxLevel}");
            if (skill.damage > 0) lines.Add($"Damage: {skill.damage}");
            if (skill.mpCost > 0) lines.Add($"MP Cost: {skill.mpCost}");
            if (skill.cooldown > 0) lines.Add($"Cooldown: {skill.cooldown / 1000f:F1}s");
            if (skill.range > 0) lines.Add($"Range: {skill.range:F0}");
            if (skill.aoe > 0) lines.Add($"AoE: {skill.aoe:F0}");
            if (!string.IsNullOrEmpty(skill.tree)) lines.Add($"Tree: {skill.tree}");
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
        if (_instance.panel == null) return;
        var rt = _instance.panel.GetComponent<RectTransform>();
        if (rt == null) return;

        Vector2 size = rt.sizeDelta;
        float x = screenPos.x + 10f;
        float y = screenPos.y - 10f;

        if (x + size.x > Screen.width) x = screenPos.x - size.x - 10f;
        if (y - size.y < 0) y = screenPos.y + size.y + 10f;

        rt.position = new Vector3(x, y, 0);
    }
}
