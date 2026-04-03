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
            var fitter = entry.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (fitter == null) fitter = entry.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
            fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            entry.enableWordWrapping = true;
            entry.overflowMode = TMPro.TextOverflowModes.Overflow;
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
                string nameColor = done ? "#66ff66" : "#dddddd";
                string prog = done
                    ? $"<color=#66ff66>\u2713 </color>"
                    : $"<color=#888888>\u25a1 </color>";
                string progText = done
                    ? $"<color=#888888>({current}/{required})</color>"
                    : (current > 0
                        ? $"<color=#ffdd44>{current}<color=#888888>/{required}</color></color>"
                        : $"<color=#888888>0/{required}</color>");
                string descLine = !string.IsNullOrEmpty(def.description)
                    ? $"\n  <color=#555566><size=10>{def.description}</size></color>"
                    : "";
                _listEntries[i].text = $"{prog}<color={nameColor}>{def.name}</color>  {progText}{descLine}";
                _listEntries[i].color = Color.white;
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
        if (popupName != null) { popupName.color = Color.white; popupName.text = $"<color=#ffd900>\u2605</color> <b>{e.name}</b>"; }
        if (popupReward != null) { popupReward.color = Color.white; popupReward.text = !string.IsNullOrEmpty(e.rewardDesc) ? $"<color=#aaffaa>Reward: {e.rewardDesc}</color>" : ""; }
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
