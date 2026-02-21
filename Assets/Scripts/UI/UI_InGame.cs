using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InGame : MonoBehaviour
{
    [SerializeField] private GameObject charcaterUI;
    [SerializeField] private GameObject carUI;

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

    [Header("Car info")]
    [SerializeField] private Image carHealthBar;
    [SerializeField] private TextMeshProUGUI carSpeedText;

    private GameObject centerMessageParent;
    private CanvasGroup centerMessageCanvasGroup;
    private TextMeshProUGUI centerMessageText;
    private Coroutine centerMessageRoutine;

    [Header("Center message timings")]
    [SerializeField] private float defaultCenterMessageDuration = 1.4f;
    [SerializeField] private float defaultCenterMessageFadeTime = 0.5f;

    private void Awake()
    {
        weaponSlots_UI = GetComponentsInChildren<UI_WeaponSlot>();
        EnsureCenterMessageUI();
    }

    private void EnsureCenterMessageUI()
    {
        if (centerMessageParent != null && centerMessageCanvasGroup != null && centerMessageText != null)
            return;

        centerMessageParent = new GameObject("CenterMessage_UI");
        centerMessageParent.transform.SetParent(transform, false);

        var rt = centerMessageParent.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(900f, 160f);

        centerMessageCanvasGroup = centerMessageParent.AddComponent<CanvasGroup>();
        centerMessageCanvasGroup.alpha = 0f;
        centerMessageParent.SetActive(false);

        var textGO = new GameObject("CenterMessage_Text");
        textGO.transform.SetParent(centerMessageParent.transform, false);

        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0f, 0f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 0.5f);
        trt.offsetMin = new Vector2(0f, 0f);
        trt.offsetMax = new Vector2(0f, 0f);

        centerMessageText = textGO.AddComponent<TextMeshProUGUI>();
        centerMessageText.text = "";
        centerMessageText.alignment = TextAlignmentOptions.Center;
        centerMessageText.enableWordWrapping = true;
        centerMessageText.overflowMode = TextOverflowModes.Overflow;
        centerMessageText.fontSize = 34;

        if (missionText != null && missionText.font != null)
            centerMessageText.font = missionText.font;
        if (missionText != null && missionText.fontMaterial != null)
            centerMessageText.fontMaterial = missionText.fontMaterial;
    }

    public void SwitchToCharcaterUI()
    {
        charcaterUI.SetActive(true);
        carUI.SetActive(false);
    }

    public void SwitchToCarUI()
    {
        charcaterUI.SetActive(false);
        carUI.SetActive(true);
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

    public void UpdateMissionFromData(Mission mission)
    {
        if (mission == null)
            return;

        if (missionText != null) missionText.text = mission.missionName;
        if (missionDetails != null) missionDetails.text = mission.missionDescription;
    }

    public void ShowCenterMessage(string text, float duration = -1f, float fadeTime = -1f)
    {
        EnsureCenterMessageUI();

        if (duration <= 0f) duration = defaultCenterMessageDuration;
        if (fadeTime <= 0f) fadeTime = defaultCenterMessageFadeTime;

        if (centerMessageRoutine != null)
            StopCoroutine(centerMessageRoutine);

        centerMessageRoutine = StartCoroutine(CenterMessageRoutine(text, duration, fadeTime));
    }

    private IEnumerator CenterMessageRoutine(string text, float duration, float fadeTime)
    {
        centerMessageParent.SetActive(true);
        centerMessageText.text = text;

        centerMessageCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(duration);

        float t = 0f;
        float startA = centerMessageCanvasGroup.alpha;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            centerMessageCanvasGroup.alpha = Mathf.Lerp(startA, 0f, k);
            yield return null;
        }

        centerMessageCanvasGroup.alpha = 0f;
        centerMessageParent.SetActive(false);
        centerMessageRoutine = null;
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

    public void UpdateCarHealthUI(float currentCarHealth, float maxCarHealth)
    {
        carHealthBar.fillAmount = currentCarHealth / maxCarHealth;
    }

    public void UpdateSpeedText(string text) => carSpeedText.text = text;
}