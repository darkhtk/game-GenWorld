using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class VillageNPC : MonoBehaviour
{
    Rigidbody2D _rb;

    public NpcDef Def { get; private set; }
    public NPCBrain Brain { get; set; }
    public Vector2 Position => (Vector2)transform.position;
    public bool IsStopped { get; set; }

    public void Init(NpcDef def, Vector2 position) { Def = def; _rb = GetComponent<Rigidbody2D>(); _rb.gravityScale = 0; _rb.freezeRotation = true; transform.position = position; }
    void Update() { }
    public bool IsInInteractionRange(Vector2 playerPos, float range = 48f) => Vector2.Distance(Position, playerPos) <= range;
    public void StopMoving() { IsStopped = true; if (_rb) _rb.linearVelocity = Vector2.zero; }
    public void ResumeMoving() => IsStopped = false;
}
