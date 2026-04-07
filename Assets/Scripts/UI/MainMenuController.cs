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

    TextMeshProUGUI _newGameLabel;
    bool _awaitingNewGameConfirm;

    void Awake()
    {
        SetupBackground();

        if (titleText != null)
        {
            titleText.text = "<b><color=#ffd900>Gen</color><color=#ff9944>World</color></b>";
            titleText.color = Color.white;
        }

        if (newGameButton != null)
        {
            _newGameLabel = newGameButton.GetComponentInChildren<TextMeshProUGUI>();
            newGameButton.onClick.AddListener(OnNewGame);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(() => { CancelNewGameConfirm(); OnContinue(); });
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
        if (SaveSystem.HasSave() && !_awaitingNewGameConfirm)
        {
            _awaitingNewGameConfirm = true;
            if (_newGameLabel != null)
                _newGameLabel.text = "<color=#ff6655>OVERWRITE SAVE?</color>";
            AudioManager.Instance?.PlaySFX("sfx_error");
            return;
        }

        AudioManager.Instance?.PlaySFX("sfx_confirm");
        if (SaveSystem.HasSave())
            SaveSystem.DeleteSave();
        SceneManager.LoadScene("GameScene");
    }

    void CancelNewGameConfirm()
    {
        if (!_awaitingNewGameConfirm) return;
        _awaitingNewGameConfirm = false;
        if (_newGameLabel != null)
            _newGameLabel.text = "New Game";
    }

    void OnContinue()
    {
        if (!SaveSystem.HasSave()) return;
        AudioManager.Instance?.PlaySFX("sfx_confirm");
        SceneManager.LoadScene("GameScene");
    }
}
