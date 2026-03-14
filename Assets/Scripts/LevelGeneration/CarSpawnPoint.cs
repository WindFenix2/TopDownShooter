using UnityEngine;

public class CarSpawnPoint : MonoBehaviour
{
    [Header("Spawn Config")]
    public GameObject[] possibleCars;

    [Header("Gizmo")]
    [SerializeField] private float gizmoRadius = 1.5f;

    public GameObject SpawnCar()
    {
        if (possibleCars == null || possibleCars.Length == 0)
            return null;

        int randomIndex = Random.Range(0, possibleCars.Length);
        GameObject chosenPrefab = possibleCars[randomIndex];

        if (chosenPrefab == null)
            return null;

        GameObject spawned = Instantiate(chosenPrefab, transform.position, transform.rotation);
        return spawned;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(gizmoRadius, gizmoRadius * 0.5f, gizmoRadius));
        Gizmos.DrawIcon(transform.position, "d_Prefab Icon", true);
    }
}
