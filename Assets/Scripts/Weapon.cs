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
    public int ammo;
    public int maxAmmo;

    public bool CanShoot()
    {
        return HaveEnoughBullets();
    }

    private bool HaveEnoughBullets()
    {
        if (ammo > 0)
        {
            ammo--;
            return true;
        }

        return false;
    }
}
