using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    private Player player;
    private const float REFERENCE_BULLET_SPEED = 20;
    //This is the default speed from which our mass formula is derived.

    [SerializeField] private Weapon currentWeapon;


    [Header("Bullet details")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private Transform gunPoint;


    [SerializeField] private Transform weaponHolder;

    [Header("Inventory")]

    [SerializeField] private int maxSlots = 2;
    [SerializeField] private List<Weapon> weaponSlots;

    private void Start()
    {
        player = GetComponent<Player>();
        AssignInputEvents();

        Invoke("EquipStartingWeapon", .1f);
    }

    #region Slots managment - Pickup\Equip\Drop Weapon

    private void EquipStartingWeapon() => EquipWeapon(0);

    private void EquipWeapon(int i)
    {
        currentWeapon = weaponSlots[i];
        player.weaponVisuals.PlayWeaponEquipAnimation();
    }

    public void PickupWeapon(Weapon newWeapon)
    {
        if (weaponSlots.Count >= maxSlots)
        {
            Debug.Log("No slots availible");
            return;
        }

        weaponSlots.Add(newWeapon);
        player.weaponVisuals.SwitchOnBackupWeaponModel();
    }

    private void DropWeapon()
    {
        if (HasOnlyOneWeapon())
            return;
        
        weaponSlots.Remove(currentWeapon);
        EquipWeapon(0);
    }
    #endregion

    private void Shoot()
    {
        if (currentWeapon.CanShoot() == false)
            return;

        GameObject newBullet = ObjectPool.instance.GetBullet();

        newBullet.transform.position = gunPoint.position;
        newBullet.transform.rotation = Quaternion.LookRotation(gunPoint.forward);

        Rigidbody rbNewBullet = newBullet.GetComponent<Rigidbody>();

        rbNewBullet.mass = REFERENCE_BULLET_SPEED / bulletSpeed;
        rbNewBullet.velocity = BulletDirection() * bulletSpeed;

        GetComponentInChildren<Animator>().SetTrigger("Fire");
    }

    public Vector3 BulletDirection()
    {
        Transform aim = player.aim.Aim();

        Vector3 direction = (aim.position - gunPoint.position).normalized;

        if(player.aim.CanAimPrecisly() == false && player.aim.Target() == null)
            direction.y = 0;

        //weaponHolder.LookAt(aim);
        //gunPoint.LookAt(aim); TODO: find a better place for it.

        return direction;
    }


    public bool HasOnlyOneWeapon() => weaponSlots.Count <= 1;

    public Weapon CurrentWeapon() => currentWeapon;

    public Weapon BackupWeapon()
    {
        foreach (Weapon weapon in weaponSlots)
        {
            if (weapon != currentWeapon)
                return weapon;
        }

        return null;
    }

    public Transform GunPoint() => gunPoint;

    #region Input Event
    private void AssignInputEvents()
    {
        PlayerControls controls = player.controls;

        player.controls.Character.Fire.performed += context => Shoot();

        controls.Character.EquipSlot1.performed += context => EquipWeapon(0);
        controls.Character.EquipSlot2.performed += context => EquipWeapon(1);

        controls.Character.DropCurrentWeapon.performed += context => DropWeapon();

        controls.Character.Reload.performed += context =>
        {
            if (currentWeapon.CanReload())
            {
                player.weaponVisuals.PlayReloadAnimation();
            }
        };

    }

    #endregion
}
