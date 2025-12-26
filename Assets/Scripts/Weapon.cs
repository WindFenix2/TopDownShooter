using UnityEngine;

public enum WeaponType
{
    Pistol,
    Revolver,
    AutoRifle,
    Shotgun,
    Rifle
}

public enum ShootType
{
    Single,
    Auto
}

[System.Serializable] // Makes class visible in the inspector.
public class Weapon
{
    public WeaponType weaponType;

    [Header("Shooting specifics")]

    public ShootType shootType;
    public float fireRate = 1; // bullets per second
    private float lastShootTime;

    [Header("Magazine datails")]
    public int bulletsInMagazine;
    public int magazineCapacity;
    public int totalReserveAmmo;

    [Range(1, 3)]
    public float reloadSpeed = 1; // how fast character reloads weapon
    [Range(1, 3)]
    public float equipmentSpeed = 1; // how fast character equips weapon

    

    public bool CanShoot()
    {
        if(HaveEnoughBullets() && ReadyToFire())
        {
            bulletsInMagazine--;
            return true;
        }

        return false;
    }

    private bool ReadyToFire()
    {
        if(Time.time > lastShootTime + 1 / fireRate)
        {
            lastShootTime = Time.time; // 25 seconds
            return true;
        }

        return false;
    }


    #region Reload methods
    public bool CanReload()
    {
        if (bulletsInMagazine == magazineCapacity)
            return false;

        if (totalReserveAmmo > 0)
        {
            return true;
        }

        return false;
    }
    public void RefillBullets()
    {
        //totalReserveAmmo += bulletsInMagazine; 
        // this will add bullets in magazine to total amount of bullets

        int bulletsToReload = magazineCapacity;

        if (bulletsToReload > totalReserveAmmo)
            bulletsToReload = totalReserveAmmo;

        totalReserveAmmo -= bulletsToReload;
        bulletsInMagazine = bulletsToReload;

        if (totalReserveAmmo < 0)
            totalReserveAmmo = 0;
    }
    private bool HaveEnoughBullets() => bulletsInMagazine > 0;
    #endregion
}
