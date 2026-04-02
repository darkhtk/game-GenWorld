using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Button closeButton;

    [Header("Tabs")]
    [SerializeField] Button graphicsTabButton;
    [SerializeField] Button audioTabButton;
    [SerializeField] Button controlsTabButton;
    [SerializeField] GameObject graphicsTab;
    [SerializeField] GameObject audioTab;
    [SerializeField] GameObject controlsTab;

    [Header("Graphics")]
    [SerializeField] TMP_Dropdown resolutionDropdown;
    [SerializeField] Toggle fullscreenToggle;

    [Header("Audio")]
    [SerializeField] Slider masterVolumeSlider;
    [SerializeField] Slider bgmVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;

    [Header("Controls")]
    [SerializeField] Toggle autoPotionToggle;

    [Header("Buttons")]
    [SerializeField] Button applyButton;
    [SerializeField] Button resetButton;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (graphicsTabButton != null) graphicsTabButton.onClick.AddListener(() => SwitchTab(0));
        if (audioTabButton != null) audioTabButton.onClick.AddListener(() => SwitchTab(1));
        if (controlsTabButton != null) controlsTabButton.onClick.AddListener(() => SwitchTab(2));
        if (applyButton != null) applyButton.onClick.AddListener(Apply);
        if (resetButton != null) resetButton.onClick.AddListener(ResetDefaults);
    }

    public void Open()
    {
        if (panel == null) return;
        panel.SetActive(true);
        LoadCurrent();
        SwitchTab(0);
    }

    public void Close() { if (panel != null) panel.SetActive(false); }

    public void SwitchTab(int idx)
    {
        if (graphicsTab != null) graphicsTab.SetActive(idx == 0);
        if (audioTab != null) audioTab.SetActive(idx == 1);
        if (controlsTab != null) controlsTab.SetActive(idx == 2);
    }

    void LoadCurrent()
    {
        if (resolutionDropdown != null) resolutionDropdown.value = SettingsManager.ResolutionIndex;
        if (fullscreenToggle != null) fullscreenToggle.isOn = SettingsManager.Fullscreen;
        if (masterVolumeSlider != null) masterVolumeSlider.value = SettingsManager.MasterVolume;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = SettingsManager.BGMVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = SettingsManager.SFXVolume;
        if (autoPotionToggle != null) autoPotionToggle.isOn = SettingsManager.AutoPotion;
    }

    public void Apply()
    {
        if (resolutionDropdown != null) SettingsManager.ResolutionIndex = resolutionDropdown.value;
        if (fullscreenToggle != null) SettingsManager.Fullscreen = fullscreenToggle.isOn;
        if (masterVolumeSlider != null) SettingsManager.MasterVolume = masterVolumeSlider.value;
        if (bgmVolumeSlider != null) SettingsManager.BGMVolume = bgmVolumeSlider.value;
        if (sfxVolumeSlider != null) SettingsManager.SFXVolume = sfxVolumeSlider.value;
        if (autoPotionToggle != null) SettingsManager.AutoPotion = autoPotionToggle.isOn;
        SettingsManager.SaveAll();

        var gm = GameManager.Instance;
        if (gm != null) gm.AutoPotionEnabled = SettingsManager.AutoPotion;

        Close();
    }

    public void ResetDefaults()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = 0.5f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0.7f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (resolutionDropdown != null) resolutionDropdown.value = 2;
        if (autoPotionToggle != null) autoPotionToggle.isOn = true;
    }
}
