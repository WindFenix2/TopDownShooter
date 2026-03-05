using UnityEngine;

public class PickupSpawnPoint : MonoBehaviour
{
    public enum PickupType { Medkit, Ammo, Weapon }

    [Header("Pickup Config")]
    public PickupType pickupType = PickupType.Ammo;
    public GameObject pickupPrefab;
    public Transform[] possiblePositions;

    [Header("Spawn Chance")]
    [Range(0f, 1f)]
    public float spawnChance = 1f;

    [Header("Gizmo")]
    [SerializeField] private float gizmoRadius = 0.3f;

    private GameObject spawnedPickup;

    public void SpawnPickup()
    {
        if (pickupPrefab == null)
            return;

        if (Random.value > spawnChance)
            return;

        Vector3 spawnPos;
        Quaternion spawnRot;

        if (possiblePositions != null && possiblePositions.Length > 0)
        {
            int randomIndex = Random.Range(0, possiblePositions.Length);
            Transform chosen = possiblePositions[randomIndex];
            spawnPos = chosen.position;
            spawnRot = chosen.rotation;
        }
        else
        {
            spawnPos = transform.position;
            spawnRot = transform.rotation;
        }

        spawnedPickup = Instantiate(pickupPrefab, spawnPos, spawnRot);

        Pickup_Ammo ammo = spawnedPickup.GetComponent<Pickup_Ammo>();
        if (ammo != null)
            ammo.randomizeOnSpawn = true;

        Pickup_Weapon weaponPickup = spawnedPickup.GetComponent<Pickup_Weapon>();
        if (weaponPickup != null)
            weaponPickup.SetupRandomWeapon();
    }

    private void OnDrawGizmos()
    {
        switch (pickupType)
        {
            case PickupType.Medkit:
                Gizmos.color = Color.green;
                break;
            case PickupType.Ammo:
                Gizmos.color = Color.yellow;
                break;
            case PickupType.Weapon:
                Gizmos.color = Color.cyan;
                break;
        }

        Gizmos.DrawWireSphere(transform.position, gizmoRadius);

        if (possiblePositions != null)
        {
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.4f);
            foreach (Transform pos in possiblePositions)
            {
                if (pos != null)
                {
                    Gizmos.DrawWireSphere(pos.position, gizmoRadius);
                    Gizmos.DrawLine(transform.position, pos.position);
                }
            }
        }
    }
}
