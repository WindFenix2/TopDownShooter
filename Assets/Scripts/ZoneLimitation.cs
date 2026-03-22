using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoneLimitation : MonoBehaviour
{
    private ParticleSystem[] lines;
    private BoxCollider zoneCollider;

    private void Start()
    {
        GetComponent<MeshRenderer>().enabled = false;
        zoneCollider = GetComponent<BoxCollider>();
        lines = GetComponentsInChildren<ParticleSystem>();

        // Wall is ALWAYS solid - no more trigger/solid swapping
        // This prevents cars at high speed from bypassing the boundary
        zoneCollider.isTrigger = false;

        foreach (var line in lines)
            line.Stop();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponentInParent<Player>() != null ||
            collision.collider.GetComponentInParent<Car_Controller>() != null)
        {
            StartCoroutine(WallVisualCo());
        }
    }

    private IEnumerator WallVisualCo()
    {
        foreach (var line in lines)
            line.Play();

        yield return new WaitForSeconds(1f);

        foreach (var line in lines)
            line.Stop();
    }
}

