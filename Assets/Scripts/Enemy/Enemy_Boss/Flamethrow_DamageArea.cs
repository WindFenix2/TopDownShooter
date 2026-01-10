using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flamethrow_DamageArea : MonoBehaviour
{
    private Enemy_Boss enemy;

    private float damageCooldown;
    private float lastTimeDamaged;
    private int flameDamage;

    private void Awake()
    {
        enemy = GetComponentInParent<Enemy_Boss>();
        damageCooldown = enemy.flameDamageCooldown;
        flameDamage = enemy.flameDamage;
    }

    private void OnTriggerStay(Collider other)
    {
        if (enemy.flamethrowActive == false)
            return;

        if (Time.time - lastTimeDamaged < damageCooldown)
            return;


        IDamagable damagable = other.GetComponent<IDamagable>();

        if (damagable != null)
        {
            damagable.TakeDamage(flameDamage);
            lastTimeDamaged = Time.time; // Update the last tiem damage was applied
            damageCooldown = enemy.flameDamageCooldown; // For easier testing I'm updating
                                                        // cooldown everytime we damage enemy
        }

    }
}
