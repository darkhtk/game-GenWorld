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
}
