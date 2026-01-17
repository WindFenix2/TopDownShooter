using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class UI_Settings : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private float sliderMultiplier = 25;

    [Header("SFX Settings")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxSliderText;
    [SerializeField] private string sfxParametr;

    [Header("BGM Settings")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmSliderText;
    [SerializeField] private string bgmParametr;

    [Header("Toggle")]
    [SerializeField] private Toggle friendlyFireToggle;


    public void SFXSliderValue(float value)
    {
        sfxSliderText.text = Mathf.RoundToInt(value * 100) + "%";
        float newValue = Mathf.Log10(value) * sliderMultiplier;
        audioMixer.SetFloat(sfxParametr, newValue);

    }

    public void BGMSliderValue(float value)
    {
        bgmSliderText.text = Mathf.RoundToInt(value * 100) + "%";
        float newValue = Mathf.Log10(value) * sliderMultiplier;
        audioMixer.SetFloat(bgmParametr, newValue);
    }

    public void OnFriendlyFireToggle()
    {
        bool friendlyFire = GameManager.instance.friendlyFire;
        GameManager.instance.friendlyFire = !friendlyFire;
    }

    public void LoadSettings()
    {
        sfxSlider.value = PlayerPrefs.GetFloat(sfxParametr,.7f);
        bgmSlider.value = PlayerPrefs.GetFloat(bgmParametr,.7f);

        int friendlyFireInt = PlayerPrefs.GetInt("FriendlyFire", 0);
        bool newFriendlyFire = false;

        if (friendlyFireInt == 1)
            newFriendlyFire = true;
        
        friendlyFireToggle.isOn = newFriendlyFire;  
    }

    private void OnDisable()
    {
        bool friendlyFire = GameManager.instance.friendlyFire;
        int friendlyFireInt = friendlyFire ? 1 : 0;

        PlayerPrefs.SetInt("FriendlyFire", friendlyFireInt);
        PlayerPrefs.SetFloat(sfxParametr, sfxSlider.value);
        PlayerPrefs.SetFloat(bgmParametr, bgmSlider.value);
    }
}
