using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnPoint : MonoBehaviour
{
    [Header("Spawn Config")]
    public EnemyType spawnType = EnemyType.Melee;
    public GameObject[] possibleEnemies;

    [Header("Weapon Override (Range enemies)")]
    public Weapon_Data[] possibleWeapons;

    [Header("Spawn Timing")]
    public float minSpawnDelay = 0f;
    public float maxSpawnDelay = 2f;

    [Header("Gizmo")]
    [SerializeField] private float gizmoRadius = 0.5f;

    public Enemy SpawnRandomEnemy()
    {
        if (possibleEnemies == null || possibleEnemies.Length == 0)
            return null;

        int randomIndex = Random.Range(0, possibleEnemies.Length);
        GameObject chosenPrefab = possibleEnemies[randomIndex];

        if (chosenPrefab == null)
            return null;

        GameObject spawned = Instantiate(chosenPrefab, transform.position, transform.rotation);
        Enemy enemy = spawned.GetComponent<Enemy>();

        if (enemy == null)
        {
            Destroy(spawned);
            return null;
        }

        if (possibleWeapons != null && possibleWeapons.Length > 0)
        {
            Enemy_RangeWeaponModel weaponModel = spawned.GetComponentInChildren<Enemy_RangeWeaponModel>();
            if (weaponModel != null)
            {
                int weaponIndex = Random.Range(0, possibleWeapons.Length);
            }
        }

        spawned.SetActive(false);
        return enemy;
    }

    public Enemy SpawnRandomEnemyFromPool()
    {
        if (possibleEnemies == null || possibleEnemies.Length == 0)
            return null;

        int randomIndex = Random.Range(0, possibleEnemies.Length);
        GameObject chosenPrefab = possibleEnemies[randomIndex];

        if (chosenPrefab == null)
            return null;

        GameObject spawned = ObjectPool.instance.GetObject(chosenPrefab, transform);
        Enemy enemy = spawned?.GetComponent<Enemy>();

        return enemy;
    }

    public float GetRandomDelay()
    {
        return Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    private void OnDrawGizmos()
    {
        switch (spawnType)
        {
            case EnemyType.Melee:
                Gizmos.color = Color.red;
                break;
            case EnemyType.Range:
                Gizmos.color = Color.blue;
                break;
            case EnemyType.Boss:
                Gizmos.color = Color.magenta;
                break;
            default:
                Gizmos.color = Color.yellow;
                break;
        }

        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
        Gizmos.DrawIcon(transform.position, "d_NavMeshAgent Icon", true);
    }
}
