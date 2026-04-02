using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class PanelAnimator : MonoBehaviour
{
    [SerializeField] float fadeDuration = 0.15f;

    CanvasGroup _cg;
    Coroutine _anim;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(Fade(0f, 1f));
    }

    public void Hide()
    {
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(FadeOut());
    }

    IEnumerator Fade(float from, float to)
    {
        if (_cg == null) yield break;
        _cg.alpha = from;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        _cg.alpha = to;
        _anim = null;
    }

    IEnumerator FadeOut()
    {
        yield return Fade(1f, 0f);
        gameObject.SetActive(false);
        _anim = null;
    }
}
