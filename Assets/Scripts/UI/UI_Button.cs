using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Button : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("Mouse hover settings")]
    public float scaleSpeed = 3;
    public float scaleRate = 1.2f;


    private Vector3 defaultScale;
    private Vector3 targetScale;

    private Image buttonImage;
    private TextMeshProUGUI buttonText;

    [Header("Audio")]
    [SerializeField] private AudioSource pointerEnterSFX;
    [SerializeField] private AudioSource pointerDownSFX;

    public virtual void Start()
    {
        defaultScale = transform.localScale;
        targetScale = defaultScale;

        buttonImage = GetComponent<Button>().image;
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }


    public virtual void Update()
    {
        if (Mathf.Abs(transform.lossyScale.x - targetScale.x) > .01f)
        {
            float scaleValue = 
                Mathf.Lerp(transform.localScale.x, targetScale.x, Time.deltaTime * scaleSpeed);

            transform.localScale = new Vector3(scaleValue, scaleValue, scaleValue);
        }
    }
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = defaultScale * scaleRate;

        if (pointerEnterSFX != null)
            pointerEnterSFX.Play();

        if(buttonImage != null) 
            buttonImage.color = Color.yellow;

        if(buttonText != null) 
            buttonText.color = Color.yellow;
    }
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        ReturnDefaultLook();
    }
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        ReturnDefaultLook();

        if(pointerDownSFX != null)
            pointerDownSFX.Play();
    }
    private void ReturnDefaultLook()
    {
        targetScale = defaultScale;

        if(buttonImage != null) 
            buttonImage.color = Color.white;

        if(buttonText != null)
            buttonText.color = Color.white;
    }
    public void AssignAudioSource()
    {
        pointerEnterSFX = GameObject.Find("UI_PointerEnter").GetComponent<AudioSource>();
        pointerDownSFX = GameObject.Find("UI_PointerDown").GetComponent<AudioSource>();

    }
}
