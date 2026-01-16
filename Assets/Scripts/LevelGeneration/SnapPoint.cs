using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SnapPointType { Enter, Exit}
public class SnapPoint : MonoBehaviour
{
    public SnapPointType pointType;

    private void Start()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        if(boxCollider != null )
            boxCollider.enabled = false;  

        if(meshRenderer != null )
            meshRenderer.enabled = false;
    }

    private void OnValidate()
    {
        gameObject.name = "SnapPoint - " + pointType.ToString();
    }
}
