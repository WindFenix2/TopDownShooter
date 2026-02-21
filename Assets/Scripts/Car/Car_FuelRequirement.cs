using UnityEngine;

public class Car_FuelRequirement : MonoBehaviour
{
    [SerializeField] private MissionItemType requiredItem = MissionItemType.Gasoline;
    [SerializeField] private bool requiresFuel = true;

    public bool refueled { get; private set; }

    public bool RequiresFuel() => requiresFuel;

    public MissionItemType RequiredItem() => requiredItem;

    public void SetRequiresFuel(bool value) => requiresFuel = value;

    public void SetRefueled(bool value) => refueled = value;
}