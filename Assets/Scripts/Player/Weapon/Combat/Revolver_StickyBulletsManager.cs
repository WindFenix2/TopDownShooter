using System.Collections.Generic;
using UnityEngine;

public class Revolver_StickyBulletsManager : MonoBehaviour
{
    private readonly List<Revolver_StuckBullet> stuckBullets = new List<Revolver_StuckBullet>();

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

    // Один раз по врагу/игроку (без мульти-хитбоксов)
    public void ApplyExplosionDamage(Vector3 pos, int damage, float radius, LayerMask whatToDamage)
    {
        // ВАЖНО: не доверяем слоям хитбоксов, поэтому берём всё, а фильтруем компонентами
        Collider[] hits = Physics.OverlapSphere(pos, radius, ~0, QueryTriggerInteraction.Collide);
        if (hits == null || hits.Length == 0) return;

        HashSet<Transform> damagedEnemyRoots = new HashSet<Transform>();
        HashSet<Transform> damagedPlayerRoots = new HashSet<Transform>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null) continue;

            // === ENEMY ===
            Enemy enemy = c.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                Transform root = enemy.transform.root;
                if (damagedEnemyRoots.Add(root))
                {
                    // вот это ключ: урон через Enemy.GetHit => корректная смерть
                    enemy.GetHit(damage);
                }
                continue;
            }

            // === PLAYER ===
            // враги тебя дамажат через IDamagable, значит это самый безопасный путь
            IDamagable damagable = c.GetComponent<IDamagable>();
            if (damagable == null)
                damagable = c.GetComponentInParent<IDamagable>();

            if (damagable != null)
            {
                Transform root = (c.transform != null) ? c.transform.root : null;
                if (root != null)
                {
                    if (damagedPlayerRoots.Add(root))
                        damagable.TakeDamage(damage);
                }
                else
                {
                    // на всякий случай
                    damagable.TakeDamage(damage);
                }
            }
        }
    }
}
