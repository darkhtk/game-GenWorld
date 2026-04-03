using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillRowUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] TextMeshProUGUI costText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] Button learnButton;
    [SerializeField] Button selectButton;
    [SerializeField] Image backgroundImage;
    [SerializeField] Image iconImage;

    [Header("Node State Sprites")]
    [SerializeField] Sprite nodeAvailable;
    [SerializeField] Sprite nodeLearned;
    [SerializeField] Sprite nodeLocked;

    static readonly Color MaxedBg    = new(0.6f, 1f, 0.6f);
    static readonly Color LearnedBg  = new(0.6f, 0.8f, 1f);
    static readonly Color LearnableBg = new(1f, 0.9f, 0.5f);
    static readonly Color LockedBg   = new(0.5f, 0.5f, 0.5f);

    public Action<string> OnLearnClicked;
    public Action<string> OnSelected;

    string _skillId;

    public void Setup(SkillDef def, Color treeColor, Sprite icon = null)
    {
        _skillId = def.id;

        if (nameText == null) nameText = FindChildComponent<TextMeshProUGUI>("NameText");
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        if (iconImage == null) iconImage = FindChildComponent<Image>("Icon");

        if (nameText != null)
        {
            nameText.text = def.name;
            nameText.color = treeColor;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.overflowMode = TextOverflowModes.Overflow;
            nameText.enableWordWrapping = false;
        }
        if (descText != null) descText.text = def.description ?? "";
        if (iconImage != null && icon != null) { iconImage.sprite = icon; iconImage.color = Color.white; }

        if (learnButton != null)
            learnButton.onClick.AddListener(() => OnLearnClicked?.Invoke(_skillId));

        if (selectButton != null)
            selectButton.onClick.AddListener(() => OnSelected?.Invoke(_skillId));
    }

    public void UpdateState(int level, bool isMaxed, bool isLearned, bool canLearn,
        int requiredPoints, int requiredLevel, int playerLevel = 0, int playerSkillPoints = 0)
    {
        if (levelText != null)
        {
            levelText.text = isMaxed ? "MAX" : $"Lv.{level}/{GameConfig.SkillMaxLevel}";
            levelText.color = isMaxed ? new Color(1f, 0.84f, 0f) : Color.white;
        }

        if (costText != null)
        {
            if (isMaxed) costText.text = "";
            else if (canLearn) { costText.text = $"{requiredPoints}pt"; costText.color = Color.yellow; }
            else if (playerLevel > 0 && playerLevel < requiredLevel)
            {
                costText.text = $"Req Lv.{requiredLevel}";
                costText.color = new Color(1f, 0.4f, 0.4f);
            }
            else
            {
                costText.text = $"Need {requiredPoints}pt";
                costText.color = new Color(1f, 0.4f, 0.4f);
            }
        }

        if (learnButton != null)
        {
            learnButton.gameObject.SetActive(true);
            learnButton.interactable = canLearn;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = isMaxed ? MaxedBg
                : isLearned ? LearnedBg
                : canLearn ? LearnableBg
                : LockedBg;

            if (nodeLearned != null)
            {
                backgroundImage.sprite = isMaxed || isLearned ? nodeLearned
                    : canLearn ? nodeAvailable
                    : nodeLocked;
            }
        }
    }

    T FindChildComponent<T>(string childName) where T : Component
    {
        var t = transform.Find(childName);
        return t != null ? t.GetComponent<T>() : null;
    }
}
