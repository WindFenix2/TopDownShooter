using UnityEngine;

public class EMI_GrenadeInventory : MonoBehaviour
{
    [SerializeField] private int startAmount = 10;
    [SerializeField] private int maxAmount = 10;

    public int Current { get; private set; }

    private void Awake()
    {
        Current = Mathf.Clamp(startAmount, 0, maxAmount);
    }

    public bool TryConsume(int amount)
    {
        amount = Mathf.Max(1, amount);

        if (Current < amount)
            return false;

        Current -= amount;
        return true;
    }

    public void Add(int amount)
    {
        if (amount <= 0)
            return;

        Current = Mathf.Clamp(Current + amount, 0, maxAmount);
    }

    public void SetMax(int newMax)
    {
        maxAmount = Mathf.Max(1, newMax);
        Current = Mathf.Clamp(Current, 0, maxAmount);
    }
}
