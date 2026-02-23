using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    [SerializeField] private Transform ragdollParent;

    private float settleDelay = 5f;

    private Collider[] ragdollColliders;
    private Rigidbody[] ragdollRigidbodies;
    private bool isFrozen;

    private void Awake()
    {
        ragdollColliders = GetComponentsInChildren<Collider>();
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();

        RagdollActive(false);

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    public void RagdollActive(bool active)
    {
        isFrozen = false;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = !active;

            if (active)
            {
                rb.sleepThreshold = 0f;
            }
        }

        if (active && settleDelay > 0f)
            StartCoroutine(FreezeAfterSettle());
    }

    private IEnumerator FreezeAfterSettle()
    {
        yield return new WaitForSeconds(settleDelay);

        isFrozen = true;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    public void WakeUp()
    {
        if (isFrozen) return;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != null && !rb.isKinematic)
                rb.WakeUp();
        }
    }

    public void CollidersActive(bool active)
    {
        foreach (Collider cd in ragdollColliders)
        {
            cd.enabled = active;
        }
    }
}
