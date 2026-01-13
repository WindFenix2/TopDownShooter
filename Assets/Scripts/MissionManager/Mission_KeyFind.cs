using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Find Key - Mission", menuName = "Missions/Key - Mission")]
public class Mission_KeyFind : Mission
{

    [SerializeField] private GameObject key;
    private bool keyFound;

    public override void StartMission()
    {
        MissionObject_Key.OnKeyPickedUp += PickUpKey;

        UI.instance.inGameUI.UpdateMissionInfo("Find a key-holder. Retrive the key.");
        
        Enemy enemy = LevelGenerator.instance.GetRandomEnemy();
        enemy.GetComponent<Enemy_DropController>()?.GiveKey(key);
        enemy.MakeEnemyVIP();
    }

    public override bool MissionCompleted()
    {
        return keyFound;
    }

    private void PickUpKey()
    {
        keyFound = true;
        MissionObject_Key.OnKeyPickedUp -= PickUpKey;

        UI.instance.inGameUI.UpdateMissionInfo("You've got the key! \n Get to the evacuation point.");
    }
}
