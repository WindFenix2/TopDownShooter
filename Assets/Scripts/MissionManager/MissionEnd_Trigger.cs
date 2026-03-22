using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionEnd_Trigger : MonoBehaviour
{
    private GameObject player;

    private void Start()
    {
        player = GameObject.Find("Player");
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for player on foot
        bool isPlayer = other.gameObject == player;

        // Check for player driving a car
        if (!isPlayer)
        {
            Car_Controller car = other.GetComponentInParent<Car_Controller>();
            if (car != null && car.carActive)
                isPlayer = true;
        }

        if (!isPlayer)
            return;

        // Special case: LastDefence mission starts when player reaches the defence point
        Mission_LastDefence defence = MissionManager.instance.currentMission as Mission_LastDefence;
        if (defence != null && !defence.defenceBegun)
        {
            defence.StartDefenceEvent();
            return;
        }

        if (MissionManager.instance.MissionCompleted())
        {
            GameManager.instance.GameCompleted();
            Debug.Log("Level completed!");
        }
        else
        {
            UI.instance?.inGameUI?.ShowCenterMessage("You can't escape yet! Complete the mission first.");
        }
    }
}
