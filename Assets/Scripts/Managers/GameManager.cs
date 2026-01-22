using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Player player;

    [Header("Settings")]
    public bool friendlyFire;
    [Space]
    public bool quickStart;

    [Header("QuickStart / Fallback weapons (for test scenes)")]
    [SerializeField] private List<Weapon_Data> quickStartDefaultWeapons = new List<Weapon_Data>();

    private void Awake()
    {
        instance = this;
        player = FindObjectOfType<Player>();
    }

    public void GameStart()
    {
        SetDefaultWeaponsForPlayer();
    }

    public void RestartScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void GameCompleted()
    {
        UI.instance.ShowVictoryScreenUI();
        ControlsManager.instance.controls.Character.Disable();
        player.health.currentHealth += 99999;
    }

    public void GameOver()
    {
        TimeManager.instance.SlowMotionFor(1.5f);
        UI.instance.ShowGameOverUI();
        CameraManager.instance.ChangeCameraDistance(5);
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
