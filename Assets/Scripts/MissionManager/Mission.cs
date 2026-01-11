using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Mission : ScriptableObject
{
    public string missionName;

    [TextArea]
    public string missionDescription;

    public abstract void StartMission();
    public abstract bool MissionCompleted();

    public virtual void UpdateMission()
    {

    }

}
