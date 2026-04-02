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

    void OnNewGame()
    {
        if (SaveSystem.HasSave())
            SaveSystem.DeleteSave();
        SceneManager.LoadScene("GameScene");
    }

    void OnContinue()
    {
        if (!SaveSystem.HasSave()) return;
        SceneManager.LoadScene("GameScene");
    }
}
