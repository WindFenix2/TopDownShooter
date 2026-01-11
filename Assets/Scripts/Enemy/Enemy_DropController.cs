using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_DropController : MonoBehaviour
{
    [SerializeField] private GameObject missionObjectKey;

    public void GiveKey(GameObject newKey) => missionObjectKey = newKey;

    public void DropItems()
    {
        if(missionObjectKey != null)
            CreateItem(missionObjectKey);
    }


    private void CreateItem(GameObject go)
    {
        GameObject newItem = Instantiate(go, transform.position + Vector3.up,Quaternion.identity);

    }
}
