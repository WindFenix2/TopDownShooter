using UnityEngine;

public class Shotgun_KillShieldAbility : MonoBehaviour
{
    [Header("Shield rules")]
    [SerializeField] private int shieldPerKill = 50;
    [SerializeField] private int maxShield = 100;
    [SerializeField] private WeaponType boundWeaponType = WeaponType.Shotgun;

    [Header("VFX")]
    [SerializeField] private GameObject shieldVfxPrefab;
    [SerializeField] private Transform vfxParent;
    [SerializeField] private Vector3 vfxLocalPosition = new Vector3(0f, 1f, 0f);

    [Header("VFX - auto size by hitbox")]
    [SerializeField] private bool autoScaleVfxToHitbox = true;
    [SerializeField] private float vfxRadiusAtScaleOne = 1.6f; // под какой радиус префаб выгл€дит "норм" при scale=1
    [SerializeField] private float vfxScaleMultiplier = 1f;    // общий множитель

    [Header("VFX - freeze (optional)")]
    [SerializeField] private bool freezeVfxAfterTime = true;
    [SerializeField] private float freezeAfterSeconds = 3.5f;

    [Header("Hitbox (blocks bullets)")]
    [SerializeField] private bool spawnShieldHitbox = true;
    [SerializeField] private float hitboxRadius = 1.6f;
    [SerializeField] private Vector3 hitboxLocalPosition = new Vector3(0f, 1f, 0f);

    [Header("Debug (read-only)")]
    [SerializeField] private int currentShield;
    [SerializeField] private bool isBoundWeaponEquipped;

    private GameObject vfxInstance;
    private GameObject hitboxInstance;

    private float vfxShownTime;
    private bool vfxFrozen;

    private Player player;
    private Player_Health playerHealth;

    public int CurrentShield => currentShield;
    public int MaxShield => maxShield;

    private void Awake()
    {
        player = GetComponent<Player>();

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
        // заморозка VFX через N секунд
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

        if (isBoundWeaponEquipped && !nowBound)
            ResetShield();

        isBoundWeaponEquipped = nowBound;
        RefreshVisuals();
    }

    public void NotifyEnemyKilled(WeaponType usedWeapon)
    {
        if (usedWeapon != boundWeaponType)
            return;

        if (!isBoundWeaponEquipped)
            return;

        AddShield(shieldPerKill);
    }

    public int AbsorbDamage(int damage)
    {
        if (damage <= 0)
            return 0;

        if (!isBoundWeaponEquipped)
            return damage;

        if (currentShield <= 0)
            return damage;

        int absorbed = Mathf.Min(currentShield, damage);
        currentShield -= absorbed;

        RefreshVisuals();

        return damage - absorbed;
    }

    private void AddShield(int amount)
    {
        if (amount <= 0)
            return;

        int before = currentShield;
        currentShield = Mathf.Clamp(currentShield + amount, 0, maxShield);

        // если щит по€вилс€ впервые (0 -> >0), перезапускаем VFX-анимацию
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
        vfxShownTime = 0f;
        vfxFrozen = false;
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        bool shouldShow = isBoundWeaponEquipped && currentShield > 0;

        UpdateVfx(shouldShow);
        UpdateHitbox(shouldShow);

        // если включЄн авто-скейл Ч подгон€ем VFX под радиус
        if (shouldShow)
            ApplyAutoScaleToVfx();
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

            // каждый раз при включении хотим красивое по€вление -> Play()
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

        // масштабируем равномерно
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

            // стопим, но оставл€ем текущее состо€ние на экране
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

        // если ты помен€л радиус в инспекторе Ч обновим коллайдер на лету
        SphereCollider existing = hitboxInstance.GetComponent<SphereCollider>();
        if (existing != null && existing.radius != hitboxRadius)
            existing.radius = hitboxRadius;

        if (!hitboxInstance.activeSelf)
            hitboxInstance.SetActive(true);
    }
}