using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Player player;

    [Header("Settings")]
    public bool friendlyFire;

    [Header("DEV: Shotgun shield behavior")]
    [Tooltip("ON = shield stays even if you switch/throw shotgun. OFF = shield works only while shotgun is equipped.")]
    public bool shotgunShieldPersists = true;

    [Header("Dev/Debug")]
    public bool quickStart;

    [Header("QuickStart / Fallback weapons (for test scenes)")]
    [SerializeField] private List<Weapon_Data> quickStartDefaultWeapons = new List<Weapon_Data>();

    private void Awake()
    {
        instance = this;
        player = FindObjectOfType<Player>();

        int friendlyFireInt = PlayerPrefs.GetInt("FriendlyFire", friendlyFire ? 1 : 0);
        friendlyFire = friendlyFireInt == 1;
    }

    public void GameStart()
    {
        SetDefaultWeaponsForPlayer();

        if (player != null)
        {
            Shotgun_KillShieldAbility ability = player.GetComponent<Shotgun_KillShieldAbility>();
            if (ability != null)
                ability.SetPersistShield(shotgunShieldPersists);
        }

        UI.instance?.inGameUI?.ShowControlsHint();
    }

    public void RestartScene()
    {
        TimeManager.instance.UnfreezeTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GameCompleted()
    {
        int comicIndex = -1;
        Mission mission = MissionManager.instance != null ? MissionManager.instance.currentMission : null;
        if (mission != null)
            comicIndex = mission.victoryComicIndex;

        UI.instance.ShowVictoryScreenUI(comicIndex);
        ControlsManager.instance.controls.Character.Disable();
        player.health.currentHealth += 99999;
        TimeManager.instance.FreezeTime();
    }

    public void GameOver()
    {
        ControlsManager.instance.controls.Character.Disable();
        TimeManager.instance.SlowMotionFor(1.5f);
        UI.instance.ShowGameOverUI();
        CameraManager.instance.ChangeCameraDistance(5);
        StartCoroutine(FreezeAfterDelay(1.6f));
    }

    private System.Collections.IEnumerator FreezeAfterDelay(float realSeconds)
    {
        yield return new WaitForSecondsRealtime(realSeconds);
        TimeManager.instance.FreezeTime();
    }

    private void SetDefaultWeaponsForPlayer()
    {
        List<Weapon_Data> selectedFromUI = null;

        if (UI.instance != null && UI.instance.weaponSelection != null)
            selectedFromUI = UI.instance.weaponSelection.SelectedWeaponData();

        List<Weapon_Data> finalList = new List<Weapon_Data>();

        if (selectedFromUI != null)
        {
            for (int i = 0; i < selectedFromUI.Count; i++)
            {
                if (selectedFromUI[i] != null)
                    finalList.Add(selectedFromUI[i]);
            }
        }

        if (finalList.Count == 0)
        {
            for (int i = 0; i < quickStartDefaultWeapons.Count; i++)
            {
                if (quickStartDefaultWeapons[i] != null)
                    finalList.Add(quickStartDefaultWeapons[i]);
            }
        }

        if (finalList.Count == 0)
        {
            Debug.LogWarning("No weapon data selected and no fallback weapons in GameManager. Player will start without weapons.");
            return;
        }

        if (finalList.Count < 2)
        {
            Weapon_Data extra = null;

            for (int i = 0; i < quickStartDefaultWeapons.Count; i++)
            {
                var w = quickStartDefaultWeapons[i];
                if (w != null && w != finalList[0])
                {
                    extra = w;
                    break;
                }
            }

            if (extra == null)
                extra = finalList[0];

            finalList.Add(extra);
        }

        player.weapon.SetDefaultWeapon(finalList);
    }
}
