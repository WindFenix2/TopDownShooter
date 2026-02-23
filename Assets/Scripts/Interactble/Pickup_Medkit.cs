using UnityEngine;

public class Pickup_Medkit : Interactable
{
    [SerializeField] private int healAmount = 50;

    private Player_Health playerHealth;

    public override void Interaction()
    {
        if (playerHealth == null)
            return;

        if (playerHealth.currentHealth >= playerHealth.maxHealth)
            return;

        playerHealth.IncreaseHealth(healAmount);

        if (spawnedByDropDirector && DropDirector.instance != null)
        {
            DropDirector.instance.NotifyMedkitPickedUp();
            DropDirector.instance.UnregisterActiveDrop(gameObject, DropType.Medkit);
            spawnedByDropDirector = false;
        }

        WakeNearbyRagdolls();
        ObjectPool.instance.ReturnObject(gameObject);
    }

    protected override void OnTriggerEnter(Collider other)
    {
        if (playerHealth == null)
            playerHealth = other.GetComponent<Player_Health>();

        base.OnTriggerEnter(other);
    }
}