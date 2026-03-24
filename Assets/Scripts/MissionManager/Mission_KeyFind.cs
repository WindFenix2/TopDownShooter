using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Find Key - Mission", menuName = "Missions/Key - Mission")]
public class Mission_KeyFind : Mission
{

    [SerializeField] private GameObject key;
    private bool keyFound;

    [Header("Tracker Settings")]
    [Tooltip("Show arrow pointing to key holder within this distance.")]
    public float trackerActivationDistance = 100f;

    private Enemy keyEnemy;

    public override void StartMission()
    {
        keyFound = false;

        MissionObject_Key.OnKeyPickedUp -= PickUpKey;
        MissionObject_Key.OnKeyPickedUp += PickUpKey;

        UI.instance.inGameUI.UpdateMissionInfo("Find a key-holder. Retrive the key.");
        
        Enemy enemy = LevelGenerator.instance.GetRandomEnemy();
        enemy.GetComponent<Enemy_DropController>()?.GiveKey(key);
        enemy.MakeEnemyVIP();

        keyEnemy = enemy;

        if (enemy.GetComponent<MissionObject_HuntTarget>() == null)
            enemy.gameObject.AddComponent<MissionObject_HuntTarget>();
    }

    private bool lastTrackingState;

    public override void UpdateMission()
    {
        if (keyFound || keyEnemy == null || keyEnemy.IsDead)
        {
            if (lastTrackingState)
            {
                lastTrackingState = false;
                if (UI_EnemyTracker.instance != null)
                    UI_EnemyTracker.instance.SetTracking(false);
            }
            return;
        }

        Player player = GameManager.instance?.player;
        if (player == null) return;

        float dist = Vector3.Distance(player.transform.position, keyEnemy.transform.position);
        bool shouldTrack = dist <= trackerActivationDistance;

        if (shouldTrack != lastTrackingState)
        {
            lastTrackingState = shouldTrack;
            if (UI_EnemyTracker.instance != null)
                UI_EnemyTracker.instance.SetTracking(shouldTrack);
        }
    }

    public override bool MissionCompleted()
    {
        return keyFound;
    }

    private void PickUpKey()
    {
        keyFound = true;
        MissionObject_Key.OnKeyPickedUp -= PickUpKey;

        if (UI_EnemyTracker.instance != null)
            UI_EnemyTracker.instance.SetTracking(false);

        UI.instance.inGameUI.UpdateMissionInfo("You've got the key! \n Get to the evacuation point.");
    }
    public override void CleanupMission()
    {
        MissionObject_Key.OnKeyPickedUp -= PickUpKey;
        keyFound = false;
        keyEnemy = null;
        lastTrackingState = false;

        if (UI_EnemyTracker.instance != null)
            UI_EnemyTracker.instance.SetTracking(false);
    }
}
