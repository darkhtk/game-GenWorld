using System.Collections.Generic;
using UnityEngine;

public struct WorldEventStartEvent { public string id, name, description; public float duration; }
public struct WorldEventEndEvent { public string id; }

public class WorldEventSystem
{
    public struct WorldEventDef
    {
        public string id, name, description, type;
        public float duration, minInterval, chance;
    }

    readonly List<WorldEventDef> _defs = new();
    readonly Dictionary<string, float> _lastOccurrence = new();
    WorldEventDef? _activeEvent;
    float _eventStartHour;
    float _lastCheckHour;

    public bool IsEventActive => _activeEvent != null;
    public WorldEventDef? ActiveEvent => _activeEvent;
    public float EventTimeRemaining(float gameHour) =>
        _activeEvent != null ? Mathf.Max(0, _activeEvent.Value.duration - (gameHour - _eventStartHour)) : 0;

    public float GlobalDropMultiplier { get; private set; } = 1f;
    public float GlobalGoldMultiplier { get; private set; } = 1f;

    public void Init()
    {
        _defs.Add(new WorldEventDef { id = "blood_moon", name = "Blood Moon", description = "Elite monsters appear!", type = "elite_spawn", duration = 2f, minInterval = 8f, chance = 0.3f });
        _defs.Add(new WorldEventDef { id = "golden_hour", name = "Golden Hour", description = "Double drops and bonus gold!", type = "bonus_drops", duration = 1f, minInterval = 6f, chance = 0.4f });
        _defs.Add(new WorldEventDef { id = "goblin_raid", name = "Goblin Raid", description = "Goblins are attacking the village!", type = "invasion", duration = 1.5f, minInterval = 10f, chance = 0.2f });
        _defs.Add(new WorldEventDef { id = "wandering_merchant", name = "Wandering Merchant", description = "A mysterious merchant has arrived.", type = "merchant", duration = 3f, minInterval = 12f, chance = 0.25f });
    }

    public void Update(float gameHour)
    {
        if (gameHour - _lastCheckHour < 1f) return;
        _lastCheckHour = gameHour;

        if (_activeEvent != null)
        {
            if (gameHour - _eventStartHour >= _activeEvent.Value.duration)
                EndEvent();
            return;
        }

        // Shuffle check order
        for (int i = _defs.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_defs[i], _defs[j]) = (_defs[j], _defs[i]);
        }

        foreach (var def in _defs)
        {
            _lastOccurrence.TryGetValue(def.id, out float lastTime);
            if (gameHour - lastTime < def.minInterval) continue;
            if (Random.value > def.chance) continue;
            StartEvent(def, gameHour);
            break;
        }
    }

    void StartEvent(WorldEventDef def, float gameHour)
    {
        _activeEvent = def;
        _eventStartHour = gameHour;
        _lastOccurrence[def.id] = gameHour;

        switch (def.type)
        {
            case "bonus_drops":
                GlobalDropMultiplier = 2f;
                GlobalGoldMultiplier = 1.5f;
                break;
        }

        EventBus.Emit(new WorldEventStartEvent
        {
            id = def.id, name = def.name,
            description = def.description, duration = def.duration
        });
        Debug.Log($"[WorldEvent] Started: {def.name} (duration {def.duration}h)");
    }

    void EndEvent()
    {
        if (_activeEvent == null) return;
        string id = _activeEvent.Value.id;

        GlobalDropMultiplier = 1f;
        GlobalGoldMultiplier = 1f;

        EventBus.Emit(new WorldEventEndEvent { id = id });
        Debug.Log($"[WorldEvent] Ended: {_activeEvent.Value.name}");
        _activeEvent = null;
    }

    // Save/Load
    public (string activeId, float startHour, Dictionary<string, float> lastOccurrence) Serialize()
    {
        return (_activeEvent?.id, _eventStartHour, new Dictionary<string, float>(_lastOccurrence));
    }

    public void Restore(string activeId, float startHour, Dictionary<string, float> lastOccurrence, float currentHour)
    {
        if (lastOccurrence != null)
            foreach (var kv in lastOccurrence) _lastOccurrence[kv.Key] = kv.Value;

        if (!string.IsNullOrEmpty(activeId))
        {
            foreach (var def in _defs)
            {
                if (def.id != activeId) continue;
                if (currentHour - startHour < def.duration)
                    StartEvent(def, startHour);
                break;
            }
        }
    }
}
