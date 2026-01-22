using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_WeaponController : MonoBehaviour
{
    [SerializeField] private LayerMask whatIsAlly;

    private Player player;
    private const float REFERENCE_BULLET_SPEED = 20;

    [SerializeField] private List<Weapon_Data> defaultWeaponData;
    [SerializeField] private Weapon currentWeapon;
    private bool weaponReady;
    private bool isShooting;

    [Header("Bullet details")]
    [SerializeField] private float bulletImpactForce = 100;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed;

    [SerializeField] private Transform weaponHolder;

    [Header("Inventory")]
    [SerializeField] private int maxSlots = 2;
    [SerializeField] private List<Weapon> weaponSlots;

    [SerializeField] private GameObject weaponPickupPrefab;

    private void Awake()
    {
        if (weaponSlots == null)
            weaponSlots = new List<Weapon>();
    }

    private void Start()
    {
        player = GetComponent<Player>();
        AssignInputEvents();
    }

    private void Update()
    {
        if (isShooting)
            Shoot();
    }

    #region Slots managment - Pickup\Equip\Drop\Ready Weapon

    public void SetDefaultWeapon(List<Weapon_Data> newWeaponData)
    {
        defaultWeaponData = new List<Weapon_Data>(newWeaponData);

        if (weaponSlots == null)
            weaponSlots = new List<Weapon>();

        weaponSlots.Clear();

        foreach (Weapon_Data weaponData in defaultWeaponData)
        {
            PickupWeapon(new Weapon(weaponData));
        }

        EquipWeapon(0);
    }

    private void EquipWeapon(int i)
    {
        if (weaponSlots == null || i >= weaponSlots.Count)
            return;

        SetWeaponReady(false);

        currentWeapon = weaponSlots[i];

        if (player != null && player.weaponVisuals != null)
            player.weaponVisuals.PlayWeaponEquipAnimation();

        UpdateWeaponUI();
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

        for (int i = 1; i <= currentWeapon.bulletsPerShot; i++)
        {
            FireSingleBullet();

            yield return new WaitForSeconds(currentWeapon.burstFireDelay);

            if (i >= currentWeapon.bulletsPerShot)
                SetWeaponReady(true);
        }
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

        FireSingleBullet();
        TriggerEnemyDodge();
    }

    private void FireSingleBullet()
    {
        if (currentWeapon == null)
            return;

        Transform gunPoint = GunPoint();
        if (gunPoint == null)
            return;

        currentWeapon.bulletsInMagazine--;
        UpdateWeaponUI();

        if (player != null && player.weaponVisuals != null && player.weaponVisuals.CurrentWeaponModel() != null)
        {
            var model = player.weaponVisuals.CurrentWeaponModel();
            if (model.fireSFX != null) model.fireSFX.Play();
        }

        if (ObjectPool.instance == null || bulletPrefab == null)
            return;

        GameObject newBullet = ObjectPool.instance.GetObject(bulletPrefab, gunPoint);
        newBullet.transform.rotation = Quaternion.LookRotation(gunPoint.forward);

        Rigidbody rbNewBullet = newBullet.GetComponent<Rigidbody>();
        Bullet bulletScript = newBullet.GetComponent<Bullet>();

        if (bulletScript != null)
            bulletScript.BulletSetup(whatIsAlly, currentWeapon.bulletDamage, currentWeapon.gunDistance, bulletImpactForce);

        Vector3 bulletsDirection = currentWeapon.ApplySpread(BulletDirection());

        if (rbNewBullet != null)
        {
            rbNewBullet.mass = REFERENCE_BULLET_SPEED / bulletSpeed;
            rbNewBullet.velocity = bulletsDirection * bulletSpeed;
        }
    }

    private void Reload()
    {
        SetWeaponReady(false);

        if (player != null && player.weaponVisuals != null)
            player.weaponVisuals.PlayReloadAnimation();

        if (player != null && player.weaponVisuals != null && player.weaponVisuals.CurrentWeaponModel() != null)
        {
            var model = player.weaponVisuals.CurrentWeaponModel();
            if (model.realodSfx != null) model.realodSfx.Play();
        }
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

        PlayerControls controls = player.controls;

        controls.Character.Fire.performed += context => isShooting = true;
        controls.Character.Fire.canceled += context => isShooting = false;

        controls.Character.EquipSlot1.performed += context => EquipWeapon(0);
        controls.Character.EquipSlot2.performed += context => EquipWeapon(1);
        controls.Character.EquipSlot3.performed += context => EquipWeapon(2);
        controls.Character.EquipSlot4.performed += context => EquipWeapon(3);
        controls.Character.EquipSlot5.performed += context => EquipWeapon(4);

        controls.Character.DropCurrentWeapon.performed += context => DropWeapon();

        controls.Character.Reload.performed += context =>
        {
            if (currentWeapon != null && currentWeapon.CanReload() && WeaponReady())
                Reload();
        };

        controls.Character.ToogleWeaponMode.performed += context =>
        {
            if (currentWeapon != null)
                currentWeapon.ToggleBurst();
        };
    }

    #endregion
}
