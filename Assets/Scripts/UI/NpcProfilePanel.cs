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
            npcNameText.text = npcName;
            if (!string.IsNullOrEmpty(color) && ColorUtility.TryParseHtmlString(color, out var c))
                npcNameText.color = c;
        }

        if (relationshipText != null)
        {
            int hearts = Mathf.Clamp(Mathf.RoundToInt((relationship + 100f) / 200f * MaxHearts), 0, MaxHearts);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < MaxHearts; i++)
                sb.Append(i < hearts ? "<color=#ff4444>H</color>" : "<color=#666666>H</color>");
            relationshipText.text = sb.ToString();
        }

        if (moodText != null)
        {
            string emoji = mood switch
            {
                "Happy" => ":)",
                "Angry" => ">:(",
                "Scared" => ":O",
                "Grateful" => ":D",
                _ => ":|"
            };
            moodText.text = $"Mood: {emoji} {mood}";
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
            entry.text = $"- {mem}";
            entry.color = new Color(0.7f, 0.7f, 0.8f);
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
