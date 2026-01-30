using System.Collections.Generic;
using UnityEngine;

public class Revolver_StickyBulletsManager : MonoBehaviour
{
    private readonly List<Revolver_StuckBullet> stuckBullets = new List<Revolver_StuckBullet>();

    [Header("Special case")]
    [SerializeField] private bool damageCarsEvenIfDefaultLayerNotInMask = true;

    [Header("Explosion push (physics)")]
    [SerializeField] private float explosionPushForce = 14f;
    [SerializeField] private float explosionUpwards = 0.25f;
    [SerializeField, Range(0.1f, 1f)] private float pushRadiusMultiplier = 0.8f;
    [SerializeField] private bool pushOnlyPropsCars = true;    

    private static bool LayerInMask(int layer, LayerMask mask)
    {
        int bit = 1 << layer;
        return (mask.value & bit) != 0;
    }

    public void Register(Revolver_StuckBullet bullet)
    {
        if (bullet == null) return;
        if (!stuckBullets.Contains(bullet))
            stuckBullets.Add(bullet);
    }

    public void Unregister(Revolver_StuckBullet bullet)
    {
        if (bullet == null) return;
        stuckBullets.Remove(bullet);
    }

    public void DetonateAll(int damage, float radius, LayerMask whatToDamage)
    {
        if (stuckBullets.Count == 0)
            return;

        var copy = new List<Revolver_StuckBullet>(stuckBullets);
        stuckBullets.Clear();

        for (int i = 0; i < copy.Count; i++)
        {
            if (copy[i] == null) continue;
            copy[i].DetonateNow(damage, radius, whatToDamage);
        }
    }

    public void ApplyExplosionDamage(Vector3 pos, int damage, float radius, LayerMask whatToDamage)
    {
        if (damage <= 0) return;
        if (radius <= 0f) return;

        if (whatToDamage.value == 0)
            return;

        Collider[] hits = Physics.OverlapSphere(pos, radius, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return;

        HashSet<int> damagedEnemyIds = new HashSet<int>();
        HashSet<int> damagedPlayerIds = new HashSet<int>();
        HashSet<int> damagedCarIds = new HashSet<int>();
        HashSet<int> damagedOtherIds = new HashSet<int>();

        HashSet<int> pushedRigidbodyIds = new HashSet<int>();

        float pushRadius = radius * pushRadiusMultiplier;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null) continue;

            if (explosionPushForce > 0f && pushRadius > 0f)
            {
                Rigidbody hitRb = c.attachedRigidbody;
                if (hitRb != null && !hitRb.isKinematic)
                {
                    if (!pushOnlyPropsCars || (c.GetComponentInParent<Player>() == null && c.GetComponentInParent<Enemy>() == null))
                    {
                        Vector3 rbPos = hitRb.worldCenterOfMass;
                        if ((rbPos - pos).sqrMagnitude <= pushRadius * pushRadius)
                        {
                            int rbId = hitRb.GetInstanceID();
                            if (pushedRigidbodyIds.Add(rbId))
                                hitRb.AddExplosionForce(explosionPushForce, pos, pushRadius, explosionUpwards, ForceMode.Impulse);
                        }
                    }
                }
            }

            Car_HealthController car = c.GetComponentInParent<Car_HealthController>();
            if (car != null)
            {
                bool allowed =
                    LayerInMask(car.gameObject.layer, whatToDamage) ||
                    (damageCarsEvenIfDefaultLayerNotInMask && car.gameObject.layer == 0);

                if (!allowed)
                    continue;

                int id = car.GetInstanceID();
                if (damagedCarIds.Add(id))
                    car.TakeDamage(damage);

                continue;
            }

            Enemy enemy = c.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                if (!LayerInMask(enemy.gameObject.layer, whatToDamage))
                    continue;

                int id = (enemy.health != null) ? enemy.health.GetInstanceID() : enemy.GetInstanceID();
                if (damagedEnemyIds.Add(id))
                    enemy.GetHit(damage);

                continue;
            }

            Player player = c.GetComponentInParent<Player>();
            if (player != null)
            {
                if (!LayerInMask(player.gameObject.layer, whatToDamage))
                    continue;

                int id = player.GetInstanceID();
                if (damagedPlayerIds.Add(id))
                {
                    IDamagable playerDmg = player.GetComponentInChildren<IDamagable>();
                    if (playerDmg != null)
                        playerDmg.TakeDamage(damage);
                    else if (player.health != null)
                        player.health.ReduceHealth(damage);
                }

                continue;
            }

            IDamagable damagable = c.GetComponent<IDamagable>();
            if (damagable == null)
                damagable = c.GetComponentInParent<IDamagable>();

            if (damagable == null)
                continue;

            Component dmgComp = damagable as Component;
            if (dmgComp == null)
            {
                damagable.TakeDamage(damage);
                continue;
            }

            Enemy enemyFromDmg = dmgComp.GetComponentInParent<Enemy>();
            if (enemyFromDmg != null)
            {
                if (!LayerInMask(enemyFromDmg.gameObject.layer, whatToDamage))
                    continue;

                int id = (enemyFromDmg.health != null) ? enemyFromDmg.health.GetInstanceID() : enemyFromDmg.GetInstanceID();
                if (damagedEnemyIds.Add(id))
                    enemyFromDmg.GetHit(damage);

                continue;
            }

            Player playerFromDmg = dmgComp.GetComponentInParent<Player>();
            if (playerFromDmg != null)
            {
                if (!LayerInMask(playerFromDmg.gameObject.layer, whatToDamage))
                    continue;

                int id = playerFromDmg.GetInstanceID();
                if (damagedPlayerIds.Add(id))
                {
                    IDamagable playerDmg = playerFromDmg.GetComponentInChildren<IDamagable>();
                    if (playerDmg != null)
                        playerDmg.TakeDamage(damage);
                    else if (playerFromDmg.health != null)
                        playerFromDmg.health.ReduceHealth(damage);
                }

                continue;
            }

            if (!LayerInMask(dmgComp.gameObject.layer, whatToDamage))
                continue;

            int otherId = dmgComp.GetInstanceID();
            if (damagedOtherIds.Add(otherId))
                damagable.TakeDamage(damage);
        }
    }
}