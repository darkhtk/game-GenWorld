using System;
using UnityEngine;

public static class SteamCloudStorage
{
    public static bool SaveToCloud(string fileName, byte[] data)
    {
#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED
        if (SteamManager.Instance == null || !SteamManager.Instance.Initialized) return false;
        try
        {
            return Steamworks.SteamRemoteStorage.FileWrite(fileName, data, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SteamCloud] Save failed: {e.Message}");
            return false;
        }
#else
        return false;
#endif
    }

    public static byte[] LoadFromCloud(string fileName)
    {
#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED
        if (SteamManager.Instance == null || !SteamManager.Instance.Initialized) return null;
        try
        {
            int size = Steamworks.SteamRemoteStorage.GetFileSize(fileName);
            if (size <= 0) return null;
            byte[] data = new byte[size];
            Steamworks.SteamRemoteStorage.FileRead(fileName, data, size);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SteamCloud] Load failed: {e.Message}");
            return null;
        }
#else
        return null;
#endif
    }

    public static bool CloudFileExists(string fileName)
    {
#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED
        if (SteamManager.Instance == null || !SteamManager.Instance.Initialized) return false;
        return Steamworks.SteamRemoteStorage.FileExists(fileName);
#else
        return false;
#endif
    }

    public static bool IsAvailable =>
        SteamManager.Instance != null && SteamManager.Instance.Initialized;
}
