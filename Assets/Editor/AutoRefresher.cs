using UnityEditor;

[InitializeOnLoad]
public static class AutoRefresher
{
    const double IntervalSeconds = 10.0;
    static double _nextRefresh;

    static AutoRefresher()
    {
        _nextRefresh = EditorApplication.timeSinceStartup + IntervalSeconds;
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        if (EditorApplication.timeSinceStartup < _nextRefresh) return;
        _nextRefresh = EditorApplication.timeSinceStartup + IntervalSeconds;
        AssetDatabase.Refresh();
    }
}
