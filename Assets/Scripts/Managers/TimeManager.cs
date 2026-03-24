using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;

    [SerializeField] private float resumeRate = 3;
    [SerializeField] private float pauseRate = 7;

    private float timeAdjustRate;
    private float targetTimeScale = 1;

    [SerializeField] private float slowMotionTimeScale = .5f;
    private bool isSlowMotionHeld;
    private bool frozen;

    private void Awake()
    {
        instance = this;
    }


    private void Update()
    {
        if (frozen)
            return;

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (!isSlowMotionHeld)
            {
                isSlowMotionHeld = true;
                targetTimeScale = slowMotionTimeScale;
                Time.timeScale = slowMotionTimeScale;
            }
        }
        else if (isSlowMotionHeld)
        {
            isSlowMotionHeld = false;
            ResumeTime();
        }

        if (Mathf.Abs(Time.timeScale - targetTimeScale) > .05f)
        {
            float adjustRate = Time.unscaledDeltaTime * timeAdjustRate;
            Time.timeScale = Mathf.Lerp(Time.timeScale, targetTimeScale, adjustRate);
        }
        else
            Time.timeScale = targetTimeScale;
    }

    public void PauseTime()
    {
        timeAdjustRate = pauseRate;
        targetTimeScale = 0;
        if (AudioManager.instance != null)
            AudioManager.instance.SetSFXPause(true);
    }

    public void ResumeTime()
    {
        if (frozen)
            return;

        timeAdjustRate = resumeRate;
        targetTimeScale = 1;
        if (AudioManager.instance != null)
            AudioManager.instance.SetSFXPause(false);
    }

    public void FreezeTime()
    {
        frozen = true;
        targetTimeScale = 0;
        Time.timeScale = 0;
        if (AudioManager.instance != null)
            AudioManager.instance.SetSFXPause(true);
    }

    public void UnfreezeTime()
    {
        frozen = false;
        targetTimeScale = 1;
        Time.timeScale = 1;
        if (AudioManager.instance != null)
            AudioManager.instance.SetSFXPause(false);
    }

    public void SlowMotionFor(float seconds) => StartCoroutine(SlowTimeCo(seconds));

    private IEnumerator SlowTimeCo(float seconds)
    {
        targetTimeScale = slowMotionTimeScale;
        Time.timeScale = targetTimeScale;
        yield return new WaitForSecondsRealtime(seconds);

        if (!frozen)
            ResumeTime();
    }
}
