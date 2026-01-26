using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public GameObject originalPrefab;

    [HideInInspector] public bool isInPool;
    [HideInInspector] public bool returnScheduled;
}
