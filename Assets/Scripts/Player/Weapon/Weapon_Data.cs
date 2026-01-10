using UnityEngine;


[CreateAssetMenu(fileName = "New Weapon Data", menuName = "Weapon System/Weapon Data")]
public class Weapon_Data : ScriptableObject
{
    public string weaponName;

    [Header("Bullet info")]
    public int bulletDamage;

    [Header("Magazine details")]
    public int bulletsInMagazine;
    public int magazineCapacity;
    public int totalReserveAmmo;


    [Header("Regular shot")]
    public ShootType shootType;
    public int bulletsPerShot = 1;
    public float fireRate;


    [Header("Burst shot")]
    public bool burstAvalible;
    public bool burstActive;
    public int burstBulletsPerShot;
    public float burstFireRate;
    public float burstFireDelay = .1f;

    [Header("Weapon spread")]
    public float baseSpread;
    public float maxSpread;
    public float spreadIncreaseRate = .15f;

    [Header("Weapon generics")]
    public WeaponType weaponType;
    [Range(1, 3)]
    public float reloadSpeed = 1;
    [Range(1, 3)]
    public float equipmentSpeed = 1;
    [Range(4, 25)]
    public float gunDistance = 4;
    [Range(4, 10)]
    public float cameraDistance = 6;
}
