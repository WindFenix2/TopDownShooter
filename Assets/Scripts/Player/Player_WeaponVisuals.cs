using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Player_WeaponVisuals : MonoBehaviour
{
    private Player player;
    private Animator anim;

    [SerializeField] private WeaponModel[] weaponModels;
    [SerializeField] private BackupWeaponModel[] backupWeaponModels;

    [Header("Rig ")]
    [SerializeField] private float rigWeightIncreaseRate;
    private bool shouldIncrease_RigWeight;
    private Rig rig;

    [Header("Left hand IK")]
    [SerializeField] private float leftHandIkWeightIncreaseRate;
    [SerializeField] private TwoBoneIKConstraint leftHandIK;
    [SerializeField] private Transform leftHandIK_Target;
    private bool shouldIncrease_LeftHandIKWieght;

    private void Start()
    {
        player = GetComponent<Player>();
        anim = GetComponentInChildren<Animator>();
        rig = GetComponentInChildren<Rig>();

        weaponModels = GetComponentsInChildren<WeaponModel>(true);
        backupWeaponModels = GetComponentsInChildren<BackupWeaponModel>(true);

        if (player != null && player.weapon != null && player.weapon.CurrentWeapon() != null)
        {
            SwitchOnCurrentWeaponModel();
        }
    }

    private void Update()
    {
        UpdateRigWigth();
        UpdateLeftHandIKWeight();
    }

    public void PlayFireAnimation()
    {
        if (anim == null) return;
        anim.SetTrigger("Fire");
    }

    public void PlayReloadAnimation()
    {
        if (player == null || player.weapon == null) return;
        var w = player.weapon.CurrentWeapon();
        if (w == null) return;

        if (anim == null) return;

        float reloadSpeed = w.reloadSpeed;

        anim.SetFloat("ReloadSpeed", reloadSpeed);
        anim.SetTrigger("Reload");
        ReduceRigWeight();
    }

    public void PlayWeaponEquipAnimation()
    {
        if (player == null || player.weapon == null) return;
        var w = player.weapon.CurrentWeapon();
        if (w == null) return;

        var model = CurrentWeaponModel();
        if (model == null) return;

        if (anim == null) return;

        EquipType equipType = model.equipAnimationType;
        float equipmentSpeed = w.equipmentSpeed;

        if (leftHandIK != null) leftHandIK.weight = 0;

        ReduceRigWeight();
        anim.SetTrigger("EquipWeapon");
        anim.SetFloat("EquipType", (float)equipType);
        anim.SetFloat("EquipSpeed", equipmentSpeed);
    }

    public void SwitchOnCurrentWeaponModel()
    {
        if (player == null || player.weapon == null) return;

        var model = CurrentWeaponModel();
        if (model == null) return;

        int animationIndex = (int)model.holdType;

        SwitchOffWeaponModels();
        SwitchOffBackupWeaponModels();

        if (player.weapon.HasOnlyOneWeapon() == false)
            SwitchOnBackupWeaponModel();

        SwitchAnimationLayer(animationIndex);
        model.gameObject.SetActive(true);
        AttachLeftHand();
    }

    public void SwitchOffWeaponModels()
    {
        if (weaponModels == null) return;

        for (int i = 0; i < weaponModels.Length; i++)
        {
            if (weaponModels[i] != null)
                weaponModels[i].gameObject.SetActive(false);
        }
    }

    private void SwitchOffBackupWeaponModels()
    {
        if (backupWeaponModels == null) return;

        foreach (BackupWeaponModel backupModel in backupWeaponModels)
        {
            if (backupModel != null)
                backupModel.Activate(false);
        }
    }

    public void SwitchOnBackupWeaponModel()
    {
        if (player == null || player.weapon == null) return;
        if (backupWeaponModels == null) return;

        var current = player.weapon.CurrentWeapon();
        if (current == null) return;

        SwitchOffBackupWeaponModels();

        BackupWeaponModel lowHangWeapon = null;
        BackupWeaponModel backHangWeapon = null;
        BackupWeaponModel sideHangWeapon = null;

        foreach (BackupWeaponModel backupModel in backupWeaponModels)
        {
            if (backupModel == null) continue;

            if (backupModel.weaponType == current.weaponType)
                continue;

            if (player.weapon.WeaponInSlots(backupModel.weaponType) != null)
            {
                if (backupModel.HangTypeIs(HangType.LowBackHang))
                    lowHangWeapon = backupModel;

                if (backupModel.HangTypeIs(HangType.BackHang))
                    backHangWeapon = backupModel;

                if (backupModel.HangTypeIs(HangType.SideHang))
                    sideHangWeapon = backupModel;
            }
        }

        lowHangWeapon?.Activate(true);
        backHangWeapon?.Activate(true);
        sideHangWeapon?.Activate(true);
    }

    private void SwitchAnimationLayer(int layerIndex)
    {
        if (anim == null) return;

        for (int i = 1; i < anim.layerCount; i++)
        {
            anim.SetLayerWeight(i, 0);
        }

        anim.SetLayerWeight(layerIndex, 1);
    }

    public WeaponModel CurrentWeaponModel()
    {
        if (player == null || player.weapon == null) return null;
        var w = player.weapon.CurrentWeapon();
        if (w == null) return null;
        if (weaponModels == null) return null;

        WeaponType weaponType = w.weaponType;

        for (int i = 0; i < weaponModels.Length; i++)
        {
            if (weaponModels[i] != null && weaponModels[i].weaponType == weaponType)
                return weaponModels[i];
        }

        return null;
    }

    #region Animation Rigging Methods

    private void AttachLeftHand()
    {
        var model = CurrentWeaponModel();
        if (model == null) return;

        if (leftHandIK_Target == null) return;
        if (model.holdPoint == null) return;

        Transform targetTransform = model.holdPoint;

        leftHandIK_Target.localPosition = targetTransform.localPosition;
        leftHandIK_Target.localRotation = targetTransform.localRotation;
    }

    private void UpdateLeftHandIKWeight()
    {
        if (!shouldIncrease_LeftHandIKWieght) return;
        if (leftHandIK == null) { shouldIncrease_LeftHandIKWieght = false; return; }

        leftHandIK.weight += leftHandIkWeightIncreaseRate * Time.deltaTime;

        if (leftHandIK.weight >= 1)
            shouldIncrease_LeftHandIKWieght = false;
    }

    private void UpdateRigWigth()
    {
        if (!shouldIncrease_RigWeight) return;
        if (rig == null) { shouldIncrease_RigWeight = false; return; }

        rig.weight += rigWeightIncreaseRate * Time.deltaTime;

        if (rig.weight >= 1)
            shouldIncrease_RigWeight = false;
    }

    private void ReduceRigWeight()
    {
        if (rig == null) return;
        rig.weight = .15f;
    }

    public void MaximizeRigWeight() => shouldIncrease_RigWeight = true;
    public void MaximizeLeftHandWeight() => shouldIncrease_LeftHandIKWieght = true;

    #endregion
}
