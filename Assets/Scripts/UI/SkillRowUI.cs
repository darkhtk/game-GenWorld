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

    static readonly Color MaxedBg    = new(0.2f, 0.45f, 0.25f);
    static readonly Color LearnedBg  = new(0.2f, 0.3f, 0.55f);
    static readonly Color LearnableBg = new(0.45f, 0.38f, 0.12f);
    static readonly Color LockedBg   = new(0.22f, 0.22f, 0.25f);

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
            string treeHex = "#" + ColorUtility.ToHtmlStringRGB(treeColor);
            nameText.color = Color.white;
            nameText.text = $"<b><color={treeHex}>{def.name}</color></b>";
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.overflowMode = TextOverflowModes.Overflow;
            nameText.enableWordWrapping = false;
        }
        if (descText != null)
        {
            descText.text = def.description ?? "";
            descText.color = new Color(0.7f, 0.7f, 0.75f);
            descText.fontStyle = TMPro.FontStyles.Italic;
        }
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
            if (isMaxed)
            {
                levelText.text = "<b><color=#ffd900>MAX</color></b>";
                levelText.color = Color.white;
            }
            else if (level > 0)
            {
                levelText.text = $"<color=#55ee88><b>Lv.{level}</b></color><color=#888888>/{GameConfig.SkillMaxLevel}</color>";
                levelText.color = Color.white;
            }
            else
            {
                levelText.text = $"<color=#444444>Lv.0/{GameConfig.SkillMaxLevel}</color>";
                levelText.color = Color.white;
            }
        }

        if (costText != null)
        {
            if (isMaxed) costText.text = "";
            else if (canLearn) { costText.text = $"<b><color=#ffd900>{requiredPoints}pt</color></b>"; costText.color = Color.white; }
            else if (playerLevel > 0 && playerLevel < requiredLevel)
            {
                costText.text = $"<color=#ff6655>Lv.<b>{requiredLevel}</b>+</color>";
                costText.color = Color.white;
            }
            else
            {
                int deficit = requiredPoints - playerSkillPoints;
                costText.text = $"<color=#ff6655><b>-{deficit}pt</b></color>";
                costText.color = Color.white;
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
                backgroundImage.type = UnityEngine.UI.Image.Type.Sliced;
            }
        }
    }

    T FindChildComponent<T>(string childName) where T : Component
    {
        var t = transform.Find(childName);
        return t != null ? t.GetComponent<T>() : null;
    }
}
