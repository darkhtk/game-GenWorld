using System;
using System.Collections.Generic;
using System.Linq;

public static class EventBus
{
    static readonly Dictionary<Type, List<Delegate>> _listeners = new();

    public static void On<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!_listeners.ContainsKey(type)) _listeners[type] = new List<Delegate>();
        _listeners[type].Add(handler);
    }

    public static void Off<T>(Action<T> handler)
    {
        if (_listeners.TryGetValue(typeof(T), out var list)) list.Remove(handler);
    }

    public static void Emit<T>(T evt)
    {
        if (!_listeners.TryGetValue(typeof(T), out var list)) return;
        for (int i = list.Count - 1; i >= 0; i--)
            ((Action<T>)list[i]).Invoke(evt);
    }

    public static void Clear() => _listeners.Clear();
}
