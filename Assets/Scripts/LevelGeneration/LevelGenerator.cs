using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;


public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator instance;

    // Enemies
    private List<Enemy> enemyList;

    [SerializeField] private NavMeshSurface navMeshSurface;
    [Space]

    [Header("Shared Level Parts Pool")]
    [Tooltip("Default exit part. Missions can override this via 'lastLevelPartOverride'.")]
    [SerializeField] private Transform lastLevelPart;
    [Tooltip("Default level parts list. Missions can override via 'availableLevelParts'. If mission leaves it empty, these are used.")]
    [SerializeField] private List<Transform> levelParts;
    private List<Transform> currentLevelParts;
    private List<Transform> generatedLevelParts = new List<Transform>();

    private Transform activeLastLevelPart;
    private Transform activePenultimatePart;
    private bool activeHasExit = true;
    private bool activeDisableCarSpawns = false;

    [SerializeField] private SnapPoint nextSnapPoint;
    private SnapPoint defaultSnapPoint;

    [Header("Car Spawns")]
    [SerializeField] private int minCars = 1;
    [SerializeField] private int maxCars = 3;
    private int carCount;

    [Space]
    [Tooltip("Delay between generating each level part (seconds). Controls generation speed.")]
    [SerializeField] private float generationCooldown;
    private float cooldownTimer;
    private bool generationOver = true;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        enemyList = new List<Enemy>();
        defaultSnapPoint = nextSnapPoint;
    }


    private void Update()
    {
        if (generationOver)
            return;

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer < 0)
        {
            if (currentLevelParts.Count > 0)
            {
                cooldownTimer = generationCooldown;
                GenerateNextLevelPart();
            }
            else if (generationOver == false)
            {
                FinishGeneration();
            }
        }
    }

    [ContextMenu("Restart generation")]
    public void InitializeGeneration()
    {
        nextSnapPoint = defaultSnapPoint;
        generationOver = false;

        // Resolve mission config
        Mission mission = MissionManager.instance != null ? MissionManager.instance.currentMission : null;

        if (mission != null && mission.availableLevelParts != null && mission.availableLevelParts.Count > 0)
            currentLevelParts = new List<Transform>(mission.availableLevelParts);
        else
            currentLevelParts = new List<Transform>(levelParts);

        // Limit level parts count if mission specifies it
        if (mission != null && mission.maxLevelParts > 0 && currentLevelParts.Count > mission.maxLevelParts)
        {
            // Shuffle and trim to desired count
            for (int i = currentLevelParts.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Transform temp = currentLevelParts[i];
                currentLevelParts[i] = currentLevelParts[j];
                currentLevelParts[j] = temp;
            }
            currentLevelParts.RemoveRange(mission.maxLevelParts, currentLevelParts.Count - mission.maxLevelParts);
        }

        if (mission != null)
        {
            activeHasExit = mission.hasExit;
            activeLastLevelPart = mission.lastLevelPartOverride != null ? mission.lastLevelPartOverride : lastLevelPart;
            activePenultimatePart = mission.penultimateLevelPartOverride;
            activeDisableCarSpawns = mission.disableCarSpawns;

            // Remove penultimate part from random pool so it only appears once
            if (activePenultimatePart != null)
                currentLevelParts.Remove(activePenultimatePart);
        }
        else
        {
            activeHasExit = true;
            activeLastLevelPart = lastLevelPart;
            activePenultimatePart = null;
            activeDisableCarSpawns = false;
        }

        carCount = 0;
        DestroyOldLevelPartsAndEnemies();
    }

    private void DestroyOldLevelPartsAndEnemies()
    {
        foreach (Enemy enemy in enemyList)
        {
            Destroy(enemy.gameObject);
        }

        foreach (Transform t in generatedLevelParts)
        {
            Destroy(t.gameObject);
        }

        generatedLevelParts = new List<Transform>();
        enemyList = new List<Enemy>();
    }

    private void FinishGeneration()
    {
        generationOver = true;

        // Insert penultimate part (e.g. long road) before the exit if mission specifies one
        if (activePenultimatePart != null)
        {
            GeneratePenultimatePart();
        }

        // Always generate the closing level part to seal the map,
        // regardless of whether the mission uses an exit trigger.
        // But only if there's a valid snap point to attach it to
        // (defence missions use a fixed map with no snap points).
        if (activeLastLevelPart != null && nextSnapPoint != null)
            GenerateLastLevelPart();

        LevelPart startPart = defaultSnapPoint.GetComponentInParent<LevelPart>();
        if (startPart != null)
        {
            startPart.ActivatePickupSpawnPoints();
            enemyList.AddRange(startPart.SpawnEnemiesFromSpawnPoints());

            if (!activeDisableCarSpawns)
                carCount += startPart.SpawnCarsFromSpawnPoints(carCount, maxCars);
        }

        // Guarantee minimum cars
        if (!activeDisableCarSpawns && carCount < minCars)
        {
            ForceSpawnMinCars();
        }

        // Disable NavMeshAgents before building NavMesh to prevent
        // "Failed to create agent because there is no valid NavMesh" warnings
        foreach (Enemy enemy in enemyList)
        {
            if (enemy.agent != null)
                enemy.agent.enabled = false;
        }

        navMeshSurface.BuildNavMesh();

        foreach (Enemy enemy in enemyList)
        {
            enemy.transform.parent = null;
            if (enemy.agent != null)
                enemy.agent.enabled = true;
            enemy.gameObject.SetActive(true);
        }
        // Disable intersection check colliders on all parts (no longer needed at runtime)
        // Prevents bullets from hitting invisible generation-only colliders
        if (startPart != null)
            startPart.DisableIntersectionColliders();

        foreach (Transform part in generatedLevelParts)
        {
            LevelPart lp = part.GetComponent<LevelPart>();
            if (lp != null)
                lp.DisableIntersectionColliders();
        }

        MissionManager.instance.StartMission();
    }

    [ContextMenu("Create next level part")]
    private void GenerateNextLevelPart()
    {
        Transform newPart = Instantiate(ChooseRandomPart());

        generatedLevelParts.Add(newPart);

        LevelPart levelPartScript = newPart.GetComponent<LevelPart>();
        levelPartScript.SnapAndAlignPartTo(nextSnapPoint);

        if (levelPartScript.IntersectionDetected())
        {
            InitializeGeneration();
            return;
        }

        // Close any unused entrances (e.g. second entrance on T-shaped parts)
        levelPartScript.CloseUnusedEntrances();

        nextSnapPoint = levelPartScript.GetExitPoint();

        enemyList.AddRange(levelPartScript.MyEnemies());
        enemyList.AddRange(levelPartScript.SpawnEnemiesFromSpawnPoints());

        levelPartScript.ActivatePickupSpawnPoints();

        if (!activeDisableCarSpawns)
            carCount += levelPartScript.SpawnCarsFromSpawnPoints(carCount, maxCars);
    }

    private void GenerateLastLevelPart()
    {
        Transform newPart = Instantiate(activeLastLevelPart);
        generatedLevelParts.Add(newPart);

        LevelPart levelPartScript = newPart.GetComponent<LevelPart>();
        levelPartScript.SnapAndAlignPartTo(nextSnapPoint);

        if (levelPartScript.IntersectionDetected())
        {
            InitializeGeneration();
            return;
        }

        nextSnapPoint = levelPartScript.GetExitPoint();

        enemyList.AddRange(levelPartScript.MyEnemies());
        enemyList.AddRange(levelPartScript.SpawnEnemiesFromSpawnPoints());
        levelPartScript.ActivatePickupSpawnPoints();
    }

    private void GeneratePenultimatePart()
    {
        Transform newPart = Instantiate(activePenultimatePart);
        generatedLevelParts.Add(newPart);

        LevelPart levelPartScript = newPart.GetComponent<LevelPart>();
        levelPartScript.SnapAndAlignPartTo(nextSnapPoint);

        if (levelPartScript.IntersectionDetected())
        {
            InitializeGeneration();
            return;
        }

        nextSnapPoint = levelPartScript.GetExitPoint();

        enemyList.AddRange(levelPartScript.MyEnemies());
        enemyList.AddRange(levelPartScript.SpawnEnemiesFromSpawnPoints());
        levelPartScript.ActivatePickupSpawnPoints();

        // For car mission: spawn exactly 1 car on the penultimate part
        CarSpawnPoint[] penultimateCarPoints = levelPartScript.GetCarSpawnPoints();
        if (penultimateCarPoints.Length > 0)
        {
            GameObject car = penultimateCarPoints[0].SpawnCar();
            if (car != null)
                carCount++;
        }
    }

    private void ForceSpawnMinCars()
    {
        List<CarSpawnPoint> allAvailable = new List<CarSpawnPoint>();

        foreach (Transform part in generatedLevelParts)
        {
            LevelPart lp = part.GetComponent<LevelPart>();
            if (lp != null)
            {
                CarSpawnPoint[] points = lp.GetCarSpawnPoints();
                allAvailable.AddRange(points);
            }
        }

        // Shuffle
        for (int i = allAvailable.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            CarSpawnPoint t = allAvailable[i];
            allAvailable[i] = allAvailable[j];
            allAvailable[j] = t;
        }

        foreach (CarSpawnPoint point in allAvailable)
        {
            if (carCount >= minCars)
                break;

            GameObject car = point.SpawnCar();
            if (car != null)
                carCount++;
        }
    }

    private Transform ChooseRandomPart()
    {
        int randomIndex = Random.Range(0, currentLevelParts.Count);

        Transform choosenPart = currentLevelParts[randomIndex];

        currentLevelParts.RemoveAt(randomIndex);

        return choosenPart;
    }

    public Enemy GetRandomEnemy()
    {
        int randomIndex = Random.Range(0, enemyList.Count);

        return enemyList[randomIndex];
    }

    public List<Enemy> GetEnemyList() => enemyList;
}
