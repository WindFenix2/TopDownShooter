using UnityEngine;

public class Revolver_StuckBullet : MonoBehaviour
{
    [Header("Stick")]
    [SerializeField] private float embedDepth = 0.06f;
    [SerializeField] private float autoReturnAfter = 0f;

    [Header("Detonation")]
    [SerializeField] private GameObject detonationFx;
    [SerializeField] private float detonationFxLife = 1f;

    private Revolver_StickyBulletsManager manager;
    private Rigidbody rb;
    private Collider col;
    private bool stuck;
    private float spawnTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        stuck = false;
        spawnTime = Time.time;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (col != null)
            col.isTrigger = false;

        transform.SetParent(null);
    }

    public void Setup(Revolver_StickyBulletsManager newManager)
    {
        manager = newManager;
    }

    private void Update()
    {
        if (!stuck && autoReturnAfter > 0f && Time.time - spawnTime > autoReturnAfter)
            gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (stuck) return;
        if (collision == null || collision.contactCount == 0) return;
        if (collision.collider == null) return;

        StickTo(collision);
    }

    private void StickTo(Collision collision)
    {
        stuck = true;

        ContactPoint cp = collision.GetContact(0);
        Vector3 normal = cp.normal;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Vector3 fwd = -normal;
        if (fwd.sqrMagnitude < 0.0001f)
            fwd = transform.forward;

        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);

        // ВАЖНО: embed по новому направлению, а не по старому transform.forward
        transform.position = cp.point - fwd * embedDepth;

        transform.SetParent(collision.transform, true);

        if (manager != null)
            manager.Register(this);
    }

    public void DetonateNow(int damage, float radius, LayerMask whatToDamage)
    {
        // FX (только Instantiate, чтобы ObjectPool НЕ падал)
        if (detonationFx != null)
        {
            GameObject fx = Instantiate(detonationFx, transform.position, Quaternion.identity);
            Destroy(fx, detonationFxLife);
        }

        if (manager != null)
            manager.ApplyExplosionDamage(transform.position, damage, radius, whatToDamage);

        if (manager != null)
            manager.Unregister(this);

        gameObject.SetActive(false);
    }
}
