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
    public int bulletsPerShot;
    public float defaultFireRate;
    public float fireRate = 1; // bullets per second
    private float lastShootTime;

    [Header("Burst fire")]
    public bool burstAvailable;
    public bool burstActive;

    public int burstBulletsPerShot;
    public float burstFireRate;
    public float burstFireDelay = .1f;

    [Header("Magazine datails")]
    public int bulletsInMagazine;
    public int magazineCapacity;
    public int totalReserveAmmo;

    [Range(1, 3)]
    public float reloadSpeed = 1; // how fast character reloads weapon
    [Range(1, 3)]
    public float equipmentSpeed = 1; // how fast character equips weapon

    [Header("Spread")]
    public float baseSpread = 1;
    public float maximumSpread = 3;
    private float currentSpread = 2;

    public float spreadIncreaseRate = .15f;

    private float lastSpreadUpdateTime;
    private float spreadCooldown = 1;

    #region Spread methods

    public Vector3 ApplySpread(Vector3 originalDirection)
    {
        UpdateSpread();

        float randomizedValue = Random.Range(-currentSpread, currentSpread);

        Quaternion spreadRotation = Quaternion.Euler(randomizedValue, randomizedValue, randomizedValue);

        return spreadRotation * originalDirection;
    }

    private void UpdateSpread()
    {
        if (Time.time > lastSpreadUpdateTime + spreadCooldown)
            currentSpread = baseSpread;
        else
            IncreaseSpread();


        lastSpreadUpdateTime = Time.time;
    }

    private void IncreaseSpread()
    {
        currentSpread = Mathf.Clamp(currentSpread + spreadIncreaseRate, baseSpread, maximumSpread);
    }

    #endregion

    #region Burst methods

    public bool BurstActivated()
    {
        if(weaponType == WeaponType.Shotgun)
        {
            burstFireDelay = 0;
            return true;
        }

        return burstActive;
    }

    public void ToggleBurst()
    {
        if (burstAvailable == false)
            return;

        burstActive = !burstActive;

        if(burstActive)
        {
            bulletsPerShot = burstBulletsPerShot;
            fireRate = burstFireRate;
        }
        else
        {
            bulletsPerShot = 1;
            fireRate = defaultFireRate;
        }
    }

    #endregion

    public bool CanShoot() => HaveEnoughBullets() && ReadyToFire();

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
