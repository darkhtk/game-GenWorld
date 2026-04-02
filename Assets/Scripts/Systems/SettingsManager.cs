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
}
