using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Mission : ScriptableObject
{
    public string missionName;

    [TextArea]
    public string missionDescription;

    [Header("Level Generation Config")]
    [Tooltip("Level parts available for this mission. If empty, uses LevelGenerator defaults.")]
    public List<Transform> availableLevelParts;

    [Tooltip("Override for the last level part (e.g. plane exit, empty exit). If null, uses LevelGenerator default.")]
    public Transform lastLevelPartOverride;

    [Tooltip("If false, no exit is generated (e.g. EnemyHunt, LastDefence). Player wins by completing the objective.")]
    public bool hasExit = true;

    [Tooltip("Max number of level parts to generate (0 = use LevelGenerator default). Use small values for arena missions.")]
    public int maxLevelParts = 0;

    [Tooltip("Override for the penultimate (second-to-last) level part. If set, this prefab is inserted before the exit.")]
    public Transform penultimateLevelPartOverride;

    [Tooltip("If true, the general car spawn system is disabled for this mission (mission handles its own car spawning).")]
    public bool disableCarSpawns = false;

    [Tooltip("If true, an exit marker arrow appears when the mission is completed. Disable for timer/escape missions.")]
    public bool showExitMarker = true;

    public abstract void StartMission();
    public abstract bool MissionCompleted();

    public virtual void UpdateMission()
    {

    }
}
