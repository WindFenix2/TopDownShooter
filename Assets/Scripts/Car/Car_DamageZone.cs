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

    private HashSet<int> recentlyHit = new HashSet<int>();
    private float clearTimer;

    private float recentMaxSpeed;
    private float speedDecayTimer;
    private const float speedMemoryDuration = 0.3f;

    private void Awake()
    {
        carController = GetComponentInParent<Car_Controller>();
    }

    private void FixedUpdate()
    {
        clearTimer -= Time.fixedDeltaTime;
        if (clearTimer <= 0f)
        {
            recentlyHit.Clear();
            clearTimer = 0.2f;
        }

        if (carController == null || carController.rb == null)
            return;

        float currentSpeed = carController.rb.velocity.magnitude;

        if (currentSpeed >= recentMaxSpeed)
        {
            recentMaxSpeed = currentSpeed;
            speedDecayTimer = speedMemoryDuration;
        }
        else
        {
            speedDecayTimer -= Time.fixedDeltaTime;
            if (speedDecayTimer <= 0f)
                recentMaxSpeed = currentSpeed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other.gameObject);
    }

    private void TryDamage(GameObject target)
    {
        if (carController == null || carController.rb == null)
            return;

        if (recentMaxSpeed < minSpeedToDamage)
            return;

        Enemy enemy = target.GetComponent<Enemy>();
        if (enemy == null)
            enemy = target.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            if (enemy.IsDead)
                return;

            int enemyId = enemy.gameObject.GetInstanceID();
            if (!recentlyHit.Add(enemyId))
                return;

            enemy.GetHit(carDamage);
            return;
        }

        IDamagable damagable = target.GetComponent<IDamagable>();
        if (damagable == null)
            damagable = target.GetComponentInParent<IDamagable>();

        if (damagable == null)
            return;

        int id = target.GetInstanceID();
        Component comp = damagable as Component;
        if (comp != null)
            id = comp.gameObject.GetInstanceID();

        if (!recentlyHit.Add(id))
            return;

        damagable.TakeDamage(carDamage);

        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null && comp != null)
            rb = comp.GetComponent<Rigidbody>();
        
        if(rb != null )
            ApplyForce(rb);
    }

    private void ApplyForce(Rigidbody rigidbody)
    {
        rigidbody.isKinematic = false;
        rigidbody.AddExplosionForce(impactForce, transform.position, 3, upwardsMultiplier, ForceMode.Impulse);
    }

}
