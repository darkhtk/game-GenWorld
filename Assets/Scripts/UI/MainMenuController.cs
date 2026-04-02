using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] Button newGameButton;
    [SerializeField] Button continueButton;
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
