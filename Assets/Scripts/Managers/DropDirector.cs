using System.Collections.Generic;
using UnityEngine;

public enum DropType
{
    None,
    Ammo,
    Medkit
}

public class DropDirector : MonoBehaviour
{
    public static DropDirector instance;

    [Header("Drop prefabs (optional if ObjectPool has them)")]
    [SerializeField] private GameObject ammoPickupPrefab;
    [SerializeField] private GameObject medkitPickupPrefab;

    [Header("Drop chance scaling by total enemies")]
    [Tooltip("If total enemies on the level is <= this value, drops are frequent.")]
    [SerializeField] private int fewEnemiesThreshold = 20;
    [Tooltip("If total enemies on the level is >= this value, drops are rare.")]
    [SerializeField] private int manyEnemiesThreshold = 200;
    [Range(0f, 1f)]
    [SerializeField] private float dropChanceWhenFewEnemies = 0.65f;
    [Range(0f, 1f)]
    [SerializeField] private float dropChanceWhenManyEnemies = 0.12f;

    [Header("Drop type weights")]
    [SerializeField] private float baseAmmoWeight = 1f;
    [SerializeField] private float baseMedkitWeight = 1f;
    [SerializeField] private float healthNeedWeightMultiplier = 3.5f;
    [SerializeField] private float ammoNeedWeightMultiplier = 3.0f;

    [Header("Ammo need thresholds (in magazines)")]
    [Tooltip("<= this mags => ammo need becomes maximum")]
    [SerializeField] private float ammoCriticalMagazines = 1f;
    [Tooltip("<= this mags => ammo need is high")]
    [SerializeField] private float ammoLowMagazines = 2f;
    [Tooltip("<= this mags => ammo need is medium")]
    [SerializeField] private float ammoOkMagazines = 3f;

    [Header("Spawn")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 0.25f, 0);

    private readonly HashSet<int> registeredEnemies = new HashSet<int>();
    private int totalSpawned;
    private int totalKilled;
    private int alive;

    private Player cachedPlayer;
    private Transform tempSpawnPoint;

    private static DropDirector EnsureInstance()
    {
        if (instance != null)
            return instance;

        DropDirector existing = FindObjectOfType<DropDirector>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject go = new GameObject("DropDirector");
        instance = go.AddComponent<DropDirector>();
        DontDestroyOnLoad(go);
        return instance;
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (tempSpawnPoint == null)
        {
            GameObject t = new GameObject("_DropSpawnPoint");
            t.transform.SetParent(transform);
            tempSpawnPoint = t.transform;
        }
    }

    private void Start()
    {
        // In case some enemies exist in the scene before they had a chance to register.
        TryRegisterSceneEnemies();
    }

    private void TryRegisterSceneEnemies()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        if (enemies == null)
            return;

        for (int i = 0; i < enemies.Length; i++)
            RegisterEnemy(enemies[i]);
    }

    public static void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        DropDirector dir = EnsureInstance();

        int id = enemy.GetInstanceID();
        if (dir.registeredEnemies.Contains(id))
            return;

        dir.registeredEnemies.Add(id);
        dir.totalSpawned++;
        dir.alive++;
    }

    public static void HandleEnemyDeath(GameObject enemyGo, Vector3 enemyPosition)
    {
        DropDirector dir = EnsureInstance();

        if (enemyGo != null)
        {
            int id = enemyGo.GetInstanceID();
            if (dir.registeredEnemies.Contains(id))
            {
                // If it was registered as an enemy, keep counters consistent.
                dir.totalKilled++;
                dir.alive = Mathf.Max(0, dir.alive - 1);
                dir.registeredEnemies.Remove(id);
            }
        }

        if (!dir.RollShouldDrop())
            return;

        DropType type = dir.ChooseDropType();
        dir.SpawnDrop(type, enemyPosition);
    }

    private bool RollShouldDrop()
    {
        int totalEnemies = Mathf.Max(1, totalSpawned);

        float t = Mathf.InverseLerp(fewEnemiesThreshold, manyEnemiesThreshold, totalEnemies);
        float chance = Mathf.Lerp(dropChanceWhenFewEnemies, dropChanceWhenManyEnemies, t);
        chance = Mathf.Clamp01(chance);

        return Random.value < chance;
    }

    private DropType ChooseDropType()
    {
        Player p = GetPlayer();

        float ammoW = baseAmmoWeight;
        float medW = baseMedkitWeight;

        float healthNeed01 = 0f;
        if (p != null && p.health != null && p.health.maxHealth > 0)
        {
            healthNeed01 = 1f - Mathf.Clamp01((float)p.health.currentHealth / p.health.maxHealth);
            medW += healthNeed01 * healthNeedWeightMultiplier;
        }

        float ammoNeed01 = GetAmmoNeed01(p);
        ammoW += ammoNeed01 * ammoNeedWeightMultiplier;

        // If player is full HP, make medkit much less likely.
        if (healthNeed01 <= 0.01f)
            medW *= 0.25f;

        float total = Mathf.Max(0.0001f, ammoW + medW);
        float roll = Random.value * total;

        if (roll < ammoW)
            return DropType.Ammo;

        return DropType.Medkit;
    }

    private float GetAmmoNeed01(Player p)
    {
        if (p == null || p.weapon == null)
            return 0f;

        // We look at the "worst" weapon (lowest magazines left) among equipped weapons.
        float worstMagazinesLeft = float.MaxValue;
        bool anyWeapon = false;

        for (int i = 0; i < 5; i++)
        {
            WeaponType wt = (WeaponType)i;
            Weapon w = p.weapon.WeaponInSlots(wt);
            if (w == null)
                continue;

            anyWeapon = true;
            int magCap = Mathf.Max(1, w.magazineCapacity);
            int totalBullets = Mathf.Max(0, w.bulletsInMagazine + w.totalReserveAmmo);
            float magsLeft = (float)totalBullets / magCap;
            if (magsLeft < worstMagazinesLeft)
                worstMagazinesLeft = magsLeft;
        }

        if (!anyWeapon)
            return 0f;

        // Map magazines left to a 0..1 need value.
        if (worstMagazinesLeft <= ammoCriticalMagazines)
            return 1f;

        if (worstMagazinesLeft <= ammoLowMagazines)
            return 0.75f;

        if (worstMagazinesLeft <= ammoOkMagazines)
            return 0.4f;

        return 0.1f;
    }

    private void SpawnDrop(DropType type, Vector3 enemyPosition)
    {
        if (type == DropType.None)
            return;

        ResolvePrefabsFromPoolIfNeeded();

        GameObject prefab = null;
        if (type == DropType.Ammo)
            prefab = ammoPickupPrefab;
        else if (type == DropType.Medkit)
            prefab = medkitPickupPrefab;

        if (prefab == null)
            return;

        Vector3 pos = enemyPosition + spawnOffset;

        // Prefer pool if possible.
        if (ObjectPool.instance != null)
        {
            if (tempSpawnPoint != null)
                tempSpawnPoint.position = pos;

            GameObject go = ObjectPool.instance.GetObject(prefab, tempSpawnPoint != null ? tempSpawnPoint : transform);
            if (go != null)
                go.transform.position = pos;
        }
        else
        {
            Instantiate(prefab, pos, Quaternion.identity);
        }
    }

    private void ResolvePrefabsFromPoolIfNeeded()
    {
        if (ObjectPool.instance == null)
            return;

        if (ammoPickupPrefab == null)
            ammoPickupPrefab = ObjectPool.instance.AmmoPickupPrefab;

        if (medkitPickupPrefab == null)
            medkitPickupPrefab = ObjectPool.instance.MedkitPickupPrefab;
    }

    private Player GetPlayer()
    {
        if (cachedPlayer != null)
            return cachedPlayer;

        cachedPlayer = FindObjectOfType<Player>();
        return cachedPlayer;
    }
}