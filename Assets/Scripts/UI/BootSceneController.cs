using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class BootSceneController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI splashText;
    [SerializeField] CanvasGroup fadeGroup;
    [SerializeField] float splashDuration = 1.5f;

    void Start()
    {
        if (fadeGroup != null) fadeGroup.alpha = 0f;
        StartCoroutine(BootSequence());
    }

    IEnumerator BootSequence()
    {
        // Fade in
        if (fadeGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Clamp01(elapsed / 0.5f);
                yield return null;
            }
            fadeGroup.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(splashDuration);

        // Fade out
        if (fadeGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.3f);
                yield return null;
            }
        }

        SceneManager.LoadScene("MainMenuScene");
    }
}
