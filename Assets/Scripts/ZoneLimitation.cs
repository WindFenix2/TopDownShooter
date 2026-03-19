using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneLimitation : MonoBehaviour
{
    private ParticleSystem[] lines;
    private BoxCollider zoneCollider;
    private int overlapCount;

    private void Start()
    {
        GetComponent<MeshRenderer>().enabled = false;
        zoneCollider = GetComponent<BoxCollider>();
        lines = GetComponentsInChildren<ParticleSystem>();
        ActivateWall(false);
    }


    private void ActivateWall(bool activate)
    {
        foreach(var line in lines)
        {
            if (activate)
            {
                line.Play();
            }
            else
            {
                line.Stop();
            }
        }

        zoneCollider.isTrigger = !activate;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detect player, car, or any rigidbody approaching the boundary
        if (other.GetComponentInParent<Player>() != null ||
            other.GetComponentInParent<Car_Controller>() != null ||
            other.attachedRigidbody != null)
        {
            overlapCount++;
            if (overlapCount == 1)
                ActivateWall(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<Player>() != null ||
            other.GetComponentInParent<Car_Controller>() != null ||
            other.attachedRigidbody != null)
        {
            overlapCount = Mathf.Max(0, overlapCount - 1);
            if (overlapCount == 0)
                ActivateWall(false);
        }
    }
}
