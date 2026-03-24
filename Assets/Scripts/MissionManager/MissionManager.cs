using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager instance;

    public Mission currentMission;

    private Dictionary<MissionItemType, int> missionItems = new Dictionary<MissionItemType, int>();
    private Coroutine uiSyncRoutine;
    private bool missionActive;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        TrySyncMissionUI();
    }

    private void OnDestroy()
    {
        CleanupCurrentMission();
    }

    public void ClearMissionItems() => missionItems.Clear();

    public void AddItem(MissionItemType itemType, int amount = 1)
    {
        if (amount <= 0) return;

        if (missionItems.ContainsKey(itemType) == false)
            missionItems[itemType] = 0;

        missionItems[itemType] += amount;
    }

    public bool HasItem(MissionItemType itemType, int amount = 1)
    {
        if (amount <= 0) return true;

        if (missionItems.TryGetValue(itemType, out int value) == false)
            return false;

        return value >= amount;
    }

    public bool ConsumeItem(MissionItemType itemType, int amount = 1)
    {
        if (HasItem(itemType, amount) == false)
            return false;

        missionItems[itemType] -= amount;

        if (missionItems[itemType] <= 0)
            missionItems.Remove(itemType);

        return true;
    }

    private bool exitMarkerPlaced;

    private void Update()
    {
        if (missionActive)
            currentMission?.UpdateMission();

        if (!exitMarkerPlaced && currentMission != null && currentMission.showExitMarker && MissionCompleted())
        {
            exitMarkerPlaced = true;

            MissionEnd_Trigger exitTrigger = FindObjectOfType<MissionEnd_Trigger>();
            if (exitTrigger != null)
            {
                if (exitTrigger.GetComponent<MissionObject_HuntTarget>() == null)
                    exitTrigger.gameObject.AddComponent<MissionObject_HuntTarget>();

                if (UI_EnemyTracker.instance != null)
                    UI_EnemyTracker.instance.SetTracking(true);
            }
        }
    }

    public void SetCurrentMission(Mission newMission)
    {
        CleanupCurrentMission();

        currentMission = newMission;
        exitMarkerPlaced = false;
        ClearMissionItems();
        TrySyncMissionUI();
    }

    public void StartMission()
    {
        if (currentMission == null)
            return;

        TrySyncMissionUI();
        currentMission.StartMission();
        missionActive = true;
    }

    public void CleanupCurrentMission()
    {
        missionActive = false;
        currentMission?.CleanupMission();

        MissionObject_HuntTarget.ClearEvent();
        MissionObject_Key.ClearEvent();
        MissionObject_CarToDeliver.ClearEvent();
        Car_HealthController.ClearEvent();
        Pickup_Gasoline.ClearEvent();
    }

    public bool MissionCompleted()
    {
        return currentMission != null && currentMission.MissionCompleted();
    }

    private void TrySyncMissionUI()
    {
        if (uiSyncRoutine != null)
            StopCoroutine(uiSyncRoutine);

        uiSyncRoutine = StartCoroutine(SyncMissionUIRoutine());
    }

    private IEnumerator SyncMissionUIRoutine()
    {
        int frames = 0;
        while ((UI.instance == null || UI.instance.inGameUI == null) && frames < 300)
        {
            frames++;
            yield return null;
        }

        uiSyncRoutine = null;
    }
}