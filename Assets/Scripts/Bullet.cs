using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private Rigidbody rb => GetComponent<Rigidbody>();

    private void OnCollisionEnter(Collision collision)
    {
        //rb.constraints = RigidbodyConstraints.FreezeAll;
        Destroy(gameObject);
    }
}
