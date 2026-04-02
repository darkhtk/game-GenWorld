using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementUI : MonoBehaviour
{
    [Header("Achievement List")]
    [SerializeField] GameObject listPanel;
    [SerializeField] Transform listContent;
    [SerializeField] TextMeshProUGUI listEntryPrefab;

    [Header("Unlock Popup")]
    [SerializeField] GameObject popupPanel;
    [SerializeField] TextMeshProUGUI popupName;
    [SerializeField] TextMeshProUGUI popupReward;
    [SerializeField] CanvasGroup popupCanvasGroup;

    readonly List<TextMeshProUGUI> _listEntries = new();

    void Awake()
    {
        if (listPanel != null) listPanel.SetActive(false);
        if (popupPanel != null) popupPanel.SetActive(false);
        EventBus.On<AchievementUnlockedEvent>(OnUnlocked);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (listPanel != null)
            {
                bool show = !listPanel.activeSelf;
                listPanel.SetActive(show);
                if (show) RefreshList();
            }
        }
    }

    void RefreshList()
    {
        var gm = GameManager.Instance;
        if (gm?.Achievements == null || listContent == null || listEntryPrefab == null) return;

        var all = gm.Achievements.GetAll();

        while (_listEntries.Count < all.Length)
        {
            var entry = Instantiate(listEntryPrefab, listContent);
            entry.gameObject.SetActive(true);
            _listEntries.Add(entry);
        }

        for (int i = 0; i < _listEntries.Count; i++)
        {
            if (i < all.Length)
            {
                var def = all[i];
                var (current, required) = gm.Achievements.GetProgress(def.id);
                bool done = gm.Achievements.IsCompleted(def.id);
                string icon = done ? "\u2705" : "\ud83d\udd32";
                _listEntries[i].text = $"{icon} {def.name} — {current}/{required}";
                _listEntries[i].color = done ? new Color(0.6f, 1f, 0.6f) : Color.white;
                _listEntries[i].gameObject.SetActive(true);
            }
            else
            {
                _listEntries[i].gameObject.SetActive(false);
            }
        }
    }

    void OnUnlocked(AchievementUnlockedEvent e)
    {
        if (popupPanel == null) return;
        if (popupName != null) popupName.text = e.name;
        if (popupReward != null) popupReward.text = !string.IsNullOrEmpty(e.rewardDesc) ? $"Reward: {e.rewardDesc}" : "";
        StartCoroutine(ShowPopup());
    }

    IEnumerator ShowPopup()
    {
        popupPanel.SetActive(true);
        if (popupCanvasGroup != null) popupCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(3f);

        if (popupCanvasGroup != null)
        {
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                popupCanvasGroup.alpha = 1f - t / 0.5f;
                yield return null;
            }
        }
        popupPanel.SetActive(false);
    }
}
