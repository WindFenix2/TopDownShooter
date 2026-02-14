using UnityEngine;

public class EMI_GrenadeLauncher : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode fireKey = KeyCode.T;

    [Header("Launcher")]
    [SerializeField] private float cooldown = 15f;
    [SerializeField] private int grenadesPerShot = 1;

    [Header("Projectile")]
    [SerializeField] private GameObject emiGrenadeProjectilePrefab;
    [SerializeField] private float projectileSpeed = 18f;
    [SerializeField] private float projectileLifetime = 2.0f;
    [SerializeField] private bool explodeOnCollision = true;

    [Header("Ballistics")]
    [SerializeField] private bool useGravity = true;
    [SerializeField] private float upwardBoost = 2.0f;

    [Header("Explosion")]
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private LayerMask whatIsEnemy = ~0;

    [Header("Debuff - regular enemies")]
    [SerializeField] private float enemyDuration = 10f;
    [SerializeField, Range(0.05f, 1f)] private float enemySpeedMultiplier = 0.25f;

    [Header("Debuff - bosses")]
    [SerializeField] private float bossDuration = 7f;
    [SerializeField, Range(0.05f, 1f)] private float bossSpeedMultiplier = 0.5f;

    [Header("Debuff - player")]
    [SerializeField] private float playerDuration = 10f;
    [SerializeField, Range(0.05f, 1f)] private float playerSpeedMultiplier = 0.25f;

    [Header("VFX")]
    [SerializeField] private GameObject explosionVfx;
    [SerializeField] private GameObject hitAuraVfx;

    [Header("Radius indicator")]
    [Tooltip("Плоский круг/кольцо, которое показывает реальный радиус взрыва 1:1.")]
    [SerializeField] private GameObject radiusIndicatorVfx;

    [Tooltip("Сколько секунд держать индикатор радиуса (обычно 0.4-0.8).")]
    [SerializeField] private float indicatorLifetime = 0.6f;

    [Tooltip("Небольшой Y-offset, чтобы круг не мерцал с землёй.")]
    [SerializeField] private float indicatorY = 0.02f;

    [Tooltip("Если твой индикатор сделан не под 1 unit = 1m, подгони тут.")]
    [SerializeField] private float indicatorScaleMultiplier = 1.0f;

    [Tooltip("Оставь 1.0. Это только для красоты взрыва, но НЕ для честного индикатора.")]
    [SerializeField] private float explosionVfxScaleMultiplier = 1.0f;

    [Tooltip("Размер ауры НА ЦЕЛИ. Не зависит от радиуса.")]
    [SerializeField] private float auraScaleMultiplier = 1.0f;

    private float lastTimeFired = -999f;

    private Player player;
    private Player_WeaponController weapon;
    private EMI_GrenadeInventory inventory;
    private Player_EMIStatus playerStatus;

    private void Awake()
    {
        player = GetComponent<Player>();
        weapon = GetComponent<Player_WeaponController>();
        inventory = GetComponent<EMI_GrenadeInventory>();
        playerStatus = GetComponent<Player_EMIStatus>();
    }

    private void Update()
    {
        if (player == null || weapon == null)
            return;

        if (player.health != null && player.health.isDead)
            return;

        if (player.controlsEnabled == false)
            return;

        if (playerStatus != null && !playerStatus.CanUseAbilities)
            return;

        Weapon w = weapon.CurrentWeapon();
        if (w == null || w.weaponType != WeaponType.AutoRifle)
            return;

        if (emiGrenadeProjectilePrefab == null)
            return;

        if (!Input.GetKeyDown(fireKey))
            return;

        if (Time.time < lastTimeFired + Mathf.Max(0.05f, cooldown))
            return;

        if (inventory == null || inventory.TryConsume(grenadesPerShot) == false)
            return;

        Transform gunPoint = weapon.GunPoint();
        if (gunPoint == null)
            return;

        SpawnProjectile(gunPoint);
        lastTimeFired = Time.time;
    }

    private void SpawnProjectile(Transform gunPoint)
    {
        Vector3 dir = weapon.BulletDirection();
        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;

        GameObject go;

        if (ObjectPool.instance != null)
            go = ObjectPool.instance.GetObject(emiGrenadeProjectilePrefab, gunPoint);
        else
            go = Instantiate(emiGrenadeProjectilePrefab, gunPoint.position, Quaternion.identity);

        if (go == null)
            return;

        go.transform.position = gunPoint.position;
        go.transform.rotation = Quaternion.LookRotation(dir.normalized);

        EMI_GrenadeProjectile proj = go.GetComponent<EMI_GrenadeProjectile>();
        if (proj == null)
            proj = go.AddComponent<EMI_GrenadeProjectile>();

        proj.ConfigureAndFire(
            owner: player,
            direction: dir,
            speed: projectileSpeed,
            upwardBoost: upwardBoost,
            useGravity: useGravity,
            lifeTime: projectileLifetime,
            explodeOnHit: explodeOnCollision,
            explosionRadius: radius,
            enemyMask: whatIsEnemy,
            enemyDuration: enemyDuration,
            enemySpeedMultiplier: enemySpeedMultiplier,
            bossDuration: bossDuration,
            bossSpeedMultiplier: bossSpeedMultiplier,
            playerDuration: playerDuration,
            playerSpeedMultiplier: playerSpeedMultiplier,
            explosionVfx: explosionVfx,
            hitAuraVfx: hitAuraVfx,
            radiusIndicatorVfx: radiusIndicatorVfx,
            indicatorLifetime: indicatorLifetime,
            indicatorY: indicatorY,
            indicatorScaleMultiplier: indicatorScaleMultiplier,
            explosionVfxScaleMultiplier: explosionVfxScaleMultiplier,
            auraScaleMultiplier: auraScaleMultiplier
        );
    }
}
