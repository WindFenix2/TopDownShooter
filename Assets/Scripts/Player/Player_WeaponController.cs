using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_WeaponController : MonoBehaviour
{
    [SerializeField] private LayerMask whatIsAlly;

    private Player player;
    private const float REFERENCE_BULLET_SPEED = 20f;

    [SerializeField] private List<Weapon_Data> defaultWeaponData;
    [SerializeField] private Weapon currentWeapon;
    private bool weaponReady;
    private bool isShooting;

    [Header("Bullet details")]
    [SerializeField] private float bulletImpactForce = 100f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed = 15f;
    [SerializeField] private float rifleBulletSpeed = 50f;

    [SerializeField] private Transform weaponHolder;

    [Header("Inventory")]
    [SerializeField] private int maxSlots = 2;
    [SerializeField] private List<Weapon> weaponSlots;
    [SerializeField] private GameObject weaponPickupPrefab;

    private Collider[] playerColliders;

    [Header("Pistol - Smart Bullets (Soft homing)")]
    [SerializeField] private float pistolHomingRadius = 2.0f;
    [SerializeField] private float pistolHomingMaxDistanceFromPlayer = 12.0f;
    [SerializeField] private float pistolHomingTimeMin = 0.05f;
    [SerializeField] private float pistolHomingTimeMax = 0.12f;
    [SerializeField] private float pistolHomingTurnSpeedDeg = 900f;

    [Header("Revolver - Stuck bullets")]
    [SerializeField] private GameObject revolverStuckBulletPrefab;
    [SerializeField] private int revolverDetonationDamage = 150;
    [SerializeField] private float revolverDetonationRadius = 2.2f;
    [SerializeField] private LayerMask revolverDetonationWhatToDamage;

    private Revolver_StickyBulletsManager revolverManager;
    private Shotgun_KillShieldAbility shotgunShieldAbility;
    private Sniper_MarkChainShotAbility sniperChainAbility;

    private Coroutine readyFallbackRoutine;
    private bool inputAssigned;

    private void Awake()
    {
        if (weaponSlots == null)
            weaponSlots = new List<Weapon>();
    }

    private void Start()
    {
        player = GetComponent<Player>();

        RefreshPlayerColliders();

        revolverManager = GetComponent<Revolver_StickyBulletsManager>();
        if (revolverManager == null)
            revolverManager = gameObject.AddComponent<Revolver_StickyBulletsManager>();

        shotgunShieldAbility = GetComponent<Shotgun_KillShieldAbility>();
        if (shotgunShieldAbility == null)
            shotgunShieldAbility = gameObject.AddComponent<Shotgun_KillShieldAbility>();

        sniperChainAbility = GetComponent<Sniper_MarkChainShotAbility>();
        if (sniperChainAbility == null)
            sniperChainAbility = gameObject.AddComponent<Sniper_MarkChainShotAbility>();

        if (currentWeapon != null)
        {
            if (shotgunShieldAbility != null)
                shotgunShieldAbility.OnEquippedWeaponChanged(currentWeapon.weaponType);

            if (sniperChainAbility != null)
                sniperChainAbility.OnEquippedWeaponChanged(currentWeapon.weaponType);
        }

        AssignInputEvents();
    }

    private void OnEnable()
    {
        AssignInputEvents();
    }

    private void OnDisable()
    {
        UnassignInputEvents();
    }

    private void Update()
    {
        if (isShooting)
            Shoot();
    }

    private float CurrentBulletSpeed()
    {
        if (currentWeapon != null && currentWeapon.weaponType == WeaponType.Rifle)
            return Mathf.Max(0.01f, rifleBulletSpeed);

        return Mathf.Max(0.01f, bulletSpeed);
    }

    private float CurrentRifleCooldown()
    {
        if (currentWeapon == null)
            return 0.01f;

        float fr = Mathf.Max(0.01f, currentWeapon.fireRate);
        return 1f / fr;
    }

    private void ApplyRifleFireRateCooldown()
    {
        if (currentWeapon == null)
            return;

        if (currentWeapon.weaponType != WeaponType.Rifle)
            return;

        SetWeaponReady(false);
        StartReadyFallback(CurrentRifleCooldown());
    }

    public void RefreshPlayerColliders()
    {
        playerColliders = GetComponentsInChildren<Collider>(true);
    }

    #region Slots

    public void SetDefaultWeapon(List<Weapon_Data> newWeaponData)
    {
        defaultWeaponData = new List<Weapon_Data>(newWeaponData);

        if (weaponSlots == null)
            weaponSlots = new List<Weapon>();

        weaponSlots.Clear();

        foreach (Weapon_Data weaponData in defaultWeaponData)
            PickupWeapon(new Weapon(weaponData));

        EquipWeapon(0);
    }

    private void EquipWeapon(int i)
    {
        if (weaponSlots == null || i < 0 || i >= weaponSlots.Count)
            return;

        Weapon weaponToEquip = weaponSlots[i];
        if (weaponToEquip == null)
            return;

        if (currentWeapon != null && weaponReady && weaponToEquip.weaponType == currentWeapon.weaponType)
            return;

        SetWeaponReady(false);

        currentWeapon = weaponToEquip;

        if (shotgunShieldAbility != null)
            shotgunShieldAbility.OnEquippedWeaponChanged(currentWeapon.weaponType);

        if (sniperChainAbility != null)
            sniperChainAbility.OnEquippedWeaponChanged(currentWeapon.weaponType);

        if (player != null && player.aim != null)
            player.aim.SetRegularAimCameraDistance(currentWeapon.cameraDistance);

        if (player != null && player.weaponVisuals != null)
            player.weaponVisuals.PlayWeaponEquipAnimation();

        UpdateWeaponUI();
        StartReadyFallback(0.25f);
    }

    public void PickupWeapon(Weapon newWeapon)
    {
        if (weaponSlots == null)
            weaponSlots = new List<Weapon>();

        if (newWeapon == null)
            return;

        Weapon existing = WeaponInSlots(newWeapon.weaponType);
        if (existing != null)
        {
            existing.totalReserveAmmo += newWeapon.bulletsInMagazine;
            UpdateWeaponUI();
            return;
        }

        if (weaponSlots.Count >= maxSlots && currentWeapon != null && newWeapon.weaponType != currentWeapon.weaponType)
        {
            int weaponIndex = weaponSlots.IndexOf(currentWeapon);

            if (player != null && player.weaponVisuals != null)
                player.weaponVisuals.SwitchOffWeaponModels();

            weaponSlots[weaponIndex] = newWeapon;

            CreateWeaponOnTheGround();
            EquipWeapon(weaponIndex);
            return;
        }

        weaponSlots.Add(newWeapon);

        if (player != null && player.weaponVisuals != null)
            player.weaponVisuals.SwitchOnBackupWeaponModel();

        UpdateWeaponUI();
    }

    private void DropWeapon()
    {
        if (HasOnlyOneWeapon())
            return;

        CreateWeaponOnTheGround();

        weaponSlots.Remove(currentWeapon);
        EquipWeapon(0);
    }

    private void CreateWeaponOnTheGround()
    {
        if (ObjectPool.instance == null || weaponPickupPrefab == null)
            return;

        GameObject droppedWeapon = ObjectPool.instance.GetObject(weaponPickupPrefab, transform);
        if (droppedWeapon == null) return;

        droppedWeapon.GetComponent<Pickup_Weapon>()?.SetupPickupWeapon(currentWeapon, transform);
    }

    public void SetWeaponReady(bool ready)
    {
        weaponReady = ready;

        if (ready && player != null && player.sound != null && player.sound.weaponReady != null)
            player.sound.weaponReady.Play();
    }

    public bool WeaponReady() => weaponReady;

    #endregion

    public void UpdateWeaponUI()
    {
        if (UI.instance == null || UI.instance.inGameUI == null)
            return;

        UI.instance.inGameUI.UpdateWeaponUI(weaponSlots, currentWeapon);
    }

    private IEnumerator BurstFire()
    {
        SetWeaponReady(false);
        StartReadyFallback(0.6f);

        if (currentWeapon == null)
        {
            SetWeaponReady(true);
            yield break;
        }

        bool shotgunPellets = currentWeapon.weaponType == WeaponType.Shotgun;

        if (shotgunPellets)
        {
            if (currentWeapon.bulletsInMagazine <= 0)
            {
                SetWeaponReady(true);
                yield break;
            }

            currentWeapon.bulletsInMagazine--;
            UpdateWeaponUI();

            if (player != null && player.weaponVisuals != null && player.weaponVisuals.CurrentWeaponModel() != null)
            {
                var model = player.weaponVisuals.CurrentWeaponModel();
                if (model.fireSFX != null) model.fireSFX.Play();
            }
        }

        for (int i = 1; i <= currentWeapon.bulletsPerShot; i++)
        {
            if (shotgunPellets)
                FireSingleBullet(false, false);
            else
                FireSingleBullet(true, true);

            if (currentWeapon.burstFireDelay > 0)
                yield return new WaitForSeconds(currentWeapon.burstFireDelay);
        }

        TriggerEnemyDodge();
        SetWeaponReady(true);
    }

    private void Shoot()
    {
        if (WeaponReady() == false)
            return;

        if (currentWeapon == null || currentWeapon.CanShoot() == false)
            return;

        if (player != null && player.weaponVisuals != null)
            player.weaponVisuals.PlayFireAnimation();

        if (currentWeapon.shootType == ShootType.Single)
            isShooting = false;

        if (currentWeapon.BurstActivated() == true)
        {
            StartCoroutine(BurstFire());
            return;
        }

        if (currentWeapon.weaponType == WeaponType.Rifle && sniperChainAbility != null)
        {
            Transform gunPoint = GunPoint();
            if (gunPoint != null)
            {
                Vector3 dir = currentWeapon.ApplySpread(BulletDirection());
                float spd = CurrentBulletSpeed();

                bool chainTriggered = sniperChainAbility.TryFireChainShot(
                    gunPoint.position,
                    dir,
                    currentWeapon.gunDistance,
                    currentWeapon.bulletDamage,
                    spd
                );

                if (chainTriggered)
                {
                    currentWeapon.bulletsInMagazine--;
                    UpdateWeaponUI();

                    if (player != null && player.weaponVisuals != null && player.weaponVisuals.CurrentWeaponModel() != null)
                    {
                        var model = player.weaponVisuals.CurrentWeaponModel();
                        if (model.fireSFX != null) model.fireSFX.Play();
                    }

                    TriggerEnemyDodge();
                    ApplyRifleFireRateCooldown();
                    return;
                }
            }
        }

        FireSingleBullet();
        TriggerEnemyDodge();

        if (currentWeapon != null && currentWeapon.weaponType == WeaponType.Rifle)
            ApplyRifleFireRateCooldown();
    }

    private void FireSingleBullet(bool consumeAmmo = true, bool playSfx = true)
    {
        if (currentWeapon == null)
            return;

        Transform gunPoint = GunPoint();
        if (gunPoint == null)
            return;

        if (consumeAmmo)
        {
            currentWeapon.bulletsInMagazine--;
            UpdateWeaponUI();
        }

        if (playSfx)
        {
            if (player != null && player.weaponVisuals != null && player.weaponVisuals.CurrentWeaponModel() != null)
            {
                var model = player.weaponVisuals.CurrentWeaponModel();
                if (model.fireSFX != null) model.fireSFX.Play();
            }
        }

        if (ObjectPool.instance == null)
            return;

        bool isRevolver = currentWeapon.weaponType == WeaponType.Revolver;

        GameObject prefabToSpawn = isRevolver ? revolverStuckBulletPrefab : bulletPrefab;
        if (prefabToSpawn == null)
            return;

        GameObject newBullet = ObjectPool.instance.GetObject(prefabToSpawn, gunPoint);
        if (newBullet == null) return;

        newBullet.transform.position = gunPoint.position;
        newBullet.transform.rotation = Quaternion.LookRotation(gunPoint.forward);

        Collider bulletCol = newBullet.GetComponent<Collider>();

        if (playerColliders == null || playerColliders.Length == 0)
            RefreshPlayerColliders();

        if (bulletCol != null && playerColliders != null)
        {
            for (int i = 0; i < playerColliders.Length; i++)
            {
                if (playerColliders[i] == null) continue;
                Physics.IgnoreCollision(bulletCol, playerColliders[i], true);
            }
        }

        Vector3 bulletsDirection = currentWeapon.ApplySpread(BulletDirection());

        Rigidbody rbNewBullet = newBullet.GetComponent<Rigidbody>();
        if (rbNewBullet != null)
        {
            float spd = CurrentBulletSpeed();
            rbNewBullet.mass = REFERENCE_BULLET_SPEED / spd;
            rbNewBullet.velocity = bulletsDirection * spd;
        }

        if (!isRevolver)
        {
            Bullet bulletScript = newBullet.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bool isPlayerBullet = true;
                bulletScript.BulletSetup(whatIsAlly, currentWeapon.bulletDamage, currentWeapon.gunDistance, bulletImpactForce, transform, isPlayerBullet, currentWeapon.weaponType);
            }

            if (currentWeapon.weaponType == WeaponType.Pistol)
                TryApplyPistolSoftHoming(newBullet);
        }
        else
        {
            Revolver_StuckBullet stuck = newBullet.GetComponent<Revolver_StuckBullet>();
            if (stuck != null)
                stuck.Setup(revolverManager);
        }
    }

    private void TryApplyPistolSoftHoming(GameObject bulletObject)
    {
        if (player == null || player.aim == null)
            return;

        Transform aimTr = player.aim.Aim();
        if (aimTr == null)
            return;

        Vector3 aimPoint = aimTr.position;

        if (Vector3.Distance(transform.position, aimPoint) > pistolHomingMaxDistanceFromPlayer)
            return;

        Collider[] hits = Physics.OverlapSphere(aimPoint, pistolHomingRadius);
        if (hits == null || hits.Length == 0)
            return;

        Transform bestTarget = null;
        Vector3 bestTargetLocalOffset = Vector3.zero;
        float bestDist = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            Enemy enemy = hits[i].GetComponentInParent<Enemy>();
            if (enemy == null) continue;
            if (enemy.IsDead) continue;
            if (!enemy.isActiveAndEnabled) continue;

            Collider enemyCol = enemy.GetComponentInChildren<Collider>();
            Vector3 targetWorldPoint = enemy.transform.position + Vector3.up * 1.0f;

            if (enemyCol != null)
                targetWorldPoint = enemyCol.bounds.center;

            float d = Vector3.Distance(aimPoint, targetWorldPoint);
            if (d < bestDist)
            {
                bestDist = d;
                bestTarget = enemy.transform;
                bestTargetLocalOffset = bestTarget.InverseTransformPoint(targetWorldPoint);
            }
        }

        if (bestTarget == null)
            return;

        Pistol_BulletSoftHoming homing = bulletObject.GetComponent<Pistol_BulletSoftHoming>();
        if (homing == null)
            homing = bulletObject.AddComponent<Pistol_BulletSoftHoming>();

        float t = Random.Range(pistolHomingTimeMin, pistolHomingTimeMax);
        homing.EnableHoming(bestTarget, bestTargetLocalOffset, t, pistolHomingTurnSpeedDeg);
    }

    private void OnReloadPressed()
    {
        if (!WeaponReady())
            return;

        if (currentWeapon == null)
            return;

        isShooting = false;

        if (currentWeapon.weaponType == WeaponType.Revolver)
        {
            if (revolverManager != null)
                revolverManager.DetonateAll(revolverDetonationDamage, revolverDetonationRadius, revolverDetonationWhatToDamage);

            if (currentWeapon.CanReload())
                ReloadAnimationOnly();

            return;
        }

        if (currentWeapon.CanReload())
            ReloadAnimationOnly();
    }

    private void ReloadAnimationOnly()
    {
        SetWeaponReady(false);
        StartReadyFallback(0.8f);

        if (player != null && player.weaponVisuals != null)
            player.weaponVisuals.PlayReloadAnimation();

        if (player != null && player.weaponVisuals != null && player.weaponVisuals.CurrentWeaponModel() != null)
        {
            var model = player.weaponVisuals.CurrentWeaponModel();
            if (model.realodSfx != null) model.realodSfx.Play();
        }
    }

    private void StartReadyFallback(float t)
    {
        if (readyFallbackRoutine != null)
            StopCoroutine(readyFallbackRoutine);

        readyFallbackRoutine = StartCoroutine(ReadyFallback(t));
    }

    private IEnumerator ReadyFallback(float t)
    {
        yield return new WaitForSeconds(t);
        if (!weaponReady)
            SetWeaponReady(true);
    }

    public Vector3 BulletDirection()
    {
        if (player == null || player.aim == null)
            return transform.forward;

        Transform aim = player.aim.Aim();
        Transform gunPoint = GunPoint();

        if (aim == null || gunPoint == null)
            return transform.forward;

        Vector3 direction = (aim.position - gunPoint.position).normalized;

        if (player.aim.CanAimPrecisly() == false)
            direction.y = 0;

        return direction;
    }

    public bool HasOnlyOneWeapon() => weaponSlots == null || weaponSlots.Count <= 1;

    public Weapon WeaponInSlots(WeaponType weaponType)
    {
        if (weaponSlots == null)
            return null;

        foreach (Weapon weapon in weaponSlots)
        {
            if (weapon != null && weapon.weaponType == weaponType)
                return weapon;
        }

        return null;
    }

    public Weapon CurrentWeapon() => currentWeapon;

    public Transform GunPoint()
    {
        if (player == null || player.weaponVisuals == null)
            return null;

        var model = player.weaponVisuals.CurrentWeaponModel();
        if (model == null)
            return null;

        return model.gunPoint;
    }

    private void TriggerEnemyDodge()
    {
        Transform gunPoint = GunPoint();
        if (gunPoint == null)
            return;

        Vector3 rayOrigin = gunPoint.position;
        Vector3 rayDirection = BulletDirection();

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, Mathf.Infinity))
        {
            Enemy_Melee enemy_Melee = hit.collider.gameObject.GetComponentInParent<Enemy_Melee>();
            if (enemy_Melee != null)
                enemy_Melee.ActivateDodgeRoll();
        }
    }

    #region Input Events

    private void AssignInputEvents()
    {
        if (player == null)
            return;

        if (inputAssigned)
            return;

        PlayerControls controls = player.controls;
        if (controls == null) return;

        controls.Character.Fire.performed += context => isShooting = true;
        controls.Character.Fire.canceled += context => isShooting = false;

        controls.Character.EquipSlot1.performed += context => EquipWeapon(0);
        controls.Character.EquipSlot2.performed += context => EquipWeapon(1);
        controls.Character.EquipSlot3.performed += context => EquipWeapon(2);
        controls.Character.EquipSlot4.performed += context => EquipWeapon(3);
        controls.Character.EquipSlot5.performed += context => EquipWeapon(4);

        controls.Character.DropCurrentWeapon.performed += context => DropWeapon();

        controls.Character.Reload.performed += context => OnReloadPressed();

        controls.Character.ToogleWeaponMode.performed += context =>
        {
            if (currentWeapon != null)
                currentWeapon.ToggleBurst();
        };

        inputAssigned = true;
    }

    private void UnassignInputEvents()
    {
        if (player == null)
            return;

        if (!inputAssigned)
            return;

        PlayerControls controls = player.controls;
        if (controls == null) return;

        controls.Character.Fire.performed -= context => isShooting = true;
        controls.Character.Fire.canceled -= context => isShooting = false;

        controls.Character.EquipSlot1.performed -= context => EquipWeapon(0);
        controls.Character.EquipSlot2.performed -= context => EquipWeapon(1);
        controls.Character.EquipSlot3.performed -= context => EquipWeapon(2);
        controls.Character.EquipSlot4.performed -= context => EquipWeapon(3);
        controls.Character.EquipSlot5.performed -= context => EquipWeapon(4);

        controls.Character.DropCurrentWeapon.performed -= context => DropWeapon();

        controls.Character.Reload.performed -= context => OnReloadPressed();

        controls.Character.ToogleWeaponMode.performed -= context =>
        {
            if (currentWeapon != null)
                currentWeapon.ToggleBurst();
        };

        inputAssigned = false;
    }

    #endregion
}
