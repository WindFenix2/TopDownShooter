using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionObject_HuntTarget : MonoBehaviour
{
    public static event Action OnTargetKilled;

    public void InvokeOnTargetKilled() => OnTargetKilled?.Invoke();
}
