using UnityEngine;
using UnityEditor;

public class SpawnPointCreator : Editor
{
    [MenuItem("GameObject/Spawn Points/Enemy - Melee Spawn", false, 10)]
    static void CreateMeleeSpawn()
    {
        var go = CreateSpawnPointGO("EnemySpawn_Melee");
        var sp = go.AddComponent<EnemySpawnPoint>();
        sp.spawnType = EnemyType.Melee;
    }

    [MenuItem("GameObject/Spawn Points/Enemy - Range Spawn", false, 11)]
    static void CreateRangeSpawn()
    {
        var go = CreateSpawnPointGO("EnemySpawn_Range");
        var sp = go.AddComponent<EnemySpawnPoint>();
        sp.spawnType = EnemyType.Range;
    }

    [MenuItem("GameObject/Spawn Points/Enemy - Boss Spawn", false, 12)]
    static void CreateBossSpawn()
    {
        var go = CreateSpawnPointGO("EnemySpawn_Boss");
        var sp = go.AddComponent<EnemySpawnPoint>();
        sp.spawnType = EnemyType.Boss;
    }

    [MenuItem("GameObject/Spawn Points/Pickup - Medkit Spawn", false, 30)]
    static void CreateMedkitSpawn()
    {
        var go = CreateSpawnPointGO("PickupSpawn_Medkit");
        var sp = go.AddComponent<PickupSpawnPoint>();
        sp.pickupType = PickupSpawnPoint.PickupType.Medkit;
    }

    [MenuItem("GameObject/Spawn Points/Pickup - Ammo Spawn", false, 31)]
    static void CreateAmmoSpawn()
    {
        var go = CreateSpawnPointGO("PickupSpawn_Ammo");
        var sp = go.AddComponent<PickupSpawnPoint>();
        sp.pickupType = PickupSpawnPoint.PickupType.Ammo;
    }

    [MenuItem("GameObject/Spawn Points/Pickup - Weapon Spawn", false, 32)]
    static void CreateWeaponSpawn()
    {
        var go = CreateSpawnPointGO("PickupSpawn_Weapon");
        var sp = go.AddComponent<PickupSpawnPoint>();
        sp.pickupType = PickupSpawnPoint.PickupType.Weapon;
    }

    [MenuItem("GameObject/Spawn Points/Car Spawn", false, 40)]
    static void CreateCarSpawn()
    {
        var go = CreateSpawnPointGO("CarSpawn");
        go.AddComponent<CarSpawnPoint>();
    }

    [MenuItem("GameObject/Spawn Points/Mission - Vehicle Spawn", false, 50)]
    static void CreateVehicleSpawn()
    {
        var go = CreateSpawnPointGO("MissionSpawn_Vehicle");
        var sp = go.AddComponent<MissionSpawnPoint>();
        sp.category = MissionSpawnPoint.SpawnCategory.Vehicle;
    }

    [MenuItem("GameObject/Spawn Points/Mission - Gasoline Spawn", false, 51)]
    static void CreateGasolineSpawn()
    {
        var go = CreateSpawnPointGO("MissionSpawn_Gasoline");
        var sp = go.AddComponent<MissionSpawnPoint>();
        sp.category = MissionSpawnPoint.SpawnCategory.Gasoline;
    }

    [MenuItem("GameObject/Spawn Points/Mission - Enemy Respawn Point (Defence)", false, 52)]
    static void CreateDefenceRespawn()
    {
        var go = CreateSpawnPointGO("DefenceRespawnPoint");
        go.AddComponent<MissionObject_EnemyRespawnPoint>();
    }

    private static GameObject CreateSpawnPointGO(string name)
    {
        var go = new GameObject(name);

        if (Selection.activeTransform != null)
            go.transform.SetParent(Selection.activeTransform, false);

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
            go.transform.position = sceneView.camera.transform.position + sceneView.camera.transform.forward * 3f;

        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);

        return go;
    }
}
