using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform playerBody;

    public PlayerControls controls { get; private set; }
    public Player_AimController aim { get; private set; }
    public Player_Movement movement { get; private set; }
    public Player_WeaponController weapon { get; private set; }
    public Player_WeaponVisuals weaponVisuals { get; private set; }
    public Player_Interaction interaction { get; private set; }
    public Player_Health health { get; private set; }
    public Ragdoll ragdoll { get; private set; }

    public Animator anim { get; private set; }
    public Player_SoundFX sound { get; private set; }

    public bool controlsEnabled { get; private set; }

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        ragdoll = GetComponent<Ragdoll>();
        health = GetComponent<Player_Health>();
        aim = GetComponent<Player_AimController>();
        movement = GetComponent<Player_Movement>();
        weapon = GetComponent<Player_WeaponController>();
        weaponVisuals = GetComponent<Player_WeaponVisuals>();
        interaction = GetComponent<Player_Interaction>();
        sound = GetComponent<Player_SoundFX>();
        controls = ControlsManager.instance.controls;
    }


    private void OnEnable()
    {
        controls.Enable();
        controls.Character.UIMissionToolTipSwitch.performed += ctx => UI.instance.inGameUI.SwitchMissionTooltip();
        controls.Character.UIPause.performed += ctx => UI.instance.PauseSwitch();
    }
    private void OnDisable()
    {
        controls.Disable();
    }

    public void SetControlsEnabledTo(bool enabled)
    {
        controlsEnabled = enabled;
        ragdoll.CollidersActive(enabled);
        aim.EnableAimLaer(enabled);
    }
}
