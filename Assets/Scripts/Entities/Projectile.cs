using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Action<MonsterController> OnHitMonster;
    public Action<Vector2> OnArrive;
    public void Init(Vector2 from, Vector2 to, float speed, int color, float size, bool piercing) { }
}
