using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;


    private CinemachineVirtualCamera virtualCamera;
    private CinemachineFramingTransposer transposer;


    [Header("Camera distance")]
    [SerializeField] private bool canChangeCameraDistance;
    [SerializeField] private float distanceChangeRate;
    [SerializeField] private float targetCameraDistance;

    [Header("Camera rotation (Q/E)")]
    [SerializeField] private float rotationStep = 45f;
    [SerializeField] private float rotationSpeed = 8f;
    private float currentYawAngle;
    private float targetYawAngle;
    private Quaternion initialRotation;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Debug.LogWarning("You had more than one Camera Manager");
            Destroy(gameObject);
        }


        virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        transposer = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();

        initialRotation = transform.rotation;
    }

    private void Update()
    {
        UpdateCameraRotationInput();
        UpdateCameraRotation();
        UpdateCameraDistance();
    }

    #region Camera Rotation

    private void UpdateCameraRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            targetYawAngle -= rotationStep;

        if (Input.GetKeyDown(KeyCode.E))
            targetYawAngle += rotationStep;
    }

    private void UpdateCameraRotation()
    {
        currentYawAngle = Mathf.LerpAngle(currentYawAngle, targetYawAngle, rotationSpeed * Time.unscaledDeltaTime);
        transform.rotation = initialRotation * Quaternion.Euler(0f, currentYawAngle, 0f);
    }

    public float GetCurrentYawAngle() => currentYawAngle;

    #endregion

    private void UpdateCameraDistance()
    {
        if (canChangeCameraDistance == false)
            return;

        float currentDistnace = transposer.m_CameraDistance;

        if (Mathf.Abs(targetCameraDistance - currentDistnace) < .01f)
            return;
        
        transposer.m_CameraDistance =
            Mathf.Lerp(currentDistnace, targetCameraDistance, distanceChangeRate * Time.deltaTime);
    }

    public void ChangeCameraDistance(float distance, float newChangeRate = .25f)
    {
        distanceChangeRate = newChangeRate;
        targetCameraDistance = distance;
    }

    public void ChangeCameraTarget(Transform target,float cameraDistance = 10,float newLookAheadTime = 0)
    {
        virtualCamera.Follow = target;
        transposer.m_LookaheadTime = newLookAheadTime;
        ChangeCameraDistance(cameraDistance);
    }

}
