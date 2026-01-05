using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_BossVisuals : MonoBehaviour
{
    private Enemy_Boss enemy;

    [SerializeField] private float landingOffset = 1;
    [SerializeField] private ParticleSystem landindZoneFx;
    [SerializeField] private GameObject[] weaponTrails;

    [Header("Batteries")]
    [SerializeField] private GameObject[] batteries;
    [SerializeField] private float initalBatterySclaeY = .2f;

    private float dischargeSpeed;
    private float rechargeSpeed;

    private bool isRecharging;

    private void Awake()
    {
        enemy = GetComponent<Enemy_Boss>();

        landindZoneFx.transform.parent = null;
        landindZoneFx.Stop();

        ResetBatteries();
    }

    private void Update()
    {
        UpdateBatteriesScale();
    }

    public void EnableWeaponTrail(bool active)
    {
        if (weaponTrails.Length <= 0)
        {
            Debug.LogWarning("No weapon trails assigned");
            return;
        }

        foreach (var trail in weaponTrails)
        {
            trail.gameObject.SetActive(active);
        }
    }

    public void PlaceLandindZone(Vector3 target)
    {

        Vector3 dir = target - transform.position;
        Vector3 offset = dir.normalized * landingOffset;
        landindZoneFx.transform.position = target + offset;
        landindZoneFx.Clear();

        var mainModule = landindZoneFx.main;
        mainModule.startLifetime = enemy.travelTimeToTarget * 2;

        landindZoneFx.Play();
    }

    private void UpdateBatteriesScale()
    {
        if (batteries.Length <= 0)
            return;

        foreach (GameObject battery in batteries)
        {
            if (battery.activeSelf)
            {
                float scaleChange = (isRecharging ? rechargeSpeed : -dischargeSpeed) * Time.deltaTime;
                float newScaleY =
                    Mathf.Clamp(battery.transform.localScale.y + scaleChange, 0, initalBatterySclaeY);

                battery.transform.localScale = new Vector3(0.15f, newScaleY, 0.15f);

                if(battery.transform.localScale.y <= 0)
                    battery.SetActive(false);

            }
        }
    }


    public void ResetBatteries()
    {
        isRecharging = true;

        rechargeSpeed = initalBatterySclaeY / enemy.abilityCooldown;
        dischargeSpeed = initalBatterySclaeY / (enemy.flamethrowDuration * .75f);

        foreach (GameObject battery in batteries)
        {
            battery.SetActive(true);
        }
    }

    public void DischargeBatteries() => isRecharging = false;
}
