using UnityEngine;
using UnityEngine.SceneManagement;

public class EventVFX : MonoBehaviour
{
    static EventVFX _instance;
    PlayerController _cachedPlayer;

    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnEnable()
    {
        EventBus.On<LevelUpEvent>(OnLevelUp);
        EventBus.On<ItemCollectEvent>(OnItemCollect);
    }

    void OnDisable()
    {
        EventBus.Off<LevelUpEvent>(OnLevelUp);
        EventBus.Off<ItemCollectEvent>(OnItemCollect);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Re-subscribe in case EventBus was cleared during scene transition
        EventBus.Off<LevelUpEvent>(OnLevelUp);
        EventBus.Off<ItemCollectEvent>(OnItemCollect);
        EventBus.On<LevelUpEvent>(OnLevelUp);
        EventBus.On<ItemCollectEvent>(OnItemCollect);
        _cachedPlayer = null;
    }

    PlayerController GetPlayer()
    {
        if (_cachedPlayer == null)
            _cachedPlayer = FindFirstObjectByType<PlayerController>();
        return _cachedPlayer;
    }

    void OnLevelUp(LevelUpEvent e)
    {
        var player = GetPlayer();
        if (player == null) return;
        SkillVFX.ShowAtPosition(this, "vfx_heal", player.Position.x, player.Position.y + 0.5f);
        DamageText.SpawnText(this, player.Position + Vector2.up, $"LEVEL UP! Lv.{e.level}", new Color(1f, 0.85f, 0.2f));
    }

    void OnItemCollect(ItemCollectEvent e)
    {
        var player = GetPlayer();
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
