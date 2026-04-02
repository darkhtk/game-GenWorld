using UnityEngine;
using UnityEngine.EventSystems;

public static class EventSystemEnsurer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Ensure()
    {
        if (EventSystem.current != null) return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        Object.DontDestroyOnLoad(go);
        Debug.Log("[EventSystemEnsurer] Created missing EventSystem");
    }
}
