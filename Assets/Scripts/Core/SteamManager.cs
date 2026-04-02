using UnityEngine;

public struct SteamInitializedEvent { public bool success; }

public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }
    public bool Initialized { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitSteam();
    }

    void InitSteam()
    {
#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED
        try
        {
            if (!Steamworks.SteamAPI.Init())
            {
                Debug.LogWarning("[SteamManager] Steam API init failed — running in offline mode");
                Initialized = false;
                EventBus.Emit(new SteamInitializedEvent { success = false });
                return;
            }
            Initialized = true;
            Debug.Log("[SteamManager] Steam API initialized successfully");
            EventBus.Emit(new SteamInitializedEvent { success = true });
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SteamManager] Steam init exception: {e.Message}");
            Initialized = false;
            EventBus.Emit(new SteamInitializedEvent { success = false });
        }
#else
        Debug.Log("[SteamManager] Steamworks disabled — offline mode");
        Initialized = false;
        EventBus.Emit(new SteamInitializedEvent { success = false });
#endif
    }

    void Update()
    {
#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED
        if (Initialized)
            Steamworks.SteamAPI.RunCallbacks();
#endif
    }

    void OnDestroy()
    {
#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED
        if (Initialized)
            Steamworks.SteamAPI.Shutdown();
#endif
    }

    public bool IsSteamRunning()
    {
#if !DISABLESTEAMWORKS && STEAMWORKS_ENABLED
        return Initialized && Steamworks.SteamAPI.IsSteamRunning();
#else
        return false;
#endif
    }
}
