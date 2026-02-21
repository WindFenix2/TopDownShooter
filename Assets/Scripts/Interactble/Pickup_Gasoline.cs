using System;
using UnityEngine;

public class Pickup_Gasoline : Interactable
{
    public static event Action OnGasolinePickedUp;

    [SerializeField] private int amount = 1;

    public override void Interaction()
    {
        if (MissionManager.instance != null)
            MissionManager.instance.AddItem(MissionItemType.Gasoline, Mathf.Max(1, amount));

        OnGasolinePickedUp?.Invoke();

        ObjectPool.instance.ReturnObject(gameObject);
    }
}