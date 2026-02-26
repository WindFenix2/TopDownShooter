using UnityEngine;

public class MissionSpawnPoint : MonoBehaviour
{
    public enum SpawnCategory { Vehicle, Gasoline, MissionItem }

    [Header("Config")]
    public SpawnCategory category = SpawnCategory.Vehicle;
    public GameObject prefab;

    [Header("Gizmo")]
    [SerializeField] private float gizmoRadius = 1f;

    public GameObject Spawn()
    {
        if (prefab == null)
            return null;

        return Instantiate(prefab, transform.position, transform.rotation);
    }

    public static GameObject[] SpawnRandom(MissionSpawnPoint[] allPoints, int count)
    {
        if (allPoints == null || allPoints.Length == 0 || count <= 0)
            return new GameObject[0];

        count = Mathf.Min(count, allPoints.Length);

        MissionSpawnPoint[] shuffled = (MissionSpawnPoint[])allPoints.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            MissionSpawnPoint temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        GameObject[] spawned = new GameObject[count];
        for (int i = 0; i < count; i++)
        {
            spawned[i] = shuffled[i].Spawn();
        }

        return spawned;
    }

    private void OnDrawGizmos()
    {
        switch (category)
        {
            case SpawnCategory.Vehicle:
                Gizmos.color = new Color(1f, 0.5f, 0f);
                break;
            case SpawnCategory.Gasoline:
                Gizmos.color = new Color(0.8f, 0.2f, 0.2f);
                break;
            case SpawnCategory.MissionItem:
                Gizmos.color = Color.white;
                break;
        }

        Gizmos.DrawWireCube(transform.position, Vector3.one * gizmoRadius);
        Gizmos.DrawIcon(transform.position, "d_Prefab Icon", true);
    }
}
