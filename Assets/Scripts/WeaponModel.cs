using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GrabType { SideGrab, BackGrab };
public enum HoldType { CommonHold = 1, LowHold, HighHold};

public class WeaponModel : MonoBehaviour
{
    
    public WeaponType weaponType;
    public GrabType grabType;
    public HoldType holdType;

    public Transform gunType;
    public Transform holdPoint;

}
