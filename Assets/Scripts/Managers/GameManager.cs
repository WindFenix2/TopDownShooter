using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Settings")]
    public bool friendlyFire;

    private void Awake()
    {
        instance = this;
    }
}
