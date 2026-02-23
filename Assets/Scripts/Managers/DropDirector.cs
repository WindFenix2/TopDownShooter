using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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


    [Header("Safe spawn")]
    [Tooltip("Minimum distance from the player for a drop to spawn.")]
    [SerializeField] private float minDistFromPlayer = 2f;
    [Tooltip("Radius used to check for obstacles (walls) around the spawn point.")]
    [SerializeField] private float safeCheckRadius = 0.5f;
    [Tooltip("How far from the original point we try to offset when blocked.")]
    [SerializeField] private float offsetRange = 1.5f;
    [Tooltip("Height above ground at which the obstacle check sphere is centered.")]
    [SerializeField] private float obstacleCheckHeight = 0.5f;
    [Tooltip("Max attempts to find a valid position before giving up.")]
    [SerializeField] private int maxPlacementAttempts = 5;
    [Tooltip("Layer(s) that count as obstacles (walls, environment). Set in Inspector!")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("NavMesh sample distance. Keep small — we only need nearby walkable area.")]
    [SerializeField] private float navMeshSampleRange = 3f;


    [Header("Ground cap")]
    [Tooltip("Maximum medkit pickups allowed on the ground at once.")]
    [SerializeField] private int maxMedkitsOnGround = 3;
    [Tooltip("Maximum ammo pickups allowed on the ground at once.")]
    [SerializeField] private int maxAmmoOnGround = 4;

    private readonly List<GameObject> activeMedkits = new List<GameObject>();
    private readonly List<GameObject> activeAmmo = new List<GameObject>();


    [Header("Pity / budget")]
    [Tooltip("After this many kills without a drop, the next kill guarantees a drop (pity).")]
    [SerializeField] private int pityThreshold = 4;
    [Tooltip("After a drop, at least this many kills must pass before another drop (budget cooldown).")]
    [SerializeField] private int budgetCooldown = 1;
    [Tooltip("Small chance to override the budget cooldown and drop anyway.")]
    [Range(0f, 1f)]
    [SerializeField] private float budgetOverrideChance = 0.15f;

    private int killsSinceLastDrop;


    [Header("Pickup cooldown")]
    [Tooltip("Duration (seconds) during which the picked-up type has reduced drop weight.")]
    [SerializeField] private float pickupCooldownDuration = 30f;
    [Tooltip("Weight multiplier while on cooldown (0.15 = 15% of normal weight).")]
    [Range(0f, 1f)]
    [SerializeField] private float cooldownWeightMultiplier = 0.15f;

    private float ammoCooldownUntil;
    private float medkitCooldownUntil;


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
                dir.totalKilled++;
                dir.alive = Mathf.Max(0, dir.alive - 1);
                dir.registeredEnemies.Remove(id);
            }
        }

        dir.killsSinceLastDrop++;

        if (!dir.RollShouldDrop())
            return;

        dir.killsSinceLastDrop = 0;

        DropType type = dir.ChooseDropType();
        dir.SpawnDrop(type, enemyPosition);
    }


    private bool RollShouldDrop()
    {

        if (killsSinceLastDrop >= pityThreshold)
        {
            Debug.Log($"[DropDirector] Pity triggered after {killsSinceLastDrop} kills without drop.");
            return true;
        }


        if (killsSinceLastDrop <= budgetCooldown)
        {

            if (Random.value < budgetOverrideChance)
            {
                Debug.Log("[DropDirector] Budget cooldown overridden by lucky roll.");
                return true;
            }

            return false;
        }


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


        if (healthNeed01 <= 0.01f)
            medW *= 0.25f;


        if (Time.time < ammoCooldownUntil)
        {
            ammoW *= cooldownWeightMultiplier;
            Debug.Log($"[DropDirector] Ammo on cooldown (until {ammoCooldownUntil:F1}s). Weight reduced.");
        }

        if (Time.time < medkitCooldownUntil)
        {
            medW *= cooldownWeightMultiplier;
            Debug.Log($"[DropDirector] Medkit on cooldown (until {medkitCooldownUntil:F1}s). Weight reduced.");
        }

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


        Vector3 pos = FindSafeDropPosition(enemyPosition);
        pos += spawnOffset;


        EnforceGroundCap(type);


        GameObject spawnedGo = null;
        if (ObjectPool.instance != null)
        {
            if (tempSpawnPoint != null)
                tempSpawnPoint.position = pos;

            spawnedGo = ObjectPool.instance.GetObject(prefab, tempSpawnPoint != null ? tempSpawnPoint : transform);
            if (spawnedGo != null)
                spawnedGo.transform.position = pos;
        }
        else
        {
            spawnedGo = Instantiate(prefab, pos, Quaternion.identity);
        }

        if (spawnedGo != null)
        {
            Interactable interactable = spawnedGo.GetComponent<Interactable>();
            if (interactable != null)
                interactable.spawnedByDropDirector = true;

            RegisterActiveDrop(spawnedGo, type);
        }
    }


    private Vector3 FindSafeDropPosition(Vector3 origin)
    {
        Player p = GetPlayer();
        Vector3 playerPos = (p != null) ? p.transform.position : Vector3.zero;
        bool hasPlayer = (p != null);

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {

            Vector3 candidate = origin;
            if (attempt == 0)
            {

                Vector2 smallOffset = Random.insideUnitCircle.normalized * Random.Range(0.3f, 0.8f);
                candidate += new Vector3(smallOffset.x, 0f, smallOffset.y);
            }
            else
            {
                Vector2 rndCircle = Random.insideUnitCircle * offsetRange;
                candidate = origin + new Vector3(rndCircle.x, 0f, rndCircle.y);
            }


            NavMeshHit navHit;
            if (!NavMesh.SamplePosition(candidate, out navHit, navMeshSampleRange, NavMesh.AllAreas))
            {
                Debug.Log($"[DropDirector] Attempt {attempt}: NavMesh check failed at {candidate}");
                continue;
            }

            candidate = navHit.position;


            Vector3 checkCenter = candidate + Vector3.up * obstacleCheckHeight;
            if (Physics.CheckSphere(checkCenter, safeCheckRadius, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                Debug.Log($"[DropDirector] Attempt {attempt}: Obstacle overlap at {candidate}");
                continue;
            }


            if (hasPlayer && Vector3.Distance(candidate, playerPos) < minDistFromPlayer)
            {
                Debug.Log($"[DropDirector] Attempt {attempt}: Too close to player ({Vector3.Distance(candidate, playerPos):F1}m < {minDistFromPlayer}m)");
                continue;
            }

            return candidate;
        }

        Debug.Log("[DropDirector] Safe spawn: no ideal position found, using original enemy position as fallback.");
        return origin;
    }


    private void EnforceGroundCap(DropType type)
    {
        if (type == DropType.Ammo)
        {
            CleanNullEntries(activeAmmo);
            while (activeAmmo.Count >= maxAmmoOnGround && activeAmmo.Count > 0)
                EvictOldest(activeAmmo);
        }
        else if (type == DropType.Medkit)
        {
            CleanNullEntries(activeMedkits);
            while (activeMedkits.Count >= maxMedkitsOnGround && activeMedkits.Count > 0)
                EvictOldest(activeMedkits);
        }
    }

    private void EvictOldest(List<GameObject> list)
    {
        GameObject oldest = list[0];
        list.RemoveAt(0);

        if (oldest == null)
            return;

        Debug.Log($"[DropDirector] Ground cap reached. Evicting oldest pickup: {oldest.name}");

        if (ObjectPool.instance != null)
            ObjectPool.instance.ReturnObject(oldest);
        else
            Destroy(oldest);
    }

    private void CleanNullEntries(List<GameObject> list)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i] == null)
                list.RemoveAt(i);
        }
    }

    private void RegisterActiveDrop(GameObject go, DropType type)
    {
        if (type == DropType.Ammo)
            activeAmmo.Add(go);
        else if (type == DropType.Medkit)
            activeMedkits.Add(go);
    }


    public void UnregisterActiveDrop(GameObject go, DropType type)
    {
        if (type == DropType.Ammo)
            activeAmmo.Remove(go);
        else if (type == DropType.Medkit)
            activeMedkits.Remove(go);
    }



    public void NotifyAmmoPickedUp()
    {
        ammoCooldownUntil = Time.time + pickupCooldownDuration;
        Debug.Log($"[DropDirector] Ammo picked up. Cooldown active for {pickupCooldownDuration}s.");
    }


    public void NotifyMedkitPickedUp()
    {
        medkitCooldownUntil = Time.time + pickupCooldownDuration;
        Debug.Log($"[DropDirector] Medkit picked up. Cooldown active for {pickupCooldownDuration}s.");
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