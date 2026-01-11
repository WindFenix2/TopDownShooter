using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Car delivery - Mission", menuName = "Missions/Car delivery - Mission")]

public class Mission_CarDelivery : Mission
{
    private bool carWasDelivered;
    public override void StartMission()
    {
        FindObjectOfType<MissionObject_CarDeliveryZone>(true).gameObject.SetActive(true);

        carWasDelivered = false;
        MissionObject_CarToDeliver.OnCarDelivery += CarDeliveryCompleted;

        Car[] cars = FindObjectsOfType<Car>();

        foreach (Car car in cars)
        {
            car.AddComponent<MissionObject_CarToDeliver>();
        }

    }

    public override bool MissionCompleted()
    {
        return carWasDelivered;
    }

    private void CarDeliveryCompleted()
    {
        carWasDelivered = true;
        MissionObject_CarToDeliver.OnCarDelivery -= CarDeliveryCompleted;
    }
    
}
