using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Car delivery - Mission", menuName = "Missions/Car delivery - Mission")]
public class Mission_CarDelivery : Mission
{
    private bool carWasDelivered;
    private bool gasolinePickedUp;

    public override void StartMission()
    {
        FindObjectOfType<MissionObject_CarDeliveryZone>(true).gameObject.SetActive(true);

        carWasDelivered = false;
        gasolinePickedUp = false;

        MissionObject_CarToDeliver.OnCarDelivery += CarDeliveryCompleted;
        Pickup_Gasoline.OnGasolinePickedUp += GasolinePicked;

        if (MissionManager.instance != null)
            MissionManager.instance.ClearMissionItems();

        Car_Controller[] cars = FindObjectsOfType<Car_Controller>();

        foreach (var car in cars)
        {
            car.AddComponent<MissionObject_CarToDeliver>();

            Car_FuelRequirement fuel = car.GetComponent<Car_FuelRequirement>();
            if (fuel == null)
                fuel = car.gameObject.AddComponent<Car_FuelRequirement>();

            fuel.SetRequiresFuel(true);
            fuel.SetRefueled(false);
        }

        UI.instance?.inGameUI?.ShowCenterMessage("Find gasoline and refuel the vehicle.");
        UI.instance?.inGameUI?.UpdateMissionInfo("Find gasoline and refuel a vehicle.");
    }

    public override bool MissionCompleted()
    {
        return carWasDelivered;
    }

    private void CarDeliveryCompleted()
    {
        carWasDelivered = true;

        MissionObject_CarToDeliver.OnCarDelivery -= CarDeliveryCompleted;
        Pickup_Gasoline.OnGasolinePickedUp -= GasolinePicked;

        UI.instance?.inGameUI?.ShowCenterMessage("Vehicle delivered!");
        UI.instance?.inGameUI?.UpdateMissionInfo("Vehicle delivered!");
    }

    private void GasolinePicked()
    {
        if (gasolinePickedUp)
            return;

        gasolinePickedUp = true;
        UI.instance?.inGameUI?.ShowCenterMessage("Gasoline collected.");
        UI.instance?.inGameUI?.UpdateMissionInfo("Deliver the vehicle to the drop zone.");
    }
}