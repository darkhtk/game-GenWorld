using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [SerializeField] WorldMapGenerator worldMap;
    [SerializeField] MonsterSpawner monsterSpawner;

    public DataManager Data { get; private set; }
    public InventorySystem Inventory { get; private set; }
    public SkillSystem Skills { get; private set; }
    public CraftingSystem Crafting { get; private set; }
    public QuestSystem Quests { get; private set; }
    public AIManager AI { get; private set; }
    public PlayerStats PlayerState { get; private set; }

    void Start() { Debug.Log("[GameManager] Start stub"); }
    void Update() { }
}
