using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car_HealthController : MonoBehaviour, IDamagable
{
    private Car_Controller carController;

    public int maxHealth;
    public int currentHealth;

    public bool carBroken { get; private set; }

    public static System.Action OnCarDestroyed;
    public static void ClearEvent() => OnCarDestroyed = null;

    [Header("Explosion Info")]
    [SerializeField] private bool canExplode = true;
    [SerializeField] private int explosionDamage = 350;
    [Space]
    [SerializeField] private float explosionRadius = 3;
    [SerializeField] private float explosionDelay = 3;
    [SerializeField] private float explosionForce = 7;
    [SerializeField] private float explosionUpwardsModifier = 2;
    [SerializeField] private Transform explosionPoint;
    [Space]
    [SerializeField] private ParticleSystem fireFx;
    [SerializeField] private ParticleSystem explosionFx;

    private void Start()
    {
        carController = GetComponent<Car_Controller>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (fireFx.gameObject.activeSelf)
            fireFx.transform.rotation = Quaternion.identity;
    }

    public void UpdateCarHealthUI()
    {
        UI.instance.inGameUI.UpdateCarHealthUI(currentHealth, maxHealth);
    }

    private void ReduceHealth(int damage)
    {
        if (carBroken)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
            BrakeTheCar();
    }

    private void BrakeTheCar()
    {
        carBroken = true;
        carController.BrakeTheCar();

        OnCarDestroyed?.Invoke();

        Car_Interaction interaction = GetComponent<Car_Interaction>();
        if (interaction != null)
        {
            interaction.HighlightActive(false);
            Player_Interaction pi = GameManager.instance?.player?.GetComponent<Player_Interaction>();
            if (pi != null)
            {
                pi.GetInteracbles().Remove(interaction);
                pi.UpdateClosestInteractble();
            }
        }

        fireFx.gameObject.SetActive(true);

        if (canExplode)
            StartCoroutine(ExplosionCo(explosionDelay));
    }

    public void TakeDamage(int damage)
    {
        ReduceHealth(damage);
        UpdateCarHealthUI();
    }

    private IEnumerator ExplosionCo(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (explosionFx != null)
            explosionFx.gameObject.SetActive(true);

        carController.rb.
            AddExplosionForce(explosionForce, explosionPoint.position,
            explosionRadius, explosionUpwardsModifier, ForceMode.Impulse);

        Explode();
    }

    private void Explode()
    {
        HashSet<GameObject> uniqEntites = new HashSet<GameObject>();

        Collider[] colliders = Physics.OverlapSphere(explosionPoint.position, explosionRadius);

        foreach (Collider hit in colliders)
        {
            IDamagable damagable = hit.GetComponent<IDamagable>();

            if (damagable != null)
            {
                GameObject rootEntity = hit.transform.root.gameObject;

                if (uniqEntites.Add(rootEntity) == false)
                    continue;

                damagable.TakeDamage(explosionDamage);

                Rigidbody rb = hit.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, explosionPoint.position, explosionRadius, explosionUpwardsModifier, ForceMode.VelocityChange);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(explosionPoint.position, explosionRadius);
    }
}
