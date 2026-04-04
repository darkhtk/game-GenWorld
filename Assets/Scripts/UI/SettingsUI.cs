using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] Transform keyBindingsContainer;
    [SerializeField] GameObject keyBindingRowPrefab;
    [SerializeField] Toggle autoPotionToggle;

    [Header("Buttons")]
    [SerializeField] Button applyButton;
    [SerializeField] Button cancelButton;
    [SerializeField] Button resetButton;

    [Header("Resolution Confirm")]
    [SerializeField] GameObject confirmPopup;
    [SerializeField] TextMeshProUGUI confirmText;
    [SerializeField] Button confirmYes;
    [SerializeField] Button confirmNo;

    // Pending settings (saved on Open, restored on Cancel)
    float _savedMaster, _savedBgm, _savedSfx;
    bool _savedFullscreen, _savedAutoPotion;
    int _savedResIndex;

    // Resolution mapping
    readonly List<(int w, int h)> _resolutions = new();

    // Key rebinding state
    readonly List<KeyBindRow> _keyRows = new();
    KeyBindRow _listeningRow;
    Coroutine _confirmCoroutine;

    void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (confirmPopup != null) confirmPopup.SetActive(false);

        if (closeButton != null) closeButton.onClick.AddListener(Cancel);
        if (graphicsTabButton != null) graphicsTabButton.onClick.AddListener(() => SwitchTab(0));
        if (audioTabButton != null) audioTabButton.onClick.AddListener(() => SwitchTab(1));
        if (controlsTabButton != null) controlsTabButton.onClick.AddListener(() => SwitchTab(2));
        if (applyButton != null) applyButton.onClick.AddListener(Apply);
        if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        if (resetButton != null) resetButton.onClick.AddListener(ResetDefaults);
    }

    public void Open()
    {
        if (panel == null) return;
        panel.SetActive(true);

        // Save current values for Cancel
        _savedMaster = SettingsManager.MasterVolume;
        _savedBgm = SettingsManager.BGMVolume;
        _savedSfx = SettingsManager.SFXVolume;
        _savedFullscreen = SettingsManager.Fullscreen;
        _savedResIndex = SettingsManager.ResolutionIndex;
        _savedAutoPotion = SettingsManager.AutoPotion;

        PopulateResolutions();
        LoadCurrent();
        BuildKeyBindingRows();
        SwitchTab(0);
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        _listeningRow = null;
        if (_confirmCoroutine != null) { StopCoroutine(_confirmCoroutine); _confirmCoroutine = null; }
        if (confirmPopup != null) confirmPopup.SetActive(false);
    }

    public void SwitchTab(int idx)
    {
        if (graphicsTab != null) graphicsTab.SetActive(idx == 0);
        if (audioTab != null) audioTab.SetActive(idx == 1);
        if (controlsTab != null) controlsTab.SetActive(idx == 2);
        SetTabStyle(graphicsTabButton, idx == 0);
        SetTabStyle(audioTabButton, idx == 1);
        SetTabStyle(controlsTabButton, idx == 2);
    }

    void SetTabStyle(Button btn, bool active)
    {
        if (btn == null) return;
        var label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label == null) return;
        label.color = active ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        label.fontStyle = active ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
    }

    void PopulateResolutions()
    {
        _resolutions.Clear();
        var seen = new HashSet<(int, int)>();

        // Add common resolutions
        (int w, int h)[] common = { (1024, 768), (1280, 720), (1366, 768), (1600, 900), (1920, 1080), (2560, 1440) };
        foreach (var r in common)
        {
            if (seen.Add(r)) _resolutions.Add(r);
        }

        // Add monitor resolutions
        foreach (var r in Screen.resolutions)
        {
            var key = (r.width, r.height);
            if (seen.Add(key)) _resolutions.Add(key);
        }

        _resolutions.Sort((a, b) => a.w != b.w ? a.w.CompareTo(b.w) : a.h.CompareTo(b.h));

        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            var options = new List<string>();
            foreach (var r in _resolutions)
                options.Add($"{r.w} x {r.h}");
            resolutionDropdown.AddOptions(options);
        }
    }

    int FindResolutionIndex(int savedIdx)
    {
        // Map old fixed index to new dynamic list
        (int w, int h) target = savedIdx switch
        {
            0 => (1024, 768), 1 => (1280, 720), 2 => (1920, 1080), 3 => (2560, 1440),
            _ => (1920, 1080)
        };
        for (int i = 0; i < _resolutions.Count; i++)
            if (_resolutions[i].w == target.w && _resolutions[i].h == target.h) return i;
        return _resolutions.Count > 0 ? _resolutions.Count - 1 : 0;
    }

    void LoadCurrent()
    {
        if (resolutionDropdown != null)
            resolutionDropdown.value = FindResolutionIndex(_savedResIndex);
        if (fullscreenToggle != null) fullscreenToggle.isOn = _savedFullscreen;
        if (masterVolumeSlider != null) masterVolumeSlider.value = _savedMaster;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = _savedBgm;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = _savedSfx;
        if (autoPotionToggle != null) autoPotionToggle.isOn = _savedAutoPotion;
    }

    public void Apply()
    {
        // Audio
        if (masterVolumeSlider != null) SettingsManager.MasterVolume = masterVolumeSlider.value;
        if (bgmVolumeSlider != null) SettingsManager.BGMVolume = bgmVolumeSlider.value;
        if (sfxVolumeSlider != null) SettingsManager.SFXVolume = sfxVolumeSlider.value;
        if (fullscreenToggle != null) SettingsManager.Fullscreen = fullscreenToggle.isOn;
        if (autoPotionToggle != null) SettingsManager.AutoPotion = autoPotionToggle.isOn;

        var gm = GameManager.Instance;
        if (gm != null) gm.AutoPotionEnabled = SettingsManager.AutoPotion;

        // Save key bindings
        foreach (var row in _keyRows)
            SettingsManager.SetKey(row.Action, row.CurrentKey);

        // Resolution — needs confirmation
        int resIdx = resolutionDropdown != null ? resolutionDropdown.value : 0;
        bool resChanged = false;
        if (resIdx >= 0 && resIdx < _resolutions.Count)
        {
            var res = _resolutions[resIdx];
            if (res.w != Screen.currentResolution.width || res.h != Screen.currentResolution.height)
            {
                Screen.SetResolution(res.w, res.h, SettingsManager.Fullscreen);
                resChanged = true;
            }
        }

        SettingsManager.SaveAll();

        // Update saved values so Cancel won't revert
        _savedMaster = SettingsManager.MasterVolume;
        _savedBgm = SettingsManager.BGMVolume;
        _savedSfx = SettingsManager.SFXVolume;
        _savedFullscreen = SettingsManager.Fullscreen;
        _savedAutoPotion = SettingsManager.AutoPotion;

        if (resChanged)
            ShowResolutionConfirm(resIdx);
        else
            Close();
    }

    public void Cancel()
    {
        // Restore saved values
        SettingsManager.MasterVolume = _savedMaster;
        SettingsManager.BGMVolume = _savedBgm;
        SettingsManager.SFXVolume = _savedSfx;
        SettingsManager.Fullscreen = _savedFullscreen;
        SettingsManager.AutoPotion = _savedAutoPotion;
        Close();
    }

    public void ResetDefaults()
    {
        if (masterVolumeSlider != null) masterVolumeSlider.value = 1f;
        if (bgmVolumeSlider != null) bgmVolumeSlider.value = 0.5f;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = 0.7f;
        if (fullscreenToggle != null) fullscreenToggle.isOn = true;
        if (autoPotionToggle != null) autoPotionToggle.isOn = true;
        if (resolutionDropdown != null) resolutionDropdown.value = FindResolutionIndex(2);
        SettingsManager.ResetKeyBindings();
        BuildKeyBindingRows();
    }

    // --- Resolution confirmation ---

    void ShowResolutionConfirm(int newResIdx)
    {
        if (confirmPopup == null) { Close(); return; }
        _savedResIndex = newResIdx;
        confirmPopup.SetActive(true);
        _confirmCoroutine = StartCoroutine(ResolutionCountdown());
    }

    IEnumerator ResolutionCountdown()
    {
        int remaining = 15;
        if (confirmYes != null)
        {
            confirmYes.onClick.RemoveAllListeners();
            confirmYes.onClick.AddListener(() =>
            {
                confirmPopup.SetActive(false);
                if (_confirmCoroutine != null) StopCoroutine(_confirmCoroutine);
                _confirmCoroutine = null;
                Close();
            });
        }
        if (confirmNo != null)
        {
            confirmNo.onClick.RemoveAllListeners();
            confirmNo.onClick.AddListener(RevertResolution);
        }

        while (remaining > 0)
        {
            if (confirmText != null)
            {
                confirmText.color = Color.white;
                confirmText.text = $"<color=#cccccc>Keep this resolution? Reverting in</color> <color=#ff9944><b>{remaining}s</b></color><color=#cccccc>...</color>";
            }
            yield return new WaitForSecondsRealtime(1f);
            remaining--;
        }

        RevertResolution();
    }

    void RevertResolution()
    {
        if (_confirmCoroutine != null) { StopCoroutine(_confirmCoroutine); _confirmCoroutine = null; }
        if (confirmPopup != null) confirmPopup.SetActive(false);

        // Revert to previous resolution
        SettingsManager.ResolutionIndex = _savedResIndex;
        if (resolutionDropdown != null)
            resolutionDropdown.value = FindResolutionIndex(_savedResIndex);
        Close();
    }

    // --- Key Binding UI ---

    void BuildKeyBindingRows()
    {
        foreach (var row in _keyRows)
            if (row.Go != null) Destroy(row.Go);
        _keyRows.Clear();
        _listeningRow = null;

        var bindings = SettingsManager.GetDefaultBindings();
        foreach (var b in bindings)
        {
            KeyCode current = SettingsManager.GetKey(b.action);
            var row = new KeyBindRow { Action = b.action, CurrentKey = current };

            if (keyBindingsContainer != null && keyBindingRowPrefab != null)
            {
                row.Go = Instantiate(keyBindingRowPrefab, keyBindingsContainer);
                var texts = row.Go.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    row.LabelText = texts[0];
                    row.KeyText = texts[1];
                }
                else if (texts.Length == 1)
                {
                    row.KeyText = texts[0];
                }

                if (row.LabelText != null) { row.LabelText.color = Color.white; row.LabelText.text = FormatActionName(b.action); }
                if (row.KeyText != null) { row.KeyText.color = Color.white; row.KeyText.text = FormatKeyName(current); }

                var btn = row.Go.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    var captured = row;
                    btn.onClick.AddListener(() => StartListening(captured));
                }
            }
            _keyRows.Add(row);
        }
    }

    void StartListening(KeyBindRow row)
    {
        _listeningRow = row;
        if (row.KeyText != null)
        {
            row.KeyText.text = "<color=#ffdd44><b>[PRESS KEY]</b></color>";
            row.KeyText.color = Color.white;
        }
    }

    void Update()
    {
        if (_listeningRow == null) return;

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (key == KeyCode.Escape || key == KeyCode.None) continue;
            if ((int)key >= 330) continue; // Skip joystick buttons
            if (!Input.GetKeyDown(key)) continue;

            // Duplicate check
            bool dup = false;
            foreach (var r in _keyRows)
            {
                if (r != _listeningRow && r.CurrentKey == key)
                {
                    r.CurrentKey = KeyCode.None;
                    if (r.KeyText != null) { r.KeyText.text = "<color=#888888>---</color>"; r.KeyText.color = Color.white; }
                    dup = true;
                }
            }

            _listeningRow.CurrentKey = key;
            if (_listeningRow.KeyText != null)
            {
                string keyText = FormatKeyName(key);
                _listeningRow.KeyText.text = dup ? keyText.Replace("#88ddff", "#ff9944") : keyText;
                _listeningRow.KeyText.color = Color.white;
            }
            _listeningRow = null;
            return;
        }
    }

    static string FormatActionName(string action)
    {
        string label = action switch
        {
            "move_up" => "Move Up",
            "move_down" => "Move Down",
            "move_left" => "Move Left",
            "move_right" => "Move Right",
            "dodge" => "Dodge",
            "interact" => "Interact",
            "inventory" => "Inventory",
            "skills" => "Skill Tree",
            "quests" => "Quests",
            "hp_potion" => "HP Potion",
            "mp_potion" => "MP Potion",
            "minimap" => "Minimap",
            _ => action
        };
        return $"<color=#aaaaaa>{label}</color>";
    }

    static string FormatKeyName(KeyCode key)
    {
        if (key == KeyCode.None) return "<color=#888888>---</color>";
        string name = key switch
        {
            KeyCode.Space => "Space",
            KeyCode.Tab => "Tab",
            KeyCode.LeftShift => "LShift",
            KeyCode.RightShift => "RShift",
            KeyCode.LeftControl => "LCtrl",
            _ => key.ToString()
        };
        return $"<color=#88ddff><b>{name}</b></color>";
    }

    class KeyBindRow
    {
        public string Action;
        public KeyCode CurrentKey;
        public GameObject Go;
        public TextMeshProUGUI LabelText;
        public TextMeshProUGUI KeyText;
    }
}
