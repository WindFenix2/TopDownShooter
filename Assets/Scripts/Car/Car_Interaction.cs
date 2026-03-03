using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car_Interaction : Interactable
{
    private Car_HealthController carHealthController;
    private Car_Controller carController;
    private Car_FuelRequirement fuelRequirement;
    private Transform player;

    private float defaultPlayerScale;

    [Header("Exit details")]
    [SerializeField] private float exitCheckRadius = .2f;
    [SerializeField] private Transform[] exitPoints;
    [SerializeField] private LayerMask whatToIngoreForExit;

    private void Start()
    {
        carHealthController = GetComponent<Car_HealthController>();
        carController = GetComponent<Car_Controller>();
        fuelRequirement = GetComponent<Car_FuelRequirement>();
        player = GameManager.instance.player.transform;

        if (exitPoints == null || exitPoints.Length == 0)
        {
            List<Transform> found = new List<Transform>();
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                if (child.name.Contains("ExitPoint") && child != transform)
                    found.Add(child);
            }
            if (found.Count > 0)
                exitPoints = found.ToArray();
        }

        if (exitPoints != null)
        {
            foreach (var point in exitPoints)
            {
                if (point == null) continue;
                var mr = point.GetComponent<MeshRenderer>();
                var sc = point.GetComponent<SphereCollider>();
                if (mr != null) mr.enabled = false;
                if (sc != null) sc.enabled = false;
            }
        }
    }

    public override void Interaction()
    {
        base.Interaction();

        if (CanUseCar() == false)
            return;

        GetIntoTheCar();
    }

    private bool CanUseCar()
    {
        if (MissionManager.instance == null)
            return true;

        if (MissionManager.instance.currentMission is Mission_CarDelivery == false)
            return true;

        if (fuelRequirement == null)
            fuelRequirement = GetComponent<Car_FuelRequirement>();

        if (fuelRequirement == null)
        {
            fuelRequirement = gameObject.AddComponent<Car_FuelRequirement>();
            fuelRequirement.SetRequiresFuel(true);
            fuelRequirement.SetRefueled(false);
        }

        if (fuelRequirement.RequiresFuel() == false)
            return true;

        if (fuelRequirement.refueled)
            return true;

        if (MissionManager.instance.ConsumeItem(fuelRequirement.RequiredItem(), 1))
        {
            fuelRequirement.SetRefueled(true);
            UI.instance?.inGameUI?.ShowCenterMessage("Vehicle refueled.");
            return true;
        }

        UI.instance?.inGameUI?.ShowCenterMessage("Not enough fuel.");
        return false;
    }

    private void GetIntoTheCar()
    {
        ControlsManager.instance.SwitchToCarControls();
        carHealthController.UpdateCarHealthUI();
        carController.ActivateCar(true);

        defaultPlayerScale = player.localScale.x;

        player.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        player.transform.parent = transform;
        player.transform.localPosition = Vector3.up / 2;

        CameraManager.instance.ChangeCameraTarget(transform, 12, .5f);
    }

    public void GetOutOfTheCar()
    {
        if (carController.carActive == false)
            return;

        carController.ActivateCar(false);
        carController.rb.angularVelocity = Vector3.zero;

        player.parent = null;
        player.position = GetExitPoint();
        player.transform.localScale = new Vector3(defaultPlayerScale, defaultPlayerScale, defaultPlayerScale);

        ControlsManager.instance.SwitchToCharacterControls();

        Player_AimController aim = GameManager.instance.player.aim;

        float camDist = 8.5f;
        var p = GameManager.instance.player;

        if (p != null && p.weapon != null && p.weapon.CurrentWeapon() != null)
        {
            camDist = p.weapon.CurrentWeapon().cameraDistance;

            if (p.aim != null)
                p.aim.SyncCameraDistanceFromCurrentWeapon(false);
        }

        CameraManager.instance.ChangeCameraTarget(aim.GetAimCameraTarget(), camDist);
    }

    private Vector3 GetExitPoint()
    {
        if (exitPoints != null)
        {
            for (int i = 0; i < exitPoints.Length; i++)
            {
                if (exitPoints[i] != null && IsExitClear(exitPoints[i].position))
                    return exitPoints[i].position;
            }

            if (exitPoints.Length > 0 && exitPoints[0] != null)
                return exitPoints[0].position;
        }

        return transform.position + Vector3.up * 0.5f;
    }

    private bool IsExitClear(Vector3 point)
    {
        Collider[] colliders = Physics.OverlapSphere(point, exitCheckRadius, ~whatToIngoreForExit);
        return colliders.Length == 0;
    }

    private void OnDrawGizmos()
    {
        if (exitPoints != null && exitPoints.Length > 0)
        {
            foreach (var point in exitPoints)
            {
                if (point == null) continue;
                Gizmos.DrawWireSphere(point.position, exitCheckRadius);
            }
        }
    }
}