using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_DropController : MonoBehaviour
{
    [SerializeField] private GameObject missionObjectKey;

    [Header("Resource drops")]
    [SerializeField] private bool enableResourceDrops = true;

    public void GiveKey(GameObject newKey) => missionObjectKey = newKey;

    public void DropItems()
    {
        if (missionObjectKey != null)
            CreateItem(missionObjectKey);

        if (enableResourceDrops)
            DropDirector.HandleEnemyDeath(gameObject, transform.position);
    }


    private void CreateItem(GameObject go)
    {
        GameObject newItem = Instantiate(go, transform.position + Vector3.up, Quaternion.identity);

    }
}