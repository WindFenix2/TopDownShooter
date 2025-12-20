using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UIElements;

public class PlayerWeaponVisuals : MonoBehaviour
{
    private Animator anim;
    private bool isGrabbingWeapon;


    #region Gun transforms region
    [SerializeField] private Transform[] gunTransforms;

    [SerializeField] private Transform pistol;
    [SerializeField] private Transform revolver;
    [SerializeField] private Transform autoRifle;
    [SerializeField] private Transform shotgun;
    [SerializeField] private Transform rifle;

    private Transform currentGun;
    #endregion 

    [Header("Rig")]
    [SerializeField] private float rigWeightIncreaseRate;
    private bool shouldIncrease_RigWeight;
    private Rig rig;

    [Header("Left hand IK")]
    [SerializeField] private float leftHandIKWeightIncreaseRate;
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform leftHandIK_Target;
    private bool shouldIncrease_LeftHandIKWiegth;



    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        rig = GetComponentInChildren<Rig>();

        SwitchOn(pistol);
    }

    private void Update()
    {
        CheckWeaponSwitch();


        if (Input.GetKeyDown(KeyCode.R) && isGrabbingWeapon == false)
        {
            anim.SetTrigger("Reload");
            ReduseRigWeight();
        }

        UpdateRigWeigth();
        UpdateLeftHandIKWeigth();

    }

    private void UpdateLeftHandIKWeigth()
    {
        if (shouldIncrease_LeftHandIKWiegth)
        {
            leftHandIK.weight += leftHandIKWeightIncreaseRate * Time.deltaTime;

            if (leftHandIK.weight >= 1)
                shouldIncrease_LeftHandIKWiegth = false;
        }
    }

    private void UpdateRigWeigth()
    {
        if (shouldIncrease_RigWeight)
        {
            rig.weight += rigWeightIncreaseRate * Time.deltaTime;

            if (rig.weight >= 1)
                shouldIncrease_RigWeight = false;
        }
    }

    private void ReduseRigWeight()
    {
        rig.weight = .15f;
    }

    private void PlayWeaponGrabAnimation(GrabType grabType)
    {
        leftHandIK.weight = 0;
        ReduseRigWeight();
        anim.SetFloat("WeaponGrabType", ((float)grabType));
        anim.SetTrigger("WeaponGrab");

        SetBusyGrabbingWeaponTo(true);
    }

    public void SetBusyGrabbingWeaponTo(bool busy)
    {
        isGrabbingWeapon = busy;
        anim.SetBool("BusyGrabbingWeapon", isGrabbingWeapon);
    }

    public void MaximizeRigWeight() => shouldIncrease_RigWeight = true;
    public void MaximizeLeftHandWeight() => shouldIncrease_LeftHandIKWiegth = true; 


    private void SwitchOn(Transform gunTransform)
    {
        SwithOffGuns();
        gunTransform.gameObject.SetActive(true);
        currentGun = gunTransform;

        AttachLeftHand();
    }

    private void SwithOffGuns()
    {
        for (int i = 0; i < gunTransforms.Length; i++)
        {
            gunTransforms[i].gameObject.SetActive(false);
        }
    }

    private void AttachLeftHand()
    {
        Transform targetTransform = currentGun.GetComponentInChildren<LeftHandTargetTransform>().transform;

        leftHandIK_Target.localPosition = targetTransform.localPosition;
        leftHandIK_Target.localRotation = targetTransform.localRotation;
    }

    private void SwitchAnimationLayer(int layerIndex)
    {
        for (int i = 1; i < anim.layerCount; i++)
        {
            anim.SetLayerWeight(i, 0);
        }

        anim.SetLayerWeight(layerIndex, 1); 
    }


    private void CheckWeaponSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SwitchOn(pistol);
            SwitchAnimationLayer(1);
            PlayWeaponGrabAnimation(GrabType.SideGrab);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SwitchOn(revolver);
            SwitchAnimationLayer(1);
            PlayWeaponGrabAnimation(GrabType.SideGrab);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SwitchOn(autoRifle);
            SwitchAnimationLayer(1);
            PlayWeaponGrabAnimation(GrabType.BackGrab);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SwitchOn(shotgun);
            SwitchAnimationLayer(2);
            PlayWeaponGrabAnimation(GrabType.BackGrab);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SwitchOn(rifle);
            SwitchAnimationLayer(3);
            PlayWeaponGrabAnimation(GrabType.BackGrab);
        }
    }
}

public enum GrabType { SideGrab, BackGrab  };