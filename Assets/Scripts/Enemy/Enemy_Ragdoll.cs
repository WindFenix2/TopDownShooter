using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Ragdoll : MonoBehaviour
{
    [SerializeField] private Transform ragdollParent;

    [SerializeField] private Collider[] ragdollColliders;
    [SerializeField] private Rigidbody[] ragdollRigidbodies;

    private void Awake()
    {
        ragdollColliders = ragdollParent.GetComponentsInChildren<Collider>(true);
        ragdollRigidbodies = ragdollParent.GetComponentsInChildren<Rigidbody>(true);

        RagdollActive(false);
    }

    public void RagdollActive(bool active)
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = !active;
        }
    }

}
