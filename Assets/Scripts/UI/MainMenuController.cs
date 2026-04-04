using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] Button newGameButton;
    [SerializeField] Button continueButton;
    [SerializeField] Button settingsButton;
    [SerializeField] SettingsUI settingsUI;
    [SerializeField] TextMeshProUGUI titleText;

    void Awake()
    {
        SetupBackground();

        if (titleText != null)
        {
            titleText.text = "<b><color=#ffd900>Gen</color><color=#ff9944>World</color></b>";
            titleText.color = Color.white;
        }

        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGame);

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinue);
            continueButton.interactable = SaveSystem.HasSave();
        }

        if (settingsButton != null && settingsUI != null)
            settingsButton.onClick.AddListener(() => settingsUI.Open());
    }

    void SetupBackground()
    {
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = GetComponent<Canvas>();
        if (canvas == null) return;

        var bgSprite = Resources.Load<Sprite>("Sprites/UI/main_menu_bg");
        if (bgSprite == null) return;

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvas.transform, false);
        bgGo.transform.SetAsFirstSibling();

        var img = bgGo.AddComponent<Image>();
        img.sprite = bgSprite;
        img.preserveAspect = false;
        img.raycastTarget = false;
        img.color = Color.white;

        var rt = bgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void OnNewGame()
    {
        AudioManager.Instance?.PlaySFX("sfx_confirm");
        if (SaveSystem.HasSave())
            SaveSystem.DeleteSave();
        SceneManager.LoadScene("GameScene");
    }

    void OnContinue()
    {
        if (!SaveSystem.HasSave()) return;
        AudioManager.Instance?.PlaySFX("sfx_confirm");
        SceneManager.LoadScene("GameScene");
    }
}
