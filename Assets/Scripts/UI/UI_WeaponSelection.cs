using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public class UI_WeaponSelection : MonoBehaviour
{
    [SerializeField] private GameObject nextUIToSwitchOn;
    public UI_SelectedWeaponWindow[] selectedWeapon;

    [Header("Warning Info")]
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private float disaperaingSpeed = .25f;
    private float currentWarningAlpha;
    private float targetWarningAlpha;


    private void Start()
    {
        selectedWeapon = GetComponentsInChildren<UI_SelectedWeaponWindow>();
    }

    private void Update()
    {
        if (currentWarningAlpha > targetWarningAlpha)
        {
            currentWarningAlpha -= Time.deltaTime * disaperaingSpeed;
            warningText.color = new Color(1, 1, 1, currentWarningAlpha);
        }
    }

    public void ConfirmWeaponSelection()
    {
        if (AtLeastOneWeaponSelected())
        {
            UI.instance.SwitchTo(nextUIToSwitchOn);
            UI.instance.StartLevelGeneration();
        }
        else
            ShowWarningMessage("Select at least one weapon.");
    }

    private bool AtLeastOneWeaponSelected() => SelectedWeaponData().Count > 0;

    public List<Weapon_Data> SelectedWeaponData()
    {
        List<Weapon_Data> selectedData = new List<Weapon_Data> ();

        foreach(UI_SelectedWeaponWindow weapon in selectedWeapon)
        {
            if(weapon.weaponData != null)
                selectedData.Add(weapon.weaponData);
        }

        return selectedData;
    }

    public UI_SelectedWeaponWindow FindEmptySlot()
    {
        for (int i = 0; i < selectedWeapon.Length; i++)
        {
            if (selectedWeapon[i].IsEmpty())
                return selectedWeapon[i];
        }

        return null;
    }
    public UI_SelectedWeaponWindow FindSlowWithWeaponOfType(Weapon_Data weaponData)
    {
        for(int i = 0;i < selectedWeapon.Length;i++)
        {
            if (selectedWeapon[i].weaponData == weaponData)
                return selectedWeapon[i];
        }

        return null;
    }

    public void ShowWarningMessage(string message)
    {
        warningText.color = Color.white;
        warningText.text = message;

        currentWarningAlpha = warningText.color.a;
        targetWarningAlpha = 0;
    }
}
