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
            string gradeHex = "#" + ColorUtility.ToHtmlStringRGB(GameConfig.GetGradeColor(item.GradeEnum));
            _instance.titleText.color = Color.white;
            _instance.titleText.fontStyle = TMPro.FontStyles.Normal;
            _instance.titleText.text = $"<b><color={gradeHex}>{item.name ?? "unknown"}</color></b>";
        }

        if (_instance.descText != null)
        {
            _instance.descText.color = Color.white;
            _instance.descText.fontStyle = TMPro.FontStyles.Italic;
            _instance.descText.text = $"<color=#bbbbbb>{item.description ?? ""}</color>";
        }

        if (_instance.statsText != null)
        {
            _instance.statsText.color = Color.white;
            var lines = new List<string>();
            var s = item.stats;
            if (s != null)
            {
                if (s.atk > 0) lines.Add($"<color=#885522>ATK</color> <color=#ff9944><b>+{s.atk}</b></color>");
                if (s.def > 0) lines.Add($"<color=#224488>DEF</color> <color=#55aaff><b>+{s.def}</b></color>");
                if (s.maxHp > 0) lines.Add($"<color=#882222>HP</color> <color=#ff6655><b>+{s.maxHp}</b></color>");
                if (s.maxMp > 0) lines.Add($"<color=#333388>MP</color> <color=#6688ff><b>+{s.maxMp}</b></color>");
                if (s.spd > 0) lines.Add($"<color=#226644>SPD</color> <color=#55ee88><b>+{s.spd}</b></color>");
                if (s.crit > 0) lines.Add($"<color=#886600>CRIT</color> <color=#ffdd44><b>+{s.crit}</b></color>");
            }
            if (item.healHp > 0) lines.Add($"<color=#884444>HealHP</color> <color=#ff9999><b>{item.healHp}</b></color>");
            if (item.healMp > 0) lines.Add($"<color=#444488>HealMP</color> <color=#9999ff><b>{item.healMp}</b></color>");
            if (item.shopPrice > 0) lines.Add($"<color=#888888>Value</color> <color=#ffd900>\u25c6 <b>{item.shopPrice:N0}</b>G</color>");
            string gradeHex = "#" + ColorUtility.ToHtmlStringRGB(GameConfig.GetGradeColor(item.GradeEnum));
            lines.Add($"<color=#888888>Grade</color> <color={gradeHex}>{item.grade ?? "common"}</color>");
            if (item.stackable) lines.Add($"<color=#888888>Stack</color> <color=#aaaaaa><b>{item.maxStack}</b></color>");
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
        {
            Color treeColor = skill.tree switch
            {
                "ranged" => new Color(0.3f, 0.9f, 0.3f),
                "magic"  => new Color(0.3f, 0.5f, 1f),
                _        => new Color(0.9f, 0.3f, 0.3f)
            };
            string treeHex = "#" + ColorUtility.ToHtmlStringRGB(treeColor);
            _instance.titleText.color = Color.white;
            _instance.titleText.fontStyle = TMPro.FontStyles.Normal;
            _instance.titleText.text = $"<b><color={treeHex}>{skill.name ?? skill.id}</color></b>";
        }

        if (_instance.descText != null)
        {
            _instance.descText.color = Color.white;
            _instance.descText.fontStyle = TMPro.FontStyles.Italic;
            _instance.descText.text = $"<color=#bbbbbb>{skill.description ?? ""}</color>";
        }

        if (_instance.statsText != null)
        {
            _instance.statsText.color = Color.white;
            var lines = new List<string>();
            lines.Add($"<color=#888888>Level</color> <color=#aaffaa><b>{level}</b>/{GameConfig.SkillMaxLevel}</color>");
            if (skill.damage > 0) lines.Add($"<color=#888888>Dmg</color> <color=#ffaa55><b>{skill.damage}</b></color>");
            if (skill.mpCost > 0) lines.Add($"<color=#888888>MP</color> <color=#6688ff><b>{skill.mpCost}</b></color>");
            if (skill.cooldown > 0) lines.Add($"<color=#888888>CD</color> <color=#cccccc><b>{skill.cooldown / 1000f:F1}s</b></color>");
            if (skill.range > 0) lines.Add($"<color=#888888>Rng</color> <color=#aaddff><b>{skill.range:F0}</b></color>");
            if (skill.aoe > 0) lines.Add($"<color=#888888>AoE</color> <color=#ffcc44><b>{skill.aoe:F0}</b></color>");
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
