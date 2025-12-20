using UnityEngine;

public enum WeaponType
{
    Pistol,
    Revolver,
    AutoRifle,
    Shotgun,
    Rifle
}


[System.Serializable] // Makes class visible in the inspector.
public class Weapon
{
    public WeaponType weaponType;


    public int bulletsInMagazine;
    public int magazineCapacity;
    public int totalReserveAmmo;

    [Range(1, 3)]
    public float reloadSpeed = 1; // how fast character reloads weapon
    [Range(1, 3)]
    public float equipmentSpeed = 1; // how fast character equips weapon

    public bool CanShoot()
    {
        return HaveEnoughBullets();
    }

    private bool HaveEnoughBullets()
    {
        if (bulletsInMagazine > 0)
        {
            bulletsInMagazine--;
            return true;
        }

        return false;
    }

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

}
