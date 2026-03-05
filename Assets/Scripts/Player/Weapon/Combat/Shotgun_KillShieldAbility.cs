using UnityEngine;

public class Shotgun_KillShieldAbility : MonoBehaviour
{
    [Header("Shield rules")]
    [SerializeField] private int shieldPerKill = 50;
    [SerializeField] private int maxShield = 100;
    [SerializeField] private WeaponType boundWeaponType = WeaponType.Shotgun;

    [Header("After shield break")]
    [Tooltip("How long the player ignores damage right after the shield is fully broken.")]
    [SerializeField] private float invulnerableAfterBreakSeconds = 1f;

    [Header("Shield hitbox")]
    [SerializeField] private float hitboxRadius = 1.6f;

    private bool persistShieldAfterUnequip = true;

    [Header("Shield VFX")]
    [SerializeField] private GameObject shieldVfxPrefab;
    [SerializeField] private Transform vfxParent;
    [SerializeField] private Vector3 vfxLocalPosition = new Vector3(0f, 1f, 0f);

    [SerializeField] private bool autoScaleVfxToHitbox = true;
    [SerializeField] private float vfxRadiusAtScaleOne = 1.6f;
    [SerializeField] private float vfxScaleMultiplier = 1f;

    [SerializeField] private bool freezeVfxAfterTime = true;
    [SerializeField] private float freezeAfterSeconds = 3.5f;

    [Header("Shield Hitbox Spawner")]
    [SerializeField] private bool spawnShieldHitbox = true;
    [SerializeField] private Vector3 hitboxLocalPosition = new Vector3(0f, 1f, 0f);

    [SerializeField, HideInInspector] private int currentShield;
    [SerializeField, HideInInspector] private bool isBoundWeaponEquipped;

    private float invulAfterBreakEndTime;

    private GameObject vfxInstance;
    private GameObject hitboxInstance;

    private float vfxShownTime;
    private bool vfxFrozen;

    private Player player;
    private Player_Health playerHealth;

    public int CurrentShield => currentShield;
    public int MaxShield => maxShield;


    public bool TryConsumeShieldForEMI()
    {
        if (currentShield <= 0)
            return false;

        if (!ShieldIsAllowed())
            return false;

        ResetShield();
        return true;
    }

    public void SetPersistShield(bool value)
    {
        persistShieldAfterUnequip = value;

        if (!persistShieldAfterUnequip && !isBoundWeaponEquipped)
            ResetShield();
        else
            RefreshVisuals();
    }

    private void Awake()
    {
        player = GetComponent<Player>();

        if (GameManager.instance != null)
            persistShieldAfterUnequip = GameManager.instance.shotgunShieldPersists;

        if (player != null && player.health != null)
            playerHealth = player.health;
        else
            playerHealth = GetComponent<Player_Health>();

        if (vfxParent == null)
        {
            if (player != null && player.playerBody != null)
                vfxParent = player.playerBody;
            else
                vfxParent = transform;
        }
    }

    private void Update()
    {
        if (vfxInstance != null && vfxInstance.activeSelf && freezeVfxAfterTime && !vfxFrozen)
        {
            vfxShownTime += Time.deltaTime;
            if (vfxShownTime >= Mathf.Max(0f, freezeAfterSeconds))
                FreezeVfxNow();
        }
    }

    public void OnEquippedWeaponChanged(WeaponType newWeaponType)
    {
        bool nowBound = newWeaponType == boundWeaponType;

        if (isBoundWeaponEquipped && !nowBound && !persistShieldAfterUnequip)
            ResetShield();

        isBoundWeaponEquipped = nowBound;
        RefreshVisuals();
    }

    public void NotifyEnemyKilled(WeaponType usedWeapon)
    {
        if (usedWeapon != boundWeaponType)
            return;

        if (!isBoundWeaponEquipped && !persistShieldAfterUnequip)
            return;

        AddShield(shieldPerKill);
    }

    public int AbsorbDamage(int damage)
    {
        if (damage <= 0)
            return 0;

        if (Time.time < invulAfterBreakEndTime)
            return 0;

        if (!ShieldIsAllowed())
            return damage;

        if (currentShield <= 0)
            return damage;

        int before = currentShield;

        currentShield -= damage;
        if (currentShield < 0)
            currentShield = 0;

        if (before > 0 && currentShield <= 0)
        {
            float d = Mathf.Max(0f, invulnerableAfterBreakSeconds);
            invulAfterBreakEndTime = Time.time + d;
        }

        RefreshVisuals();

        return 0;
    }

    private void AddShield(int amount)
    {
        if (amount <= 0)
            return;

        int before = currentShield;
        currentShield = Mathf.Clamp(currentShield + amount, 0, maxShield);

        if (before <= 0 && currentShield > 0)
        {
            vfxShownTime = 0f;
            vfxFrozen = false;
        }

        RefreshVisuals();
    }

    private void ResetShield()
    {
        currentShield = 0;
        invulAfterBreakEndTime = 0f;
        vfxShownTime = 0f;
        vfxFrozen = false;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        bool shouldShow = ShieldIsAllowed() && currentShield > 0;

        UpdateVfx(shouldShow);
        UpdateHitbox(shouldShow);

        if (shouldShow)
            ApplyAutoScaleToVfx();
    }

    private bool ShieldIsAllowed()
    {
        return isBoundWeaponEquipped || persistShieldAfterUnequip;
    }

    private void UpdateVfx(bool shouldShow)
    {
        if (shieldVfxPrefab == null)
        {
            if (vfxInstance != null)
                vfxInstance.SetActive(false);
            return;
        }

        if (!shouldShow)
        {
            if (vfxInstance != null)
                vfxInstance.SetActive(false);
            return;
        }

        if (vfxInstance == null)
        {
            Transform parent = vfxParent != null ? vfxParent : transform;
            vfxInstance = Instantiate(shieldVfxPrefab, parent);
            vfxInstance.transform.localPosition = vfxLocalPosition;
            vfxInstance.transform.localRotation = Quaternion.identity;
            vfxInstance.transform.localScale = Vector3.one;
        }

        if (!vfxInstance.activeSelf)
        {
            vfxInstance.SetActive(true);

            vfxShownTime = 0f;
            vfxFrozen = false;

            ParticleSystem[] allPs = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < allPs.Length; i++)
            {
                if (allPs[i] == null) continue;
                allPs[i].Clear(true);
                allPs[i].Play(true);
            }
        }
    }

    private void ApplyAutoScaleToVfx()
    {
        if (!autoScaleVfxToHitbox)
            return;

        if (vfxInstance == null)
            return;

        float baseRadius = Mathf.Max(0.0001f, vfxRadiusAtScaleOne);
        float targetRadius = Mathf.Max(0.0001f, hitboxRadius);

        float k = (targetRadius / baseRadius) * Mathf.Max(0.0001f, vfxScaleMultiplier);

        vfxInstance.transform.localScale = Vector3.one * k;
    }

    private void FreezeVfxNow()
    {
        if (vfxInstance == null)
            return;

        ParticleSystem[] allPs = vfxInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < allPs.Length; i++)
        {
            if (allPs[i] == null) continue;
            allPs[i].Pause(true);
        }

        vfxFrozen = true;
    }

    private void UpdateHitbox(bool shouldShow)
    {
        if (!spawnShieldHitbox)
        {
            if (hitboxInstance != null)
                hitboxInstance.SetActive(false);
            return;
        }

        if (!shouldShow)
        {
            if (hitboxInstance != null)
                hitboxInstance.SetActive(false);
            return;
        }

        if (hitboxInstance == null)
        {
            Transform parent = vfxParent != null ? vfxParent : transform;

            hitboxInstance = new GameObject("Shotgun_ShieldHitbox");
            hitboxInstance.transform.SetParent(parent);
            hitboxInstance.transform.localPosition = hitboxLocalPosition;
            hitboxInstance.transform.localRotation = Quaternion.identity;
            hitboxInstance.transform.localScale = Vector3.one;

            hitboxInstance.layer = gameObject.layer;

            SphereCollider sc = hitboxInstance.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = hitboxRadius;

            Shotgun_ShieldHitbox hb = hitboxInstance.AddComponent<Shotgun_ShieldHitbox>();
            hb.Setup(this, playerHealth);
        }

        SphereCollider existing = hitboxInstance.GetComponent<SphereCollider>();
        if (existing != null && existing.radius != hitboxRadius)
            existing.radius = hitboxRadius;

        if (!hitboxInstance.activeSelf)
            hitboxInstance.SetActive(true);
    }
}
