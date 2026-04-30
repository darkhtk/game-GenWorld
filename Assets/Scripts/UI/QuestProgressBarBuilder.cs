using System.Text;
using UnityEngine;

public static class QuestProgressBarBuilder
{
    const string FilledChar = "▰"; // ▰
    const string EmptyChar  = "░"; // ░

    public static string Build(int progress, int required)
    {
        if (!GameConfig.UI.QuestProgressBarEnabled) return "";
        if (required <= 0) return "";

        int cells = GameConfig.UI.QuestProgressBarCells;
        float ratio = progress / (float)required;
        int filled = Mathf.Clamp(Mathf.RoundToInt(ratio * cells), 0, cells);
        bool met = progress >= required;
        string filledColor = met
            ? GameConfig.UI.QuestProgressBarMetColor
            : GameConfig.UI.QuestProgressBarUnmetColor;

        var sb = new StringBuilder(64);
        sb.Append("<color=").Append(filledColor).Append('>');
        for (int i = 0; i < filled; i++) sb.Append(FilledChar);
        sb.Append("</color>");
        if (filled < cells)
        {
            sb.Append("<color=").Append(GameConfig.UI.QuestProgressBarBgColor).Append('>');
            for (int i = filled; i < cells; i++) sb.Append(EmptyChar);
            sb.Append("</color>");
        }
        return sb.ToString();
    }
}
