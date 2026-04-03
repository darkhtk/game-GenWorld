using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    readonly Queue<T> _available = new();
    readonly Func<T> _factory;
    readonly Transform _parent;
    readonly int _maxSize;
    int _totalCreated;

    public ObjectPool(Func<T> factory, Transform parent, int initialSize, int maxSize)
    {
        _factory = factory;
        _parent = parent;
        _maxSize = maxSize;
        for (int i = 0; i < initialSize; i++)
            Prewarm();
    }

    void Prewarm()
    {
        var obj = _factory();
        if (_parent != null) obj.transform.SetParent(_parent);
        obj.gameObject.SetActive(false);
        _available.Enqueue(obj);
        _totalCreated++;
    }

    public T Get()
    {
        while (_available.Count > 0)
        {
            var obj = _available.Dequeue();
            if (obj != null)
            {
                obj.gameObject.SetActive(true);
                return obj;
            }
            _totalCreated--;
        }

        if (_totalCreated < _maxSize)
        {
            var obj = _factory();
            if (_parent != null) obj.transform.SetParent(_parent);
            _totalCreated++;
            obj.gameObject.SetActive(true);
            return obj;
        }

        return null;
    }

    public void Return(T obj)
    {
        if (obj == null) return;
        if (!obj.gameObject.activeSelf) return;
        obj.gameObject.SetActive(false);
        if (_parent != null) obj.transform.SetParent(_parent);
        _available.Enqueue(obj);
    }

    public void Clear()
    {
        while (_available.Count > 0)
        {
            var obj = _available.Dequeue();
            if (obj != null) UnityEngine.Object.Destroy(obj.gameObject);
        }
        _totalCreated = 0;
    }
}
