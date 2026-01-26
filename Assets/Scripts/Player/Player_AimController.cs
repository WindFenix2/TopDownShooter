using System;
using UnityEngine;

public class Player_AimController : MonoBehaviour
{
    private CameraManager cameraManager;
    private Player player;
    private PlayerControls controls;

    [Header("Aim Viusal - Laser")]
    [SerializeField] private LineRenderer aimLaser;

    [Header("Aim Visual - Sprite (crosshair on ground)")]
    [SerializeField] private SpriteRenderer aimSprite;

    [Header("Aim Control")]
    [SerializeField] private float preciseAimCamDistance = 6;
    [SerializeField] private float regularAimCamDistance = 7;
    [SerializeField] private float camChangeRate = 5;

    [Header("Weapon camera")]
    [SerializeField] private float preciseAimOffsetFromRegular = -1f;

    [Header("Aim Setup")]
    [SerializeField] private Transform aim;
    [SerializeField] private bool isAimingPrecisly;
    [SerializeField] private float offsetChangeRate = 6;
    private float offsetY;

    [Header("Aim Layers")]
    [SerializeField] private LayerMask preciseAim;
    [SerializeField] private LayerMask regularAim;

    [Header("Anti self-aim (regular mode)")]
    [SerializeField] private float minRegularAimDistance = 1;

    [Header("Camera Control")]
    [SerializeField] private Transform cameraTarget;
    [Range(.5f, 1)]
    [SerializeField] private float minCameraDistance = 1.5f;
    [Range(1, 3f)]
    [SerializeField] private float maxCameraDistance = 4;
    [Range(3f, 5f)]
    [SerializeField] private float cameraSensetivity = 5f;

    private Vector2 mouseInput;
    private RaycastHit lastKnownMouseHit;

    private void Start()
    {
        cameraManager = CameraManager.instance;
        player = GetComponent<Player>();
        AssignInputEvents();

        if (aimSprite == null && aim != null)
            aimSprite = aim.GetComponent<SpriteRenderer>();

        SyncCameraDistanceFromCurrentWeapon();
    }

    private void Update()
    {
        if (player.health.isDead)
            return;

        if (player.controlsEnabled == false)
            return;

        UpdateAimVisuals();
        UpdateAimPosition();
        UpdateCameraPosition();
    }

    public void EnableAimLaer(bool enable)
    {
        if (aimLaser != null)
            aimLaser.enabled = enable;

        if (aimSprite != null)
            aimSprite.enabled = enable;
    }

    public void SyncCameraDistanceFromCurrentWeapon(bool applyNow = true)
    {
        if (player == null || player.weapon == null)
            return;

        Weapon w = player.weapon.CurrentWeapon();
        if (w == null)
            return;

        SetRegularAimCameraDistance(w.cameraDistance, applyNow);
    }

    public void SetRegularAimCameraDistance(float regularDistance, bool applyNow = true)
    {
        regularAimCamDistance = regularDistance;
        preciseAimCamDistance = Mathf.Max(0.5f, regularAimCamDistance + preciseAimOffsetFromRegular);

        if (!applyNow)
            return;

        if (cameraManager == null)
            return;

        float distToApply = isAimingPrecisly ? preciseAimCamDistance : regularAimCamDistance;
        cameraManager.ChangeCameraDistance(distToApply, camChangeRate);
    }

    public float GetRegularAimCameraDistance() => regularAimCamDistance;

    private void EnablePreciseAim(bool enable)
    {
        isAimingPrecisly = !isAimingPrecisly;

        if (enable)
        {
            cameraManager.ChangeCameraDistance(preciseAimCamDistance, camChangeRate);
            Time.timeScale = .9f;
        }
        else
        {
            cameraManager.ChangeCameraDistance(regularAimCamDistance, camChangeRate);
            Time.timeScale = 1;
        }
    }

    public Transform GetAimCameraTarget()
    {
        cameraTarget.position = player.transform.position;
        return cameraTarget;
    }

    private void UpdateAimVisuals()
    {
        if (aim == null || Camera.main == null)
            return;

        aim.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        if (aimLaser == null || player.weapon == null)
            return;

        aimLaser.enabled = player.weapon.WeaponReady();

        if (aimLaser.enabled == false)
            return;

        WeaponModel weaponModel = player.weaponVisuals.CurrentWeaponModel();

        weaponModel.transform.LookAt(aim);
        weaponModel.gunPoint.LookAt(aim);

        Transform gunPoint = player.weapon.GunPoint();
        Vector3 laserDirection = player.weapon.BulletDirection();

        float laserTipLenght = .5f;
        float gunDistance = player.weapon.CurrentWeapon().gunDistance;

        Vector3 endPoint = gunPoint.position + laserDirection * gunDistance;

        if (Physics.Raycast(gunPoint.position, laserDirection, out RaycastHit hit, gunDistance))
        {
            endPoint = hit.point;
            laserTipLenght = 0;
        }

        aimLaser.SetPosition(0, gunPoint.position);
        aimLaser.SetPosition(1, endPoint);
        aimLaser.SetPosition(2, endPoint + laserDirection * laserTipLenght);
    }

    private void UpdateAimPosition()
    {
        if (aim == null)
            return;

        aim.position = GetMouseHitInfo().point;

        Vector3 newAimPosition = isAimingPrecisly ? aim.position : transform.position;
        aim.position = new Vector3(aim.position.x, newAimPosition.y + AdjustedOffsetY(), aim.position.z);
    }

    private float AdjustedOffsetY()
    {
        if (isAimingPrecisly)
            offsetY = Mathf.Lerp(offsetY, 0, Time.deltaTime * offsetChangeRate * .5f);
        else
            offsetY = Mathf.Lerp(offsetY, 1, Time.deltaTime * offsetChangeRate);

        return offsetY;
    }

    public Transform Aim() => aim;
    public bool CanAimPrecisly() => isAimingPrecisly;

    public RaycastHit GetMouseHitInfo()
    {
        if (Camera.main == null)
            return lastKnownMouseHit;

        LayerMask maskToUse = isAimingPrecisly ? preciseAim : regularAim;

        Ray ray = Camera.main.ScreenPointToRay(mouseInput);

        RaycastHit[] hits = Physics.RaycastAll(ray, 999f, maskToUse, QueryTriggerInteraction.Ignore);

        if (hits != null && hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].transform != null && hits[i].transform.IsChildOf(player.transform))
                    continue;

                lastKnownMouseHit = hits[i];

                if (!isAimingPrecisly)
                    lastKnownMouseHit.point = ClampRegularAimPoint(lastKnownMouseHit.point);

                return lastKnownMouseHit;
            }
        }

        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 p = ray.GetPoint(enter);
            lastKnownMouseHit.point = (!isAimingPrecisly) ? ClampRegularAimPoint(p) : p;
            return lastKnownMouseHit;
        }

        return lastKnownMouseHit;
    }

    private Vector3 ClampRegularAimPoint(Vector3 point)
    {
        Vector3 p = point;
        Vector3 playerPos = transform.position;

        Vector3 flat = new Vector3(p.x - playerPos.x, 0, p.z - playerPos.z);
        float dist = flat.magnitude;

        if (dist < minRegularAimDistance)
        {
            Vector3 dir = flat.sqrMagnitude < 0.0001f ? transform.forward : flat.normalized;
            p = playerPos + dir * minRegularAimDistance;
        }

        return p;
    }

    #region Camera Region

    private void UpdateCameraPosition()
    {
        bool canMoveCamera = Vector3.Distance(cameraTarget.position, DesieredCameraPosition()) > 1;

        if (canMoveCamera == false)
            return;

        cameraTarget.position =
            Vector3.Lerp(cameraTarget.position, DesieredCameraPosition(), cameraSensetivity * Time.deltaTime);
    }

    private Vector3 DesieredCameraPosition()
    {
        float actualMaxCameraDistance = player.movement.moveInput.y < -.5f ? minCameraDistance : maxCameraDistance;

        Vector3 desiredCameraPosition = GetMouseHitInfo().point;
        Vector3 aimDirection = (desiredCameraPosition - transform.position).normalized;

        float distanceToDesierdPosition = Vector3.Distance(transform.position, desiredCameraPosition);
        float clampedDistance = Mathf.Clamp(distanceToDesierdPosition, minCameraDistance, actualMaxCameraDistance);

        desiredCameraPosition = transform.position + aimDirection * clampedDistance;
        desiredCameraPosition.y = transform.position.y + 1;

        return desiredCameraPosition;
    }

    #endregion

    private void AssignInputEvents()
    {
        controls = player.controls;

        controls.Character.PreciseAim.performed += context => EnablePreciseAim(true);
        controls.Character.PreciseAim.canceled += context => EnablePreciseAim(false);

        controls.Character.Aim.performed += context => mouseInput = context.ReadValue<Vector2>();
        controls.Character.Aim.canceled += context => mouseInput = Vector2.zero;
    }
}
