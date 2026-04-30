using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// S-122 Phase 1: Reusable hover/click SFX for any UI Button. Attach alongside Button;
// override per-instance via inspector if a button needs a non-default SFX. Reads
// GameConfig.UI for global names/volumes/enabled flag — single source of truth.
//
// Phase 2 (S-148): bulk-attach to existing Button prefabs/scenes. This file alone
// produces no audible change until attached, intentionally — keeps S-122 reviewable
// in isolation and avoids surprise regressions on buttons that already self-trigger
// SFX via OnClick (e.g. PauseMenuUI sfx_menu_close).
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class UIButtonSfx : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Tooltip("Empty = use GameConfig.UI.ButtonHoverSfxName")]
    [SerializeField] string hoverSfxOverride = "";
    [Tooltip("Empty = use GameConfig.UI.ButtonClickSfxName")]
    [SerializeField] string clickSfxOverride = "";
    [Tooltip("Negative = use GameConfig.UI.ButtonHoverSfxVolume")]
    [SerializeField, Range(-1f, 1f)] float hoverVolumeOverride = -1f;
    [Tooltip("Negative = use GameConfig.UI.ButtonClickSfxVolume")]
    [SerializeField, Range(-1f, 1f)] float clickVolumeOverride = -1f;
    [SerializeField] bool playOnHover = true;
    [SerializeField] bool playOnClick = true;

    Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playOnHover) return;
        if (!GameConfig.UI.ButtonSfxEnabled) return;
        if (_button != null && !_button.interactable) return;

        var name = string.IsNullOrEmpty(hoverSfxOverride)
            ? GameConfig.UI.ButtonHoverSfxName
            : hoverSfxOverride;
        var vol = hoverVolumeOverride < 0f
            ? GameConfig.UI.ButtonHoverSfxVolume
            : hoverVolumeOverride;
        AudioManager.Instance?.PlaySFXScaled(name, vol);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!playOnClick) return;
        if (!GameConfig.UI.ButtonSfxEnabled) return;
        if (_button != null && !_button.interactable) return;

        var name = string.IsNullOrEmpty(clickSfxOverride)
            ? GameConfig.UI.ButtonClickSfxName
            : clickSfxOverride;
        var vol = clickVolumeOverride < 0f
            ? GameConfig.UI.ButtonClickSfxVolume
            : clickVolumeOverride;
        AudioManager.Instance?.PlaySFXScaled(name, vol);
    }
}
