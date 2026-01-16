using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car_DamageZone : MonoBehaviour
{

    private Car_Controller carController;

    [SerializeField] private float minSpeedToDamage = 1.5f;

    [SerializeField] private int carDamage;
    [SerializeField] private float impactForce = 150;
    [SerializeField] private float upwardsMultiplier = 3;

    private void Awake()
    {
        carController = GetComponentInParent<Car_Controller>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (carController.rb.velocity.magnitude < minSpeedToDamage)
            return;

        IDamagable damagable = other.GetComponent<IDamagable>();
        if (damagable == null)
            return;

        damagable.TakeDamage(carDamage);

        Rigidbody rb = other.GetComponent<Rigidbody>();
        
        if(rb != null )
            ApplyForce(rb);
    }

    private void ApplyForce(Rigidbody rigidbody)
    {
        rigidbody.isKinematic = false;
        rigidbody.AddExplosionForce(impactForce, transform.position, 3, upwardsMultiplier, ForceMode.Impulse);
    }

}
