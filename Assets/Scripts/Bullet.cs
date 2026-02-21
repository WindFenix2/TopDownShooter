using UnityEngine;

public class Bullet : MonoBehaviour
{
    private int bulletDamage;
    private float impactForce;

    private BoxCollider cd;
    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private TrailRenderer trailRenderer;

    [SerializeField] private GameObject bulletImpactFX;

    private Vector3 startPosition;
    private float flyDistance;
    private bool bulletDisabled;

    private LayerMask allyLayerMask;
    private Transform ownerRoot;

    private WeaponType weaponTypeUsed = WeaponType.Pistol;
    private bool isPlayerBulletCached = true;

    [Header("Visual (future separation player/enemy)")]
    [SerializeField] private bool useColorOverride;
    [SerializeField] private Color playerBulletColor = new Color(0.0f, 1.0f, 0.85f, 1f);
    [SerializeField] private Color enemyBulletColor = new Color(1f, 0.1f, 0.1f, 1f);

    private MaterialPropertyBlock mpb;

    public int BulletDamage => bulletDamage;
    public bool IsPlayerBullet => isPlayerBulletCached;
    public Transform OwnerRoot => ownerRoot;
    public WeaponType WeaponTypeUsed => weaponTypeUsed;

    protected virtual void Awake()
    {
        cd = GetComponent<BoxCollider>();
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();

        mpb = new MaterialPropertyBlock();
    }

    public void BulletSetup(
        LayerMask allyLayerMask,
        int bulletDamage,
        float flyDistance = 100,
        float impactForce = 100,
        Transform owner = null,
        bool isPlayerBullet = true,
        WeaponType weaponTypeUsed = WeaponType.Pistol
    )
    {
        this.allyLayerMask = allyLayerMask;
        this.impactForce = impactForce;
        this.bulletDamage = bulletDamage;

        this.weaponTypeUsed = weaponTypeUsed;
        this.isPlayerBulletCached = isPlayerBullet;

        ownerRoot = owner != null ? owner.root : null;

        bulletDisabled = false;
        cd.enabled = true;
        meshRenderer.enabled = true;

        Pistol_BulletSoftHoming homing = GetComponent<Pistol_BulletSoftHoming>();
        if (homing != null)
            homing.DisableHoming();

        ApplyVisual(isPlayerBullet);

        trailRenderer.Clear();
        trailRenderer.time = .25f;
        startPosition = transform.position;
        this.flyDistance = flyDistance + .5f;
    }

    private void ApplyVisual(bool isPlayerBullet)
    {
        if (!useColorOverride || meshRenderer == null)
            return;

        meshRenderer.GetPropertyBlock(mpb);

        if (isPlayerBullet)
        {
            mpb.SetColor("_BaseColor", playerBulletColor);
            mpb.SetColor("_Color", playerBulletColor);
        }
        else
        {
            // mpb.SetColor("_BaseColor", enemyBulletColor);
            // mpb.SetColor("_Color", enemyBulletColor);
        }

        meshRenderer.SetPropertyBlock(mpb);
    }

    protected virtual void Update()
    {
        FadeTrailIfNeeded();
        DisableBulletIfNeeded();
        ReturnToPoolIfNeeded();
    }

    protected void ReturnToPoolIfNeeded()
    {
        if (trailRenderer.time < 0)
            ReturnBulletToPool();
    }

    protected void DisableBulletIfNeeded()
    {
        if (Vector3.Distance(startPosition, transform.position) > flyDistance && !bulletDisabled)
        {
            cd.enabled = false;
            meshRenderer.enabled = false;
            bulletDisabled = true;
        }
    }

    protected void FadeTrailIfNeeded()
    {
        if (Vector3.Distance(startPosition, transform.position) > flyDistance - 1.5f)
            trailRenderer.time -= 2 * Time.deltaTime;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
        HandleHit(collision.collider, collision.transform, hitPoint);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        HandleHit(other, other.transform, other.ClosestPoint(transform.position));
    }

    private void HandleHit(Collider hitCol, Transform hitTr, Vector3 hitPoint)
    {
        if (hitTr == null)
        {
            ReturnBulletToPool();
            return;
        }

        // ✅ FIX: ignore interaction triggers (Pickup_Weapon / Pickup_Ammo etc.)
        // These are Interactable trigger colliders used only for player interaction.
        if (hitCol != null && hitCol.isTrigger)
        {
            Interactable interactable = hitCol.GetComponentInParent<Interactable>();
            if (interactable != null)
                return;
        }

        if (isPlayerBulletCached && ownerRoot != null)
        {
            Shotgun_ShieldHitbox shieldHb = null;

            if (hitCol != null)
            {
                shieldHb = hitCol.GetComponent<Shotgun_ShieldHitbox>();
                if (shieldHb == null)
                    shieldHb = hitCol.GetComponentInParent<Shotgun_ShieldHitbox>();
            }

            if (shieldHb == null)
            {
                shieldHb = hitTr.GetComponent<Shotgun_ShieldHitbox>();
                if (shieldHb == null)
                    shieldHb = hitTr.GetComponentInParent<Shotgun_ShieldHitbox>();
            }

            if (shieldHb != null && hitTr.root == ownerRoot)
                return;
        }

        if (ownerRoot != null && hitTr.root == ownerRoot)
        {
            ReturnBulletToPool();
            return;
        }

        if (FriendlyFare() == false)
        {
            if ((allyLayerMask.value & (1 << hitTr.gameObject.layer)) > 0)
            {
                ReturnBulletToPool(10);
                return;
            }
        }

        CreateImpactFx();
        ReturnBulletToPool();

        Enemy enemy = null;
        if (hitCol != null)
            enemy = hitCol.GetComponentInParent<Enemy>();
        if (enemy == null)
            enemy = hitTr.GetComponentInParent<Enemy>();

        bool enemyWasAlive = enemy != null && enemy.IsDead == false;

        IDamagable damagable = null;

        if (hitCol != null)
        {
            damagable = hitCol.GetComponent<IDamagable>();
            if (damagable == null)
                damagable = hitCol.GetComponentInParent<IDamagable>();
        }

        if (damagable == null)
        {
            damagable = hitTr.GetComponent<IDamagable>();
            if (damagable == null)
                damagable = hitTr.GetComponentInParent<IDamagable>();
        }

        if (damagable != null)
        {
            damagable.TakeDamage(bulletDamage);
        }
        else
        {
            if (enemy != null && enemy.IsDead == false)
                enemy.GetHit(bulletDamage);
        }

        if (enemyWasAlive && enemy != null && enemy.IsDead)
            TryNotifyOwnerAboutEnemyKill();

        ApplyBulletImpactToEnemy(hitCol, hitPoint);
    }

    private void TryNotifyOwnerAboutEnemyKill()
    {
        if (!isPlayerBulletCached)
            return;

        if (weaponTypeUsed != WeaponType.Shotgun)
            return;

        if (ownerRoot == null)
            return;

        Shotgun_KillShieldAbility ability = ownerRoot.GetComponent<Shotgun_KillShieldAbility>();
        if (ability == null)
            return;

        ability.NotifyEnemyKilled(weaponTypeUsed);
    }

    private void ApplyBulletImpactToEnemy(Collider hitCol, Vector3 hitPoint)
    {
        if (hitCol == null)
            return;

        Enemy enemy = hitCol.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            Vector3 force = rb.velocity.normalized * impactForce;
            Rigidbody hitRigidbody = hitCol.attachedRigidbody;
            enemy.BulletImpact(force, hitPoint, hitRigidbody);
        }
    }

    protected void ReturnBulletToPool(float delay = 0) => ObjectPool.instance.ReturnObject(gameObject, delay);

    public void ReturnToPoolPublic(float delay = 0)
    {
        if (ObjectPool.instance != null)
            ObjectPool.instance.ReturnObject(gameObject, delay);
    }

    protected void CreateImpactFx()
    {
        if (bulletImpactFX == null)
            return;

        GameObject newFx = Instantiate(bulletImpactFX);
        newFx.transform.position = transform.position;
        Destroy(newFx, 1);
    }

    private bool FriendlyFare() => GameManager.instance.friendlyFire;
}