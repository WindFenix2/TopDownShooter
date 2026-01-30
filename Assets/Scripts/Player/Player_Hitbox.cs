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

        Shotgun_KillShieldAbility shield = null;
        if (player != null)
            shield = player.GetComponent<Shotgun_KillShieldAbility>();

        if (shield != null)
            newDamage = shield.AbsorbDamage(newDamage);

        if (newDamage <= 0)
            return;

        player.health.ReduceHealth(newDamage);
    }
}
