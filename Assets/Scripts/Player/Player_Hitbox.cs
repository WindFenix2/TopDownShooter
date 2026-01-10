using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Hitbox : HitBox
{
    private Player player;
    protected override void Awake()
    {
        base.Awake();

        player = GetComponentInParent<Player>();
    }

    public override void TakeDamage(int damage)
    {
        int newDamage = Mathf.RoundToInt(damage * damageMultiplier);

        player.health.ReduceHealth(newDamage);
    }
}
