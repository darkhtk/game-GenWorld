using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFlash : MonoBehaviour
{
    [SerializeField] Image flashImage;

    static ScreenFlash _instance;
    Coroutine _activeFlash;

    static readonly Color DamageColor = new(1f, 0f, 0f, 0.3f);                   // intense red (intentional alarm)
    static readonly Color HealColor = new(0.4f, 1f, 0.533f, 0.25f);              // #66ff88
    static readonly Color LevelUpColor = new(1f, 0.851f, 0f, 0.35f);             // #ffd900
    static readonly Color DodgeColor = new(0.533f, 0.867f, 1f, 0.2f);            // #88ddff

    void Awake()
    {
        _instance = this;
        if (flashImage != null)
        {
            flashImage.color = Color.clear;
            flashImage.raycastTarget = false;
        }
    }

    public static void Damage() => Flash(DamageColor, 0.3f);
    public static void Heal() => Flash(HealColor, 0.4f);
    public static void LevelUp() => Flash(LevelUpColor, 0.6f);
    public static void Dodge() => Flash(DodgeColor, 0.15f);

    public static void Flash(Color color, float duration)
    {
        if (_instance == null || _instance.flashImage == null) return;
        if (_instance._activeFlash != null)
            _instance.StopCoroutine(_instance._activeFlash);
        _instance._activeFlash = _instance.StartCoroutine(_instance.DoFlash(color, duration));
    }

    IEnumerator DoFlash(Color color, float duration)
    {
        flashImage.color = color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            flashImage.color = new Color(color.r, color.g, color.b, color.a * (1f - t));
            yield return null;
        }
        flashImage.color = Color.clear;
        _activeFlash = null;
    }
}
