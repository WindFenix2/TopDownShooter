using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Defence - Mission", menuName = "Missions/Defence - Mission")]
public class Mission_LastDefence : Mission
{
    public bool defenceBegun = false;
    private bool defenceCompleted = false;

    [Header("Defence Object")]
    [Tooltip("Name of the object being defended (shown in UI).")]
    public string defenceObjectName = "the radio tower";

    [Header("Wave System")]
    public List<WaveData> waves = new List<WaveData>();
    private int currentWaveIndex = -1;
    private int enemiesAliveInWave;
    private float waveCountdownTimer;
    private bool waitingForNextWave;

    [Header("Cooldown between waves")]
    public float timeBetweenWaves = 10f;

    [Header("Respawn details")]
    [Tooltip("How many of the closest MissionObject_EnemyRespawnPoint to use for spawning.")]
    public int amountOfRespawnPoints = 2;

    [Tooltip("Random XZ offset for each spawned enemy to prevent overlapping.")]
    public float spawnSpreadRadius = 3f;

    private List<Transform> respawnPoints;
    private Vector3 defencePoint;

    private void OnEnable()
    {
        defenceBegun = false;
        defenceCompleted = false;
        currentWaveIndex = -1;
        enemiesAliveInWave = 0;
        waitingForNextWave = false;
    }

    public override void StartMission()
    {
        // Reset all runtime state (ScriptableObjects persist between Editor play sessions!)
        defenceBegun = false;
        defenceCompleted = false;
        currentWaveIndex = -1;
        enemiesAliveInWave = 0;
        waitingForNextWave = false;
        respawnPoints = null;

        UI.instance.inGameUI.UpdateMissionInfo(
            $"Approach {defenceObjectName} to begin defence.");
    }

    public override bool MissionCompleted()
    {
        return defenceCompleted;
    }

    public override void UpdateMission()
    {
        if (!defenceBegun || defenceCompleted)
            return;


        if (waitingForNextWave)
        {
            waveCountdownTimer -= Time.deltaTime;

            if (waveCountdownTimer <= 0)
            {
                waitingForNextWave = false;
                StartNextWave();
            }
            else
            {
                string countdownText = Mathf.CeilToInt(waveCountdownTimer).ToString();
                UI.instance.inGameUI.UpdateMissionInfo(
                    $"Wave {currentWaveIndex + 2} incoming!",
                    $"Prepare yourself... {countdownText}s");
            }
            return;
        }


        if (currentWaveIndex >= 0 && enemiesAliveInWave <= 0)
        {
            if (currentWaveIndex >= waves.Count - 1)
            {
                defenceCompleted = true;
                UI.instance.inGameUI.UpdateMissionInfo("Defence complete! You survived!");

                if (!hasExit)
                    GameManager.instance.GameCompleted();

                return;
            }
            else
            {
                waitingForNextWave = true;
                waveCountdownTimer = timeBetweenWaves;
                UI.instance.inGameUI.UpdateMissionInfo(
                    $"Wave {currentWaveIndex + 1} cleared!",
                    "Next wave approaching...");
            }
        }
        else
        {
            string missionText = $"Defend {defenceObjectName}!";
            string missionDetails = $"Wave {currentWaveIndex + 1}/{waves.Count} — Enemies left: {enemiesAliveInWave}";
            UI.instance.inGameUI.UpdateMissionInfo(missionText, missionDetails);
        }
    }

    public void StartDefenceEvent()
    {
        if (defenceBegun)
            return;

        defenceBegun = true;

        // Initialize respawn points now (not in StartMission) to avoid stale ScriptableObject references
        defencePoint = Object.FindObjectOfType<MissionEnd_Trigger>()?.transform.position ?? Vector3.zero;
        respawnPoints = new List<Transform>(ClosestPoints(amountOfRespawnPoints));

        UI.instance.inGameUI.UpdateMissionInfo(
            $"Defend {defenceObjectName}!");

        StartNextWave();
    }

    private void StartNextWave()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= waves.Count)
        {
            defenceCompleted = true;
            return;
        }

        WaveData wave = waves[currentWaveIndex];
        enemiesAliveInWave = 0;

        SpawnEnemiesForWave(wave.meleeEnemies, wave.meleeCount);
        SpawnEnemiesForWave(wave.rangeEnemies, wave.rangeCount);
        SpawnEnemiesForWave(wave.bossPrefabs, wave.bossCount);

        string waveInfo = $"Wave {currentWaveIndex + 1}/{waves.Count} started!";
        UI.instance.inGameUI.ShowCenterMessage(waveInfo);
    }

    private void SpawnEnemiesForWave(GameObject[] possiblePrefabs, int count)
    {
        if (possiblePrefabs == null || possiblePrefabs.Length == 0 || count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            int randomEnemyIndex = Random.Range(0, possiblePrefabs.Length);
            SpawnSingleEnemy(possiblePrefabs[randomEnemyIndex]);
        }
    }

    private void SpawnSingleEnemy(GameObject prefab)
    {
        if (prefab == null || respawnPoints == null || respawnPoints.Count == 0)
            return;

        int randomRespawnIndex = Random.Range(0, respawnPoints.Count);
        Transform spawnPoint = respawnPoints[randomRespawnIndex];

        GameObject spawned = ObjectPool.instance.GetObject(prefab, spawnPoint);

        if (spawned != null)
        {
            // Apply random spread so enemies don't stack on top of each other
            Vector2 rndCircle = Random.insideUnitCircle * spawnSpreadRadius;
            spawned.transform.position = spawnPoint.position + new Vector3(rndCircle.x, 0f, rndCircle.y);

            Enemy enemy = spawned.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Set high aggression on the INSTANCE, not the prefab!
                enemy.aggresionRange = 100;
                enemiesAliveInWave++;
                enemy.onDeath += OnWaveEnemyDied;
            }
        }
    }

    private void OnWaveEnemyDied()
    {
        enemiesAliveInWave = Mathf.Max(0, enemiesAliveInWave - 1);
    }

    private List<Transform> ClosestPoints(int amount)
    {
        List<Transform> closestPoints = new List<Transform>();
        List<MissionObject_EnemyRespawnPoint> allPoints =
            new List<MissionObject_EnemyRespawnPoint>(FindObjectsOfType<MissionObject_EnemyRespawnPoint>());

        while (closestPoints.Count < amount && allPoints.Count > 0)
        {
            float shortestDistance = float.MaxValue;
            MissionObject_EnemyRespawnPoint closestPoint = null;

            foreach (var point in allPoints)
            {
                float distance = Vector3.Distance(point.transform.position, defencePoint);

                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closestPoint = point;
                }
            }

            if (closestPoint != null)
            {
                closestPoints.Add(closestPoint.transform);
                allPoints.Remove(closestPoint);
            }
        }

        return closestPoints;
    }
}

[System.Serializable]
public struct WaveData
{
    public string waveName;

    [Header("Melee")]
    [Tooltip("Possible melee enemy prefabs (random pick).")]
    public GameObject[] meleeEnemies;
    [Tooltip("How many melee enemies to spawn.")]
    public int meleeCount;

    [Header("Range")]
    [Tooltip("Possible range enemy prefabs (random pick).")]
    public GameObject[] rangeEnemies;
    [Tooltip("How many range enemies to spawn.")]
    public int rangeCount;

    [Header("Boss")]
    [Tooltip("Possible boss enemy prefabs (random pick).")]
    public GameObject[] bossPrefabs;
    [Tooltip("How many bosses to spawn.")]
    public int bossCount;
}

