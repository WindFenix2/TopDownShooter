using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private Image healthBar;

    [Header("Weapons")]
    [SerializeField] private UI_WeaponSlot[] weaponSlots_UI;

    [Header("Missions")]
    [SerializeField] private GameObject missionTooltipParent;
    [SerializeField] private GameObject missionHelpTooltip;
    [SerializeField] private TextMeshProUGUI missionText;
    [SerializeField] private TextMeshProUGUI missionDetails;
    private bool tooltipActive = true;



    private void Awake()
    {
        weaponSlots_UI = GetComponentsInChildren<UI_WeaponSlot>();
    }

    public void SwitchMissionTooltip()
    {
        tooltipActive = !tooltipActive;
        missionTooltipParent.SetActive(tooltipActive);
        missionHelpTooltip.SetActive(!tooltipActive);
    }

    public void UpdateMissionInfo(string missionText, string missionDetails = "")
    {

        this.missionText.text = missionText;
        this.missionDetails.text = missionDetails;
    }

    public void UpdateWeaponUI(List<Weapon> weaponSlots, Weapon currentWeapon)
    {
        for (int i = 0; i < weaponSlots_UI.Length; i++)
        {
            if (i < weaponSlots.Count)
            {
                bool isActiveWeapon = weaponSlots[i] == currentWeapon ? true : false;
                weaponSlots_UI[i].UpdateWeaponSlot(weaponSlots[i], isActiveWeapon);
            }
            else
            {
                weaponSlots_UI[i].UpdateWeaponSlot(null, false);
            }
        }
    }
    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        healthBar.fillAmount = currentHealth / maxHealth;
    }
}
