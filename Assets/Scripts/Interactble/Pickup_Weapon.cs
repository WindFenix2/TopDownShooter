using System.Collections;
using UnityEngine;

public class Pickup_Weapon : Interactable
{
    [SerializeField] private Weapon_Data weaponData;
    [SerializeField] private Weapon weapon;

    [SerializeField] private BackupWeaponModel[] models;

    [Header("Drop physics")]
    [SerializeField] private bool usePhysicsDrop = true;
    [SerializeField] private float spawnUpOffset = 0.15f;
    [SerializeField] private float maxLinearSpeed = 5f;
    [SerializeField] private float maxAngularSpeed = 10f;
    [SerializeField] private float settleTime = 0.35f;
    [SerializeField] private float minYKill = -50f;

    private Rigidbody rb;
    private bool oldWeapon;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (!oldWeapon)
            SyncWeaponDataFromActiveModelRuntime();
    }

    private void OnEnable()
    {
        if (usePhysicsDrop && rb != null)
            rb.maxAngularVelocity = maxAngularSpeed;
    }

    private void Start()
    {
        if (!oldWeapon)
            SyncWeaponDataFromActiveModelRuntime();

        if (!oldWeapon && weaponData != null)
            weapon = new Weapon(weaponData);

        SetupGameObject();

        if (usePhysicsDrop && rb != null)
            rb.maxAngularVelocity = maxAngularSpeed;
    }

    private void OnDisable()
    {
        oldWeapon = false;
        weapon = null;
    }

    public void SetupPickupWeapon(Weapon weapon, Transform dropFrom)
    {
        oldWeapon = true;

        this.weapon = weapon;
        weaponData = weapon.weaponData;

        Vector3 pos = dropFrom.position;
        if (usePhysicsDrop)
            pos += Vector3.up * spawnUpOffset;

        transform.position = pos;

        SetupGameObject();

        if (usePhysicsDrop && rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();

            rb.maxAngularVelocity = maxAngularSpeed;

            StopAllCoroutines();
            StartCoroutine(SettleRoutine());
        }
    }

    private IEnumerator SettleRoutine()
    {
        float t = 0f;
        while (t < settleTime)
        {
            ClampPhysics();
            t += Time.deltaTime;
            yield return null;
        }

        ClampPhysics();
    }

    private void FixedUpdate()
    {
        if (!usePhysicsDrop || rb == null)
            return;

        ClampPhysics();

        if (transform.position.y < minYKill)
            ObjectPool.instance.ReturnObject(gameObject);
    }

    private void ClampPhysics()
    {
        Vector3 v = rb.velocity;
        float speed = v.magnitude;
        if (speed > maxLinearSpeed)
            rb.velocity = v.normalized * maxLinearSpeed;

        Vector3 av = rb.angularVelocity;
        float aSpeed = av.magnitude;
        if (aSpeed > maxAngularSpeed)
            rb.angularVelocity = av.normalized * maxAngularSpeed;
    }

    [ContextMenu("Update Item Model")]
    public void SetupGameObject()
    {
        if (weaponData == null)
            return;

        if (!oldWeapon && (weapon == null || weapon.weaponData != weaponData))
            weapon = new Weapon(weaponData);

        if (rb != null)
            rb.mass = weaponData.pickupMass;

        gameObject.name = "Pickup_Weapon - " + weaponData.weaponType.ToString();
        SetupWeaponModel();
    }

    private void SetupWeaponModel()
    {
        if (weaponData == null)
            return;

        foreach (BackupWeaponModel model in models)
        {
            if (model == null) continue;

            model.gameObject.SetActive(false);

            if (model.weaponType == weaponData.weaponType)
            {
                model.gameObject.SetActive(true);
                UpdateMeshAndMaterial(model.GetComponent<MeshRenderer>());
            }
        }
    }

    public override void Interaction()
    {
        weaponController.PickupWeapon(weapon);
        WakeNearbyRagdolls();
        ObjectPool.instance.ReturnObject(gameObject);
    }

    public void SetupRandomWeapon()
    {
        WeaponType[] allTypes = (WeaponType[])System.Enum.GetValues(typeof(WeaponType));
        WeaponType randomType = allTypes[Random.Range(0, allTypes.Length)];

        Weapon_Data data = FindWeaponDataByType(randomType);
        if (data == null)
            return;

        weaponData = data;
        oldWeapon = false;
        weapon = new Weapon(data);
        SetupGameObject();
    }

    private void SyncWeaponDataFromActiveModelRuntime()
    {
        if (models == null || models.Length == 0)
            return;

        BackupWeaponModel activeModel = null;
        int activeCount = 0;

        for (int i = 0; i < models.Length; i++)
        {
            if (models[i] == null) continue;

            if (models[i].gameObject.activeSelf)
            {
                activeModel = models[i];
                activeCount++;
            }
        }

        if (activeCount != 1 || activeModel == null)
            return;

        if (weaponData != null && weaponData.weaponType == activeModel.weaponType)
            return;

        Weapon_Data found = FindWeaponDataByType(activeModel.weaponType);
        if (found != null)
            weaponData = found;
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (models == null || models.Length == 0)
            return;

        BackupWeaponModel activeModel = null;
        int activeCount = 0;

        for (int i = 0; i < models.Length; i++)
        {
            if (models[i] == null) continue;

            if (models[i].gameObject.activeSelf)
            {
                activeModel = models[i];
                activeCount++;
            }
        }

        if (activeCount != 1 || activeModel == null)
            return;

        if (weaponData != null && weaponData.weaponType == activeModel.weaponType)
            return;

        Weapon_Data found = FindWeaponDataByType(activeModel.weaponType);
        if (found != null)
            weaponData = found;
    }

    private Weapon_Data FindWeaponDataByType(WeaponType type)
    {
        Weapon_Data[] all = Resources.FindObjectsOfTypeAll<Weapon_Data>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].weaponType == type)
                return all[i];
        }

        return null;
    }
}