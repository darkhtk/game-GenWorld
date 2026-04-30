using NUnit.Framework;

public class QuestProgressBarTests
{
    [Test]
    public void Build_Zero_ReturnsAllEmpty()
    {
        // progress=0 / required=10 → 0 filled cells, full bg wrap present
        string s = QuestProgressBarBuilder.Build(0, 10);
        Assert.IsTrue(s.Contains(GameConfig.UI.QuestProgressBarBgColor),
            "Empty bar must wrap remaining cells in BG color");
        // No filled glyph "▰" should appear
        Assert.IsFalse(s.Contains("▰"),
            "progress=0 must contain no filled glyphs");
    }

    [Test]
    public void Build_Half_RoundsCorrectly_AndUnmetColor()
    {
        // progress=3 / required=5 → ratio 0.6 × 10 = 6 cells, unmet color
        string s = QuestProgressBarBuilder.Build(3, 5);
        int filledCount = CountOccurrences(s, "▰");
        int emptyCount  = CountOccurrences(s, "░");
        Assert.AreEqual(6, filledCount, "3/5 rounds to 6 filled cells");
        Assert.AreEqual(4, emptyCount,  "3/5 leaves 4 empty cells");
        Assert.IsTrue(s.Contains(GameConfig.UI.QuestProgressBarUnmetColor),
            "Unmet progress must use unmet color");
        Assert.IsFalse(s.Contains(GameConfig.UI.QuestProgressBarMetColor),
            "Unmet progress must NOT contain met color");
    }

    [Test]
    public void Build_Met_AllFilled_GreenColor_NoBgWrap()
    {
        // progress=10 / required=10 → all 10 cells met color, no BG wrap
        string s = QuestProgressBarBuilder.Build(10, 10);
        int filledCount = CountOccurrences(s, "▰");
        int emptyCount  = CountOccurrences(s, "░");
        Assert.AreEqual(10, filledCount, "Met requirement fills all 10 cells");
        Assert.AreEqual(0,  emptyCount,  "Met requirement leaves 0 empty cells");
        Assert.IsTrue(s.Contains(GameConfig.UI.QuestProgressBarMetColor),
            "Met progress must use met color");
        Assert.IsFalse(s.Contains(GameConfig.UI.QuestProgressBarBgColor),
            "Met progress must omit BG color wrap");
    }

    [Test]
    public void Build_OverMet_ClampsToMax()
    {
        // progress=15 / required=5 → ratio>1, clamps to 10 met cells
        string s = QuestProgressBarBuilder.Build(15, 5);
        int filledCount = CountOccurrences(s, "▰");
        Assert.AreEqual(10, filledCount, "Over-met clamps to max cells");
        Assert.IsTrue(s.Contains(GameConfig.UI.QuestProgressBarMetColor),
            "Over-met still uses met color");
    }

    [Test]
    public void Build_RequiredZero_ReturnsEmpty()
    {
        // div-by-zero guard — required <= 0 must return empty string
        Assert.AreEqual("", QuestProgressBarBuilder.Build(0, 0),
            "required=0 must return empty (div-by-zero guard)");
        Assert.AreEqual("", QuestProgressBarBuilder.Build(5, -1),
            "required<0 must return empty");
    }

    static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, System.StringComparison.Ordinal)) != -1)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
