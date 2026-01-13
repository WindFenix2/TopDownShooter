using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Settings : MonoBehaviour
{
    [Header("SFX Settings")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TextMeshProUGUI sfxSliderText;

    [Header("BGM Settings")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private TextMeshProUGUI bgmSliderText;


    public void SFXSliderValue(float value)
    {
        sfxSliderText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    public void BGMSliderValue(float value)
    {
        bgmSliderText.text = Mathf.RoundToInt(value * 100) + "%";
    }

    public void OnFriendlyFireToggle()
    {
        bool friendlyFire = GameManager.instance.friendlyFire;
        GameManager.instance.friendlyFire = !friendlyFire;
    }
}
