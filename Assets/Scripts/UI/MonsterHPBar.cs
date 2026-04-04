using UnityEngine;
using UnityEngine.UI;

public class MonsterHPBar : MonoBehaviour
{
    Image _fillImage;
    CanvasGroup _canvasGroup;
    Transform _target;
    float _offsetY = 0.8f;
    float _lastDamageTime;
    int _maxHp;
    bool _visible;

    const float HideDelay = 3f;
    const float FadeSpeed = 2f;

    static readonly Color ColorGreen = new(0.333f, 0.933f, 0.533f);  // #55ee88
    static readonly Color ColorYellow = new(1f, 0.867f, 0.267f);     // #ffdd44
    static readonly Color ColorRed = new(1f, 0.4f, 0.333f);          // #ff6655

    public static MonsterHPBar Create(Transform target, int maxHp)
    {
        var go = new GameObject("MonsterHPBar");

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1f, 0.15f);

        var cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(go.transform, false);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(go.transform, false);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = ColorGreen;
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillAmount = 1f;
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;

        var hpBar = go.AddComponent<MonsterHPBar>();
        hpBar._fillImage = fillImg;
        hpBar._canvasGroup = cg;
        hpBar._target = target;
        hpBar._maxHp = maxHp;
        return hpBar;
    }

    public void UpdateHP(int currentHp, int maxHp)
    {
        _maxHp = maxHp;
        float ratio = Mathf.Clamp01((float)currentHp / maxHp);
        if (_fillImage != null)
        {
            _fillImage.fillAmount = ratio;
            _fillImage.color = ratio > 0.5f ? ColorGreen : ratio > 0.25f ? ColorYellow : ColorRed;
        }
        _visible = ratio < 1f;
        _lastDamageTime = Time.time;
    }

    void LateUpdate()
    {
        if (_target == null) { Destroy(gameObject); return; }

        transform.position = (Vector2)_target.position + Vector2.up * _offsetY;

        float targetAlpha;
        if (!_visible)
            targetAlpha = 0f;
        else if (Time.time - _lastDamageTime > HideDelay)
            targetAlpha = 0f;
        else
            targetAlpha = 1f;

        if (_canvasGroup != null)
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, FadeSpeed * Time.deltaTime);
    }
}
