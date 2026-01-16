using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControlsManager : MonoBehaviour
{
    public static ControlsManager instance;
    public PlayerControls controls { get; private set; }
    private Player player;

    private void Awake()
    {
        instance = this;
        controls = new PlayerControls();
    }


    private void Start()
    {
        player = GameManager.instance.player;

        SwitchToCharacterControls();
    }

    public void SwitchToCharacterControls()
    {
        controls.Character.Enable();

        controls.Car.Disable();
        controls.UI.Disable();

        player.SetControlsEnabledTo(true);
        UI.instance.inGameUI.SwitchToCharcaterUI();
    }

    public void SwitchToUIControls()
    {
        controls.UI.Enable();

        controls.Car.Disable();
        controls.Character.Disable();
        player.SetControlsEnabledTo(false);
    }

    public void SwitchToCarControls()
    {
        controls.Car.Enable();

        controls.UI.Disable();
        controls.Character.Disable();

        player.SetControlsEnabledTo(false);
        UI.instance.inGameUI.SwitchToCarUI();
    }


}
