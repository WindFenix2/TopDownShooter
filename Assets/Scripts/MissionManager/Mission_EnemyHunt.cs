using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hunt - Mission", menuName = "Missions/Hunt - Mission")]

public class Mission_EnemyHunt : Mission
{
    [Tooltip("Total enemies to eliminate.")]
    public int amountToKill = 12;

    [Header("Enemy Tracker")]
    [Tooltip("Show arrow pointing to nearest target enemy.")]
    public bool showEnemyTracker = true;

    private int killsToGo;

    public override void StartMission()
    {
        killsToGo = amountToKill;
        UpdateMissionUI();

        // Unsubscribe first to prevent duplicate handlers from previous play sessions
        // (ScriptableObjects persist in the Editor, so static events accumulate)
        MissionObject_HuntTarget.OnTargetKilled -= EliminateTarget;
        MissionObject_HuntTarget.OnTargetKilled += EliminateTarget;

        List<Enemy> allEnemies = new List<Enemy>(LevelGenerator.instance.GetEnemyList());

        int toMark = Mathf.Min(amountToKill, allEnemies.Count);
        for (int i = 0; i < toMark; i++)
        {
            int randomIndex = Random.Range(0, allEnemies.Count);
            allEnemies[randomIndex].gameObject.AddComponent<MissionObject_HuntTarget>();
            allEnemies.RemoveAt(randomIndex);
        }

        if (showEnemyTracker && UI_EnemyTracker.instance != null)
            UI_EnemyTracker.instance.SetTracking(true);
    }

    public override bool MissionCompleted()
    {
        return killsToGo <= 0;
    }

    private void EliminateTarget()
    {
        killsToGo--;
        UpdateMissionUI();

        if (killsToGo <= 0)
        {
            UI.instance.inGameUI.UpdateMissionInfo("All targets eliminated.");
            MissionObject_HuntTarget.OnTargetKilled -= EliminateTarget;

            if (UI_EnemyTracker.instance != null)
                UI_EnemyTracker.instance.SetTracking(false);

            if (!hasExit)
            {
                GameManager.instance.GameCompleted();
            }
        }
    }

    private void UpdateMissionUI()
    {
        string missionText = "Eliminate targets.";
        string missionDetaiils = "Left: " + killsToGo;

        UI.instance.inGameUI.UpdateMissionInfo(missionText, missionDetaiils);
    }

}

