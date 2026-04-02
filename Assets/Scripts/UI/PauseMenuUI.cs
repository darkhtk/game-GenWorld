using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class PauseMenuUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] GameObject panel;
    [SerializeField] Image overlayImage;

    [Header("Buttons")]
    [SerializeField] Button resumeButton;
    [SerializeField] Button saveButton;
    [SerializeField] Button mainMenuButton;

    [Header("Feedback")]
    [SerializeField] TextMeshProUGUI saveConfirmText;

    [Header("Settings")]
    [SerializeField] Slider bgmVolumeSlider;
    [SerializeField] Slider sfxVolumeSlider;
    [SerializeField] Toggle fullscreenToggle;
    [SerializeField] TMP_Dropdown resolutionDropdown;

    public Action OnSaveRequested;
    public Action OnMainMenuRequested;

    float _previousTimeScale;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (saveConfirmText != null) saveConfirmText.gameObject.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Close);
        if (saveButton != null) saveButton.onClick.AddListener(DoSave);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMainMenu);
        InitSettings();
    }

    public void Open()
    {
        if (panel == null) return;
        panel.SetActive(true);
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        Time.timeScale = _previousTimeScale > 0 ? _previousTimeScale : 1f;
        if (saveConfirmText != null) saveConfirmText.gameObject.SetActive(false);
    }

    public void Toggle()
    {
        if (panel == null) return;
        if (panel.activeSelf) Close(); else Open();
    }

    Coroutine _saveConfirmCoroutine;

    void DoSave()
    {
        OnSaveRequested?.Invoke();
        if (saveConfirmText != null)
        {
            saveConfirmText.gameObject.SetActive(true);
            saveConfirmText.text = "Saved!";
            if (_saveConfirmCoroutine != null) StopCoroutine(_saveConfirmCoroutine);
            _saveConfirmCoroutine = StartCoroutine(HideSaveConfirmAfterDelay());
        }
    }

    IEnumerator HideSaveConfirmAfterDelay()
    {
        yield return new WaitForSecondsRealtime(2f);
        if (saveConfirmText != null) saveConfirmText.gameObject.SetActive(false);
        _saveConfirmCoroutine = null;
    }

    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        OnMainMenuRequested?.Invoke();
    }

    void InitSettings()
    {
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.value = PlayerPrefs.GetFloat("bgm_volume", 0.5f);
            bgmVolumeSlider.onValueChanged.AddListener(v =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.SetBGMVolume(v);
            });
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = PlayerPrefs.GetFloat("sfx_volume", 0.7f);
            sfxVolumeSlider.onValueChanged.AddListener(v =>
            {
                if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v);
            });
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(v => Screen.fullScreen = v);
        }
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string> { "1024x768", "1280x720", "1920x1080" };
            resolutionDropdown.AddOptions(options);
            resolutionDropdown.onValueChanged.AddListener(idx =>
            {
                int[] w = { 1024, 1280, 1920 };
                int[] h = { 768, 720, 1080 };
                if (idx < w.Length) Screen.SetResolution(w[idx], h[idx], Screen.fullScreen);
            });
        }
    }
}
