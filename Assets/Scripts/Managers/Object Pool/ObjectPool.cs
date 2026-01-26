using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool instance;

    [SerializeField] private int poolSize = 10;

    private Dictionary<GameObject, Queue<GameObject>> poolDictionary =
        new Dictionary<GameObject, Queue<GameObject>>();

    [Header("To Initialize")]
    [SerializeField] private GameObject weaponPickup;
    [SerializeField] private GameObject ammoPickup;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        InitializeNewPool(weaponPickup);
        InitializeNewPool(ammoPickup);
    }

    public GameObject GetObject(GameObject prefab, Transform target)
    {
        if (prefab == null)
            return null;

        if (poolDictionary.ContainsKey(prefab) == false)
            InitializeNewPool(prefab);

        if (poolDictionary[prefab].Count == 0)
            CreateNewObject(prefab);

        GameObject objectToGet = poolDictionary[prefab].Dequeue();

        var pooled = objectToGet.GetComponent<PooledObject>();
        if (pooled != null)
        {
            pooled.isInPool = false;
            pooled.returnScheduled = false;
        }

        objectToGet.transform.position = target.position;
        objectToGet.transform.parent = null;
        objectToGet.SetActive(true);

        return objectToGet;
    }

    public void ReturnObject(GameObject objectToReturn, float delay = 0f)
    {
        if (objectToReturn == null)
            return;

        var pooled = objectToReturn.GetComponent<PooledObject>();
        if (pooled == null)
            return;

        if (pooled.isInPool || pooled.returnScheduled)
            return;

        pooled.returnScheduled = true;

        if (delay <= 0f)
            ReturnToPool(objectToReturn);
        else
            StartCoroutine(DelayReturn(delay, objectToReturn));
    }

    private IEnumerator DelayReturn(float delay, GameObject objectToReturn)
    {
        yield return new WaitForSeconds(delay);

        if (objectToReturn != null)
            ReturnToPool(objectToReturn);
    }

    private void ReturnToPool(GameObject objectToReturn)
    {
        var pooled = objectToReturn.GetComponent<PooledObject>();
        if (pooled == null)
            return;

        GameObject originalPrefab = pooled.originalPrefab;
        if (originalPrefab == null)
            return;

        if (poolDictionary.ContainsKey(originalPrefab) == false)
            InitializeNewPool(originalPrefab);

        pooled.returnScheduled = false;

        if (pooled.isInPool)
            return;

        pooled.isInPool = true;

        objectToReturn.SetActive(false);
        objectToReturn.transform.parent = transform;

        poolDictionary[originalPrefab].Enqueue(objectToReturn);
    }

    private void InitializeNewPool(GameObject prefab)
    {
        if (prefab == null)
            return;

        if (poolDictionary.ContainsKey(prefab))
            return;

        poolDictionary[prefab] = new Queue<GameObject>();

        for (int i = 0; i < poolSize; i++)
            CreateNewObject(prefab);
    }

    private void CreateNewObject(GameObject prefab)
    {
        GameObject newObject = Instantiate(prefab, transform);

        var pooled = newObject.GetComponent<PooledObject>();
        if (pooled == null)
            pooled = newObject.AddComponent<PooledObject>();

        pooled.originalPrefab = prefab;
        pooled.isInPool = true;
        pooled.returnScheduled = false;

        newObject.SetActive(false);
        poolDictionary[prefab].Enqueue(newObject);
    }
}
