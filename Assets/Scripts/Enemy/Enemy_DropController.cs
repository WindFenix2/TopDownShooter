using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_DropController : MonoBehaviour
{
    [SerializeField] private GameObject missionObjectKey;

    [Header("Resource drops")]
    [SerializeField] private bool enableResourceDrops = true;

    [Header("Boss drops")]
    [SerializeField] private bool isBoss;
    [Range(0f, 1f)]
    [SerializeField] private float bossWeaponDropChance = 0.5f;

    public void GiveKey(GameObject newKey) => missionObjectKey = newKey;

    public void DropItems()
    {
        if (missionObjectKey != null)
            CreateItem(missionObjectKey);

        if (isBoss)
        {
            DropBossItems();
            return;
        }

        if (enableResourceDrops)
            DropDirector.HandleEnemyDeath(gameObject, transform.position);
    }

    private void DropBossItems()
    {
        Vector3 dropPos = transform.position;
        float dropRadius = 1.5f;
        float angleStep = 120f;
        float startAngle = Random.Range(0f, 360f);


        Vector3 medkitOffset = Quaternion.Euler(0, startAngle, 0) * Vector3.forward * dropRadius;
        SpawnFromPool(ObjectPool.instance.MedkitPickupPrefab, dropPos + medkitOffset);


        Vector3 ammoOffset = Quaternion.Euler(0, startAngle + angleStep, 0) * Vector3.forward * dropRadius;
        GameObject ammoGo = SpawnFromPool(ObjectPool.instance.AmmoPickupPrefab, dropPos + ammoOffset);
        if (ammoGo != null)
        {
            Pickup_Ammo ammoPickup = ammoGo.GetComponent<Pickup_Ammo>();
            if (ammoPickup != null)
                ammoPickup.SetBoxType(AmmoBoxType.bigBox);
        }


        if (Random.value < bossWeaponDropChance && ObjectPool.instance != null)
        {
            Vector3 weaponOffset = Quaternion.Euler(0, startAngle + angleStep * 2, 0) * Vector3.forward * dropRadius;
            GameObject weaponPrefab = ObjectPool.instance.WeaponPickupPrefab;
            if (weaponPrefab != null)
            {
                GameObject weaponGo = SpawnFromPool(weaponPrefab, dropPos + weaponOffset);
                if (weaponGo != null)
                {
                    Pickup_Weapon pickupWeapon = weaponGo.GetComponent<Pickup_Weapon>();
                    if (pickupWeapon != null)
                        pickupWeapon.SetupRandomWeapon();
                }
            }
        }
    }

    private GameObject SpawnFromPool(GameObject prefab, Vector3 position)
    {
        if (prefab == null || ObjectPool.instance == null)
            return null;

        GameObject tempPoint = new GameObject("_TempSpawn");
        tempPoint.transform.position = position + Vector3.up * 0.25f;

        GameObject spawned = ObjectPool.instance.GetObject(prefab, tempPoint.transform);
        Destroy(tempPoint);

        if (spawned != null)
            spawned.transform.position = position + Vector3.up * 0.25f;

        return spawned;
    }

    private void CreateItem(GameObject go)
    {
        GameObject newItem = Instantiate(go, transform.position + Vector3.up, Quaternion.identity);
    }
}