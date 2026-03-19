using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_EnemyTracker : MonoBehaviour
{
    public static UI_EnemyTracker instance;

    [Header("Sprites")]
    public Sprite arrowSprite;
    [Tooltip("Icon shown when tracking the exit point instead of an enemy.")]
    public Sprite exitSprite;

    [Header("Settings")]
    [SerializeField] private float arrowSize = 50f;
    [SerializeField] private float edgeOffset = 80f;
    [SerializeField] private Color arrowColor = Color.red;
    [SerializeField] private float distanceFontSize = 20f;

    private RectTransform arrowRect;
    private Image arrowImage;
    private TextMeshProUGUI distanceText;
    private GameObject trackerParent;

    private Transform playerTransform;
    private Camera mainCamera;
    private bool isTracking;

    private void Awake()
    {
        instance = this;
        mainCamera = Camera.main;

        CreateTrackerUI();
        SetTracking(false);
    }

    private void CreateTrackerUI()
    {
        trackerParent = new GameObject("EnemyTracker_Container");
        trackerParent.transform.SetParent(transform, false);

        RectTransform parentRT = trackerParent.AddComponent<RectTransform>();
        parentRT.anchorMin = new Vector2(0.5f, 0.5f);
        parentRT.anchorMax = new Vector2(0.5f, 0.5f);
        parentRT.pivot = new Vector2(0.5f, 0.5f);
        parentRT.sizeDelta = new Vector2(arrowSize, arrowSize + 30f);

        GameObject arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(trackerParent.transform, false);

        arrowRect = arrowGO.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
        arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.sizeDelta = new Vector2(arrowSize, arrowSize);

        arrowImage = arrowGO.AddComponent<Image>();
        arrowImage.color = arrowColor;
        arrowImage.raycastTarget = false;

        if (arrowSprite != null)
            arrowImage.sprite = arrowSprite;

        GameObject textGO = new GameObject("DistanceText");
        textGO.transform.SetParent(trackerParent.transform, false);

        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0.5f);
        textRT.anchorMax = new Vector2(0.5f, 0.5f);
        textRT.pivot = new Vector2(0.5f, 1f);
        textRT.anchoredPosition = new Vector2(0, -(arrowSize / 2f + 5f));
        textRT.sizeDelta = new Vector2(100f, 30f);

        distanceText = textGO.AddComponent<TextMeshProUGUI>();
        distanceText.text = "";
        distanceText.fontSize = distanceFontSize;
        distanceText.alignment = TextAlignmentOptions.Center;
        distanceText.color = Color.white;
        distanceText.enableWordWrapping = false;
        distanceText.overflowMode = TextOverflowModes.Overflow;
        distanceText.raycastTarget = false;

        TextMeshProUGUI existingText = GetComponentInParent<Canvas>()?.GetComponentInChildren<TextMeshProUGUI>();
        if (existingText != null && existingText.font != null)
        {
            distanceText.font = existingText.font;
            if (existingText.fontMaterial != null)
                distanceText.fontMaterial = existingText.fontMaterial;
        }

        trackerParent.SetActive(false);
    }

    private void LateUpdate()
    {
        if (!isTracking)
            return;

        if (playerTransform == null)
        {
            Player player = GameManager.instance?.player;
            if (player != null)
                playerTransform = player.transform;
            else
                return;
        }

        Transform closestTarget = FindClosestHuntTarget();

        if (closestTarget == null)
        {
            trackerParent.SetActive(false);
            return;
        }

        trackerParent.SetActive(true);

        // Swap icon based on target type (enemy vs exit)
        bool isExit = closestTarget.GetComponent<Enemy>() == null;
        Sprite targetSprite = (isExit && exitSprite != null) ? exitSprite : arrowSprite;
        if (arrowImage.sprite != targetSprite)
            arrowImage.sprite = targetSprite;

        UpdateArrowPosition(closestTarget);
    }

    public void SetTracking(bool enabled)
    {
        StopAllCoroutines();

        if (enabled)
        {
            StartCoroutine(DelayedTracking());
        }
        else
        {
            isTracking = false;
            if (trackerParent != null)
                trackerParent.SetActive(false);
        }
    }

    private IEnumerator DelayedTracking()
    {
        yield return new WaitForSeconds(5f);
        isTracking = true;
    }

    private Transform FindClosestHuntTarget()
    {
        MissionObject_HuntTarget[] targets = FindObjectsOfType<MissionObject_HuntTarget>();

        if (targets == null || targets.Length == 0)
            return null;

        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (MissionObject_HuntTarget target in targets)
        {
            if (target == null) continue;

            // Skip dead enemies, but allow non-enemy targets (e.g. exit markers)
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null && enemy.IsDead) continue;

            float dist = Vector3.Distance(playerTransform.position, target.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = target.transform;
            }
        }

        if (distanceText != null)
        {
            if (closest != null)
                distanceText.text = Mathf.RoundToInt(closestDist) + "m";
            else
                distanceText.text = "";
        }

        return closest;
    }

    private void UpdateArrowPosition(Transform target)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null) return;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(target.position);

        bool isOnScreen = screenPos.z > 0
            && screenPos.x > 0 && screenPos.x < Screen.width
            && screenPos.y > 0 && screenPos.y < Screen.height;

        if (isOnScreen)
        {
            arrowImage.enabled = false;
            trackerParent.transform.position = screenPos;
        }
        else
        {
            arrowImage.enabled = true;

            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
            Vector3 dir = (screenPos - screenCenter).normalized;

            if (screenPos.z < 0)
                dir = -dir;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrowRect.localRotation = Quaternion.Euler(0, 0, angle - 90f);

            Vector3 clampedPos = new Vector3(
                Mathf.Clamp(screenCenter.x + dir.x * (Screen.width / 2f - edgeOffset), edgeOffset, Screen.width - edgeOffset),
                Mathf.Clamp(screenCenter.y + dir.y * (Screen.height / 2f - edgeOffset), edgeOffset, Screen.height - edgeOffset),
                0
            );

            trackerParent.transform.position = clampedPos;
        }
    }
}
