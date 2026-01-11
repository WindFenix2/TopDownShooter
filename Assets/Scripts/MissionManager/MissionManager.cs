using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager instance;


    public Mission currentMission;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        Invoke(nameof(StartMission), 2);
    }

    private void Update()
    {
        currentMission?.UpdateMission();
    }

    private void StartMission() => currentMission.StartMission();

    public bool MissionCompleted() => currentMission.MissionCompleted();


}
