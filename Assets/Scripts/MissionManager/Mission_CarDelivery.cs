using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Car delivery - Mission", menuName = "Missions/Car delivery - Mission")]
public class Mission_CarDelivery : Mission
{
    private bool carWasDelivered;
    private bool gasolinePickedUp;

    [Header("Spawn Config")]
    [Tooltip("How many vehicles to spawn (1 recommended).")]
    public int vehiclesToSpawn = 1;

    [Tooltip("How many gasoline pickups to spawn from available points.")]
    public int gasolineToSpawn = 3;

    public override void StartMission()
    {
        FindObjectOfType<MissionObject_CarDeliveryZone>(true)?.gameObject.SetActive(true);

        carWasDelivered = false;
        gasolinePickedUp = false;

        MissionObject_CarToDeliver.OnCarDelivery += CarDeliveryCompleted;
        Pickup_Gasoline.OnGasolinePickedUp += GasolinePicked;
        Car_HealthController.OnCarDestroyed += OnCarDestroyed;

        if (MissionManager.instance != null)
            MissionManager.instance.ClearMissionItems();

        MissionSpawnPoint[] allPoints = FindObjectsOfType<MissionSpawnPoint>();

        MissionSpawnPoint[] vehiclePoints = allPoints
            .Where(p => p.category == MissionSpawnPoint.SpawnCategory.Vehicle).ToArray();
        MissionSpawnPoint[] gasolinePoints = allPoints
            .Where(p => p.category == MissionSpawnPoint.SpawnCategory.Gasoline).ToArray();

        if (vehiclePoints.Length > 0)
        {
            GameObject[] vehicles = MissionSpawnPoint.SpawnRandom(vehiclePoints, vehiclesToSpawn);
            foreach (GameObject vehicleGO in vehicles)
            {
                if (vehicleGO == null) continue;

                Car_Controller car = vehicleGO.GetComponent<Car_Controller>();
                if (car != null)
                {
                    car.gameObject.AddComponent<MissionObject_CarToDeliver>();

                    Car_FuelRequirement fuel = car.GetComponent<Car_FuelRequirement>();
                    if (fuel == null)
                        fuel = car.gameObject.AddComponent<Car_FuelRequirement>();

                    fuel.SetRequiresFuel(true);
                    fuel.SetRefueled(false);
                }
            }
        }
        else
        {
            Car_Controller[] cars = FindObjectsOfType<Car_Controller>();
            foreach (var car in cars)
            {
                car.gameObject.AddComponent<MissionObject_CarToDeliver>();

                Car_FuelRequirement fuel = car.GetComponent<Car_FuelRequirement>();
                if (fuel == null)
                    fuel = car.gameObject.AddComponent<Car_FuelRequirement>();

                fuel.SetRequiresFuel(true);
                fuel.SetRefueled(false);
            }
        }

        if (gasolinePoints.Length > 0)
        {
            MissionSpawnPoint.SpawnRandom(gasolinePoints, gasolineToSpawn);
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
        Car_HealthController.OnCarDestroyed -= OnCarDestroyed;

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

    private void OnCarDestroyed()
    {
        Car_HealthController.OnCarDestroyed -= OnCarDestroyed;
        MissionObject_CarToDeliver.OnCarDelivery -= CarDeliveryCompleted;
        Pickup_Gasoline.OnGasolinePickedUp -= GasolinePicked;

        UI.instance?.inGameUI?.ShowCenterMessage("Vehicle destroyed! Mission failed.");
        GameManager.instance.GameOver();
    }
}
