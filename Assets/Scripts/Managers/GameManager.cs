using System.Collections;
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

    private void Awake()
    {
        instance = this;

        player = FindObjectOfType<Player>();
    }

  
    public void GameStart()
    {
        SetDefaultWeaponsForPlayer();

        //LevelGenerator.instance.InitializeGeneration();
        // We start selected mission in a LevelGenerator script ,after we done with level creation.
    }

    public void RestartScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void GameCompleted()
    {
        UI.instance.ShowVictoryScreenUI();
        ControlsManager.instance.controls.Character.Disable();
        player.health.currentHealth += 99999; // So player won't die in last second.
    }
    public void GameOver()
    {
        TimeManager.instance.SlowMotionFor(1.5f);
        UI.instance.ShowGameOverUI();
        CameraManager.instance.ChangeCameraDistance(5);
    }

    private void SetDefaultWeaponsForPlayer()
    {
        List<Weapon_Data> newList = UI.instance.weaponSelection.SelectedWeaponData();
        player.weapon.SetDefaultWeapon(newList);
    }
}
