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
    private bool activeHasExit = true;

    [SerializeField] private SnapPoint nextSnapPoint;
    private SnapPoint defaultSnapPoint;

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
        }
        else
        {
            activeHasExit = true;
            activeLastLevelPart = lastLevelPart;
        }

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

        if (activeHasExit && activeLastLevelPart != null)
            GenerateNextLevelPart();

        navMeshSurface.BuildNavMesh();

        foreach (Enemy enemy in enemyList)
        {
            enemy.transform.parent = null;
            enemy.gameObject.SetActive(true);
        }

        MissionManager.instance.StartMission();
    }

    [ContextMenu("Create next level part")]
    private void GenerateNextLevelPart()
    {
        Transform newPart = null;

        if (generationOver)
            newPart = Instantiate(activeLastLevelPart);
        else
            newPart = Instantiate(ChooseRandomPart());

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
