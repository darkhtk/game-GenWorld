using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NpcProfilePanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;

    [Header("NPC Info")]
    [SerializeField] TextMeshProUGUI npcNameText;
    [SerializeField] TextMeshProUGUI relationshipText;
    [SerializeField] TextMeshProUGUI moodText;

    [Header("Memories")]
    [SerializeField] ScrollRect memoriesScrollRect;
    [SerializeField] Transform memoriesContent;
    [SerializeField] TextMeshProUGUI memoryEntryPrefab;

    const int MaxHearts = 10;
    readonly List<GameObject> _memoryEntries = new();

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Show(string npcName, string color, int relationship, string mood, List<string> memories)
    {
        if (panel != null && panel.activeSelf) return;
        if (panel != null) panel.SetActive(true);

        if (npcNameText != null)
        {
            string nameHex = !string.IsNullOrEmpty(color) ? color : "#ffe8aa";
            npcNameText.color = Color.white;
            npcNameText.text = $"<b><color={nameHex}>{npcName}</color></b>";
        }

        if (relationshipText != null)
        {
            relationshipText.color = Color.white;
            int hearts = Mathf.Clamp(Mathf.RoundToInt((relationship + 100f) / 200f * MaxHearts), 0, MaxHearts);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < MaxHearts; i++)
                sb.Append(i < hearts ? "<color=#ff6655>\u2665</color>" : "<color=#444444>\u2665</color>");
            relationshipText.text = sb.ToString();
        }

        if (moodText != null)
        {
            moodText.color = Color.white;
            var (emoji, col) = mood switch
            {
                "Happy"    => (":)", "#66ff88"),
                "Angry"    => (">:(", "#ff6655"),
                "Scared"   => (":O", "#ffdd44"),
                "Grateful" => (":D", "#88ddff"),
                _          => (":|", "#aaaaaa")
            };
            moodText.text = $"<color=#888888>Mood:</color> <color={col}><b>{emoji} {mood}</b></color>";
        }

        RefreshMemories(memories);
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    void RefreshMemories(List<string> memories)
    {
        ClearMemories();
        if (memoriesContent == null || memoryEntryPrefab == null || memories == null) return;

        foreach (string mem in memories)
        {
            var entry = Instantiate(memoryEntryPrefab, memoriesContent);
            entry.text = $"<color=#6677aa>\u2022</color> <color=#bbbbcc>{mem}</color>";
            entry.color = Color.white;
            var fitter = entry.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null) fitter = entry.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            entry.gameObject.SetActive(true);
            _memoryEntries.Add(entry.gameObject);
        }
    }

    void ClearMemories()
    {
        foreach (var go in _memoryEntries)
            Destroy(go);
        _memoryEntries.Clear();
    }
}
