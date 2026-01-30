using UnityEngine;

public class Shotgun_ShieldHitbox : MonoBehaviour, IDamagable
{
    private Shotgun_KillShieldAbility ability;
    private Player_Health playerHealth;

    public void Setup(Shotgun_KillShieldAbility ability, Player_Health playerHealth)
    {
        this.ability = ability;
        this.playerHealth = playerHealth;
    }

    public void TakeDamage(int damage)
    {
        if (ability == null)
            return;

        int remaining = ability.AbsorbDamage(damage);

        // если щит кончилс€ и урон осталс€ Ч сразу даЄм его игроку
        if (remaining > 0 && playerHealth != null)
            playerHealth.ReduceHealth(remaining);
    }
}
