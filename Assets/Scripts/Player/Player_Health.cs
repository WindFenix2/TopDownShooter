using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Health : HealthController
{
    private Player player;

    public bool isDead { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        player = GetComponent<Player>();
    }

    public override void ReduceHealth(int damage)
    {
        base.ReduceHealth(damage);

        if (ShouldDie())
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        Debug.Log("Player was killed at " + Time.time);
        isDead = true;
        player.anim.enabled = false;
        player.ragdoll.RagdollActive(true);
    }
}
