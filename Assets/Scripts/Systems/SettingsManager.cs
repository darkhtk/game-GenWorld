using UnityEngine;

public static class SettingsManager
{
    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat("master_volume", 1f);
        set { PlayerPrefs.SetFloat("master_volume", value); Apply(); }
    }
    public static float BGMVolume
    {
        get => PlayerPrefs.GetFloat("bgm_volume", 0.5f);
        set { PlayerPrefs.SetFloat("bgm_volume", value); Apply(); }
    }
    public static float SFXVolume
    {
        get => PlayerPrefs.GetFloat("sfx_volume", 0.7f);
        set { PlayerPrefs.SetFloat("sfx_volume", value); Apply(); }
    }
    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt("fullscreen", 1) == 1;
        set { PlayerPrefs.SetInt("fullscreen", value ? 1 : 0); Screen.fullScreen = value; }
    }
    public static int ResolutionIndex
    {
        get => PlayerPrefs.GetInt("resolution_index", 2);
        set
        {
            PlayerPrefs.SetInt("resolution_index", value);
            var res = GetResolution(value);
            Screen.SetResolution(res.w, res.h, Fullscreen);
        }
    }
    public static bool AutoPotion
    {
        get => PlayerPrefs.GetInt("auto_potion", 1) == 1;
        set { PlayerPrefs.SetInt("auto_potion", value ? 1 : 0); }
    }

    static (int w, int h) GetResolution(int idx) => idx switch
    {
        0 => (1024, 768),
        1 => (1280, 720),
        2 => (1920, 1080),
        3 => (2560, 1440),
        _ => (1920, 1080)
    };

    static void Apply()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(MasterVolume);
            AudioManager.Instance.SetBGMVolume(BGMVolume);
            AudioManager.Instance.SetSFXVolume(SFXVolume);
        }
    }

    public static void SaveAll() => PlayerPrefs.Save();

    // Key Bindings
    static readonly (string action, KeyCode defaultKey)[] DefaultBindings = {
        ("move_up", KeyCode.W),
        ("move_down", KeyCode.S),
        ("move_left", KeyCode.A),
        ("move_right", KeyCode.D),
        ("dodge", KeyCode.Space),
        ("interact", KeyCode.F),
        ("inventory", KeyCode.I),
        ("skills", KeyCode.K),
        ("quests", KeyCode.J),
        ("hp_potion", KeyCode.R),
        ("mp_potion", KeyCode.T),
        ("minimap", KeyCode.Tab),
    };

    public static KeyCode GetKey(string action)
    {
        int saved = PlayerPrefs.GetInt($"key_{action}", -1);
        if (saved >= 0) return (KeyCode)saved;
        foreach (var b in DefaultBindings)
            if (b.action == action) return b.defaultKey;
        return KeyCode.None;
    }

    public static void SetKey(string action, KeyCode key)
    {
        PlayerPrefs.SetInt($"key_{action}", (int)key);
    }

    public static (string action, KeyCode defaultKey)[] GetDefaultBindings() => DefaultBindings;

    public static void ResetKeyBindings()
    {
        foreach (var b in DefaultBindings)
            PlayerPrefs.DeleteKey($"key_{b.action}");
    }
}
