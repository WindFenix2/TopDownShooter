using UnityEngine;
using System.Collections;

public class Player_EMIStatus : MonoBehaviour
{
    public bool CanShoot { get; private set; } = true;
    public bool CanUseAbilities { get; private set; } = true;
    public float SpeedMultiplier { get; private set; } = 1f;

    private Coroutine emiRoutine;

    public float GetSpeedMultiplier() => SpeedMultiplier;

    public void ApplyEMI(float speedMultiplier, float duration, bool disableShooting, bool disableAbilities)
    {
        SpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.05f, 1f);

        if (disableShooting)
            CanShoot = false;

        if (disableAbilities)
            CanUseAbilities = false;

        if (emiRoutine != null)
            StopCoroutine(emiRoutine);

        emiRoutine = StartCoroutine(RestoreRoutine(Mathf.Max(0.05f, duration)));
    }

    private IEnumerator RestoreRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);

        SpeedMultiplier = 1f;
        CanShoot = true;
        CanUseAbilities = true;

        emiRoutine = null;
    }
}
