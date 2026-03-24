using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class EMI_GrenadeProjectile : MonoBehaviour
{
    private Rigidbody rb;
    private float spawnTime;
    private bool exploded;

    private Player owner;

    private float maxLifetime = 2.0f;
    private bool explodeOnCollision = true;

    private float radius = 2.5f;
    private LayerMask whatIsEnemy = ~0;

    private float enemyDuration = 10f;
    private float enemySpeedMultiplier = 0.4f;

    private float bossDuration = 7f;
    private float bossSpeedMultiplier = 0.5f;

    private float playerDuration = 10f;
    private float playerSpeedMultiplier = 0.4f;

    private GameObject explosionVfx;
    private GameObject hitAuraVfx;

    private GameObject radiusIndicatorVfx;
    private float indicatorLifetime = 0.6f;
    private float indicatorY = 0.02f;

    private float explosionVfxScaleMultiplier = 1.0f;
    private float auraScaleMultiplier = 1.0f;

    private const float BASE_RADIUS_FOR_VFX = 2.5f;

    private class EMI_VfxScaleCache : MonoBehaviour
    {
        public Vector3 baseScale;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        spawnTime = Time.time;
        exploded = false;
    }

    public void ConfigureAndFire(
        Player owner,
        Vector3 direction,
        float speed,
        float upwardBoost,
        bool useGravity,
        float lifeTime,
        bool explodeOnHit,
        float explosionRadius,
        LayerMask enemyMask,
        float enemyDuration,
        float enemySpeedMultiplier,
        float bossDuration,
        float bossSpeedMultiplier,
        float playerDuration,
        float playerSpeedMultiplier,
        GameObject explosionVfx,
        GameObject hitAuraVfx,
        GameObject radiusIndicatorVfx,
        float indicatorLifetime,
        float indicatorY,
        float indicatorScaleMultiplier,
        float explosionVfxScaleMultiplier,
        float auraScaleMultiplier
    )
    {
        this.owner = owner;

        maxLifetime = Mathf.Max(0.05f, lifeTime);
        explodeOnCollision = explodeOnHit;

        radius = Mathf.Max(0.1f, explosionRadius);
        whatIsEnemy = enemyMask;

        this.enemyDuration = Mathf.Max(0.01f, enemyDuration);
        this.enemySpeedMultiplier = Mathf.Clamp(enemySpeedMultiplier, 0.05f, 1f);

        this.bossDuration = Mathf.Max(0.01f, bossDuration);
        this.bossSpeedMultiplier = Mathf.Clamp(bossSpeedMultiplier, 0.05f, 1f);

        this.playerDuration = Mathf.Max(0.01f, playerDuration);
        this.playerSpeedMultiplier = Mathf.Clamp(playerSpeedMultiplier, 0.05f, 1f);

        this.explosionVfx = explosionVfx;
        this.hitAuraVfx = hitAuraVfx;

        this.radiusIndicatorVfx = radiusIndicatorVfx;
        this.indicatorLifetime = Mathf.Clamp(indicatorLifetime, 0.05f, 5f);
        this.indicatorY = indicatorY;

        this.explosionVfxScaleMultiplier = Mathf.Clamp(explosionVfxScaleMultiplier, 0.1f, 10f);
        this.auraScaleMultiplier = Mathf.Clamp(auraScaleMultiplier, 0.2f, 3f);

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        rb.useGravity = useGravity;

        Vector3 dir = direction.normalized;
        Vector3 velocity = dir * Mathf.Max(0.01f, speed);

        if (useGravity)
            velocity += Vector3.up * upwardBoost;

        rb.velocity = velocity;

        if (owner != null)
        {
            Collider grenadeCol = GetComponent<Collider>();
            if (grenadeCol != null)
            {
                Collider[] ownerCols = owner.GetComponentsInChildren<Collider>();
                for (int i = 0; i < ownerCols.Length; i++)
                    Physics.IgnoreCollision(grenadeCol, ownerCols[i], true);
            }
        }
    }

    private void Update()
    {
        if (exploded)
            return;

        if (Time.time > spawnTime + maxLifetime)
            Explode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!explodeOnCollision)
            return;

        if (exploded)
            return;

        Explode();
    }

    private void Explode()
    {
        if (exploded)
            return;

        exploded = true;

        Vector3 explosionPos = ProjectToGround(transform.position);

        SpawnRadiusIndicator(explosionPos);
        SpawnExplosionVfx(explosionPos);
        ApplyToEnemies(explosionPos);
        ApplyToOwnerPlayer(explosionPos);

        if (ObjectPool.instance != null)
            ObjectPool.instance.ReturnObject(gameObject, 0f);
        else
            Destroy(gameObject);
    }

    private Vector3 ProjectToGround(Vector3 pos)
    {
        Vector3 origin = pos + Vector3.up * 3f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 p = hit.point;
            p.y += indicatorY;
            return p;
        }

        pos.y += indicatorY;
        return pos;
    }

    private void SpawnRadiusIndicator(Vector3 pos)
    {
        if (radiusIndicatorVfx == null)
            return;

        float diameter = radius * 2f;

        if (ObjectPool.instance != null)
        {
            GameObject ring = ObjectPool.instance.GetObject(radiusIndicatorVfx, transform);
            if (ring != null)
            {
                ring.transform.position = pos;
                ring.transform.rotation = Quaternion.identity;
                ring.transform.localScale = Vector3.one;

                ForceIndicatorSize(ring, diameter);

                ObjectPool.instance.ReturnObject(ring, indicatorLifetime);
            }
        }
        else
        {
            GameObject ring = Instantiate(radiusIndicatorVfx, pos, Quaternion.identity);
            ring.transform.localScale = Vector3.one;

            ForceIndicatorSize(ring, diameter);

            Destroy(ring, indicatorLifetime);
        }
    }

    private void ForceIndicatorSize(GameObject ringObj, float diameter)
    {
        ParticleSystem ps = ringObj.GetComponent<ParticleSystem>();
        if (ps == null) ps = ringObj.GetComponentInChildren<ParticleSystem>();

        if (ps == null)
        {
            ApplyScaleWithCache(ringObj.transform, diameter);
            return;
        }

        var main = ps.main;

        if (main.startSize3D)
        {
            main.startSizeX = diameter;
            main.startSizeY = diameter;
            main.startSizeZ = diameter;
        }
        else
        {
            main.startSize = diameter;
        }

        ps.Clear(true);
        ps.Play(true);
    }

    private void SpawnExplosionVfx(Vector3 pos)
    {
        if (explosionVfx == null)
            return;

        float scaleMul = (radius / BASE_RADIUS_FOR_VFX) * explosionVfxScaleMultiplier;
        scaleMul = Mathf.Clamp(scaleMul, 0.2f, 12f);

        if (ObjectPool.instance != null)
        {
            GameObject fx = ObjectPool.instance.GetObject(explosionVfx, transform);
            if (fx != null)
            {
                fx.transform.position = pos;
                fx.transform.rotation = Quaternion.identity;

                ApplyScaleWithCache(fx.transform, scaleMul);

                ObjectPool.instance.ReturnObject(fx, 2f);
            }
        }
        else
        {
            GameObject fx = Instantiate(explosionVfx, pos, Quaternion.identity);
            ApplyScaleWithCache(fx.transform, scaleMul);
            Destroy(fx, 2f);
        }
    }

    private void ApplyToEnemies(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapSphere(pos, radius, whatIsEnemy, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return;


        HashSet<int> processed = new HashSet<int>();

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null) continue;

            Enemy e = c.GetComponentInParent<Enemy>();
            if (e == null) continue;
            if (e.IsDead) continue;

            int id = e.gameObject.GetInstanceID();
            if (!processed.Add(id))
                continue;

            bool isBoss = e.enemyType == EnemyType.Boss || e.GetComponent<Enemy_Boss>() != null;

            if (!isBoss)
            {
                e.ApplyEMIDebuff(enemySpeedMultiplier, enemyDuration, disableAttacks: true, disableAbilities: true);
                SpawnAura(e.transform, enemyDuration);
                continue;
            }

            e.ApplyEMIDebuff(bossSpeedMultiplier, bossDuration, disableAttacks: false, disableAbilities: true);
            SpawnAura(e.transform, bossDuration);
        }
    }

    private void ApplyToOwnerPlayer(Vector3 explosionPos)
    {
        if (owner == null)
            return;

        Vector2 p = new Vector2(owner.transform.position.x, owner.transform.position.z);
        Vector2 e = new Vector2(explosionPos.x, explosionPos.z);
        float distXZ = Vector2.Distance(p, e);

        if (distXZ > radius)
            return;


        Shotgun_KillShieldAbility shield = owner.GetComponent<Shotgun_KillShieldAbility>();
        if (shield != null && shield.TryConsumeShieldForEMI())
            return;

        Player_EMIStatus status = owner.GetComponent<Player_EMIStatus>();
        if (status != null)
            status.ApplyEMI(playerSpeedMultiplier, playerDuration, disableShooting: true, disableAbilities: true);

        SpawnAura(owner.transform, playerDuration);
    }

    private void SpawnAura(Transform target, float effectDuration)
    {
        if (target == null || hitAuraVfx == null)
            return;

        float ttl = Mathf.Clamp(effectDuration, 0.5f, 20f);

        if (ObjectPool.instance != null)
        {
            GameObject aura = ObjectPool.instance.GetObject(hitAuraVfx, target);
            if (aura != null)
            {
                aura.transform.SetParent(target, true);
                aura.transform.localPosition = Vector3.zero;
                aura.transform.localRotation = Quaternion.identity;

                ApplyScaleWithCache(aura.transform, auraScaleMultiplier);
                DisableAllColliders(aura);

                ObjectPool.instance.ReturnObject(aura, ttl);
            }
        }
        else
        {
            GameObject aura = Instantiate(hitAuraVfx, target.position, Quaternion.identity);
            aura.transform.SetParent(target, true);
            aura.transform.localPosition = Vector3.zero;
            aura.transform.localRotation = Quaternion.identity;

            ApplyScaleWithCache(aura.transform, auraScaleMultiplier);
            DisableAllColliders(aura);

            Destroy(aura, ttl);
        }
    }

    private void DisableAllColliders(GameObject obj)
    {
        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private void ApplyScaleWithCache(Transform t, float mul)
    {
        EMI_VfxScaleCache cache = t.GetComponent<EMI_VfxScaleCache>();
        if (cache == null)
            cache = t.gameObject.AddComponent<EMI_VfxScaleCache>();

        if (cache.baseScale == Vector3.zero)
            cache.baseScale = t.localScale;

        t.localScale = cache.baseScale * mul;
    }
}
