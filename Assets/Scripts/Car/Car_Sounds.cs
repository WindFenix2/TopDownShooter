using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car_Sounds : MonoBehaviour
{
    private Car_Controller car;

    [SerializeField] private float engineVolume = .07f;
    [SerializeField] private AudioSource engineStart;
    [SerializeField] private AudioSource workingEngine;
    [SerializeField] private AudioSource engineOff;

    private float minSpeed = 0;
    private float maxSpeed = 10;

    public float minPitch = .75f;
    public float maxPitch = 1.5f;

    private bool allowCarSounds;

    private void Start()
    {
        car = GetComponent<Car_Controller>();
        Invoke(nameof(AllowCarSounds), 1);
    }

    private void Update()
    {
        UpdateEngineSound();
    }

    private void UpdateEngineSound()
    {

        float currentSpeed = car.speed;
        float pitch = Mathf.Lerp(minPitch, maxPitch, currentSpeed / maxSpeed);
        workingEngine.pitch = pitch;
    }

    public void ActivateCarSFX(bool activate)
    {
        if (allowCarSounds == false)
            return;

        if (activate)
        {
            engineStart.Play();
            AudioManager.instance.SFXDelayAndFade(workingEngine, true, engineVolume, 1);
        }
        else
        {
            AudioManager.instance.SFXDelayAndFade(workingEngine,false, 0f, .25f);
            engineOff.Play();
        }
    }

    private void AllowCarSounds() => allowCarSounds = true;
}
