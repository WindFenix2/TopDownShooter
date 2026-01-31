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

        ability.AbsorbDamage(damage);
    }
}
