using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_AnimationEvents : MonoBehaviour
{
    private Player_WeaponVisuals visualController;
    private Player_WeaponController weaponController;

    private void Start()
    {
        visualController = GetComponentInParent<Player_WeaponVisuals>();
        weaponController = GetComponentInParent<Player_WeaponController>();
    }

    public void ReloadIsOver()
    {
        visualController.MaximizeRigWeight();
        visualController.CurrentWeaponModel().realodSfx.Stop();

        weaponController.CurrentWeapon().RefillBullets();


        weaponController.SetWeaponReady(true);
        weaponController.UpdateWeaponUI();
    }


    public void ReturnRig()
    {
        visualController.MaximizeRigWeight();
        visualController.MaximizeLeftHandWeight();
    }

    public void WeaponEquipingIsOver()
    {
        weaponController.SetWeaponReady(true);
    }

    public void SwitchOnWeaponModel() => visualController.SwitchOnCurrentWeaponModel();
}
