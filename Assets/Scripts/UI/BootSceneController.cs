using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class BootSceneController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI splashText;
    [SerializeField] Image logoImage;
    [SerializeField] CanvasGroup fadeGroup;
    [SerializeField] float splashDuration = 1.5f;

    void Start()
    {
        if (fadeGroup != null) fadeGroup.alpha = 0f;
        SetupLogo();
        StartCoroutine(BootSequence());
    }

    void SetupLogo()
    {
        if (logoImage != null) return;
        var canvas = GetComponentInChildren<Canvas>();
        if (canvas == null) return;

        var spr = Resources.Load<Sprite>("Sprites/UI/boot_logo");
        if (spr == null) return;

        var go = new GameObject("Logo");
        go.transform.SetParent(canvas.transform, false);
        logoImage = go.AddComponent<Image>();
        logoImage.sprite = spr;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.35f);
        rt.anchorMax = new Vector2(0.8f, 0.75f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
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
