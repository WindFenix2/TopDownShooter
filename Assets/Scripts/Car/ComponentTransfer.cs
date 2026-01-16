using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentTransfer : MonoBehaviour
{

    [ContextMenu("Transfer Components to Child")]
    public void TransferComponentsToChild()
    {
        GameObject selectedGameObject = transform.gameObject;

        if (selectedGameObject == null) return;

        // Create a new GameObject as a child
        GameObject childGameObject = new GameObject("Car_Wheel_Mesh");
        childGameObject.transform.SetParent(selectedGameObject.transform);
        childGameObject.transform.localPosition = Vector3.zero;
        childGameObject.transform.localRotation = Quaternion.identity;

        // Transfer Mesh Filter component
        MeshFilter meshFilter = selectedGameObject.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            MeshFilter childMeshFilter = childGameObject.AddComponent<MeshFilter>();
            childMeshFilter.sharedMesh = meshFilter.sharedMesh;
            DestroyImmediate(meshFilter);
        }

        // Transfer Mesh Renderer component
        MeshRenderer meshRenderer = selectedGameObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            MeshRenderer childMeshRenderer = childGameObject.AddComponent<MeshRenderer>();
            childMeshRenderer.sharedMaterials = meshRenderer.sharedMaterials;
            DestroyImmediate(meshRenderer);
        }
    }
}
