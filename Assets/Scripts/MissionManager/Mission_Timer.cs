using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New Timer Mission", menuName = "Missions/Timer - Mission")]

public class Mission_Timer : Mission
{
    public float time;
    private float currentTime;
    private bool timedOut;

    public override void StartMission()
    {
        currentTime = time;
        timedOut = false;
        UI.instance.inGameUI.UpdateMissionInfo("Reach the evacuation point in time.");
    }

    public override void UpdateMission()
    {
        if (timedOut) return;

        currentTime -= Time.deltaTime;

        if (currentTime < 0)
        {
            timedOut = true;
            UI.instance.inGameUI.UpdateMissionInfo("Time's up! Mission failed.");
            GameManager.instance.GameOver();
            return;
        }

        string timeText = System.TimeSpan.FromSeconds(currentTime).ToString("mm':'ss");
        string missionText = "Reach the evacuation point in time.";
        string missionDetails = "Time left: " + timeText;

        UI.instance.inGameUI.UpdateMissionInfo(missionText, missionDetails);
    }

    public override bool MissionCompleted()
    {
        return currentTime > 0;
    }
}

