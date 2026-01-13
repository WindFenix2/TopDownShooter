using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI_GameOver : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI gameOverText;


    public void ShowGameOverMessage(string message)
    {
        gameOverText.text = message;
    }
}
