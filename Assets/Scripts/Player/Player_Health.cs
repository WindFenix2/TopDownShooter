using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Health : HealthController
{
    private Player player;

    public bool isDead { get; private set; }

    [Header("Debug")]
    [SerializeField] private int debugShieldHp;

    private Shotgun_KillShieldAbility shieldAbility;

    protected override void Awake()
    {
        base.Awake();

        player = GetComponent<Player>();
        shieldAbility = GetComponent<Shotgun_KillShieldAbility>();
    }

    private void Update()
    {
        if (shieldAbility != null)
            debugShieldHp = shieldAbility.CurrentShield;
        else
            debugShieldHp = 0;
    }

    public override void ReduceHealth(int damage)
    {
        base.ReduceHealth(damage);

        if (ShouldDie())
            Die();

        UI.instance.inGameUI.UpdateHealthUI(currentHealth, maxHealth);

        if (shieldAbility != null)
            debugShieldHp = shieldAbility.CurrentShield;
    }

    public override void IncreaseHealth(int amount)
    {
        if (isDead)
            return;

        base.IncreaseHealth(amount);
        UI.instance.inGameUI.UpdateHealthUI(currentHealth, maxHealth);

        if (shieldAbility != null)
            debugShieldHp = shieldAbility.CurrentShield;
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        player.anim.enabled = false;
        player.ragdoll.RagdollActive(true);

        GameManager.instance.GameOver();
    }
}