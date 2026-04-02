using UnityEngine;

public class EventVFX : MonoBehaviour
{
    static EventVFX _instance;

    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        EventBus.On<LevelUpEvent>(OnLevelUp);
        EventBus.On<ItemCollectEvent>(OnItemCollect);
    }

    void OnLevelUp(LevelUpEvent e)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        SkillVFX.ShowAtPosition(this, "vfx_heal", player.Position.x, player.Position.y + 0.5f);
        DamageText.SpawnText(this, player.Position + Vector2.up, $"LEVEL UP! Lv.{e.level}", new Color(1f, 0.85f, 0.2f));
    }

    void OnItemCollect(ItemCollectEvent e)
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        SkillVFX.ShowAtPosition(this, "vfx_loot_pickup", player.Position.x, player.Position.y);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (_instance != null) return;
        var go = new GameObject("EventVFX");
        go.AddComponent<EventVFX>();
    }
}
