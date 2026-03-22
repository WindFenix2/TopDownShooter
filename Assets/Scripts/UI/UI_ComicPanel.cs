using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_ComicPanel : MonoBehaviour, IPointerDownHandler
{
    private Image myImage;

    [SerializeField] private Image[] comicPanel;
    [SerializeField] private GameObject buttonToEnable;

    private bool comicShowOver;
    private int imageIndex;

    private void Start()
    {
        myImage = GetComponent<Image>();
        ShowNextImage();
    }

    /// <summary>
    /// Call before showing to display only the image at the given index.
    /// </summary>
    public void SetSingleImage(int index)
    {
        if (comicPanel == null || comicPanel.Length == 0)
            return;

        index = Mathf.Clamp(index, 0, comicPanel.Length - 1);

        // Hide all images, then show only the selected one
        for (int i = 0; i < comicPanel.Length; i++)
        {
            comicPanel[i].gameObject.SetActive(i == index);
        }

        // Reset the array to contain only the selected image
        comicPanel = new Image[] { comicPanel[index] };
        imageIndex = 0;
    }

    protected void ShowNextImage()
    {
        if (comicShowOver)
            return;

        StartCoroutine(ChangeImageAlpha(1,1.5f,ShowNextImage));
    }

    private IEnumerator ChangeImageAlpha(float targetAlpha, float duration, System.Action onComplete)
    {
        float time = 0;
        Color currentColor = comicPanel[imageIndex].color;
        float startAlpha = currentColor.a;

        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);

            comicPanel[imageIndex].color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
            yield return null;
        }

        comicPanel[imageIndex].color = new Color(currentColor.r, currentColor.g, currentColor.b, targetAlpha);

        imageIndex++;

        if(imageIndex >= comicPanel.Length)
        {
            FinishComicShow();
        }

        onComplete?.Invoke();
    }

    private void FinishComicShow()
    {
        StopAllCoroutines();
        comicShowOver = true;
        buttonToEnable.SetActive(true);
        myImage.raycastTarget = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        ShowNextImageOnClick();
    }

    private void ShowNextImageOnClick()
    {
        if (comicShowOver)
            return;

        if (imageIndex >= comicPanel.Length)
        {
            FinishComicShow();
            return;
        }

        comicPanel[imageIndex].color = Color.white;
        imageIndex++;

        if (imageIndex >= comicPanel.Length)
        {
            FinishComicShow();
            return;
        }

        ShowNextImage();
    }
}
