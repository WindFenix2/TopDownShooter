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

    private Vector3 lastFixedPos;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        if (rb != null)
        {
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
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

        // IMPORTANT: trigger so it doesn't push cars/objects
        if (col != null)
            col.isTrigger = true;

        transform.SetParent(null);
        lastFixedPos = transform.position;
    }

    private void FixedUpdate()
    {
        // store previous physics-step position for raycast
        lastFixedPos = transform.position;
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

    private void OnTriggerEnter(Collider other)
    {
        if (stuck) return;
        if (other == null) return;

        StickToTrigger(other);
    }

    private void StickToTrigger(Collider other)
    {
        stuck = true;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 dir = Vector3.zero;

        if (rb != null && rb.velocity.sqrMagnitude > 0.0001f)
            dir = rb.velocity.normalized;
        else
            dir = (transform.position - lastFixedPos).normalized;

        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;

        // Try to get a proper normal via raycast between last and current positions
        Vector3 origin = lastFixedPos;
        float dist = Vector3.Distance(lastFixedPos, transform.position) + 0.25f;

        Vector3 normal = -dir;

        RaycastHit hit;
        if (Physics.Raycast(origin, dir, out hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == other || hit.collider.transform.IsChildOf(other.transform) || other.transform.IsChildOf(hit.collider.transform))
            {
                hitPoint = hit.point;
                normal = hit.normal;
            }
        }
        else
        {
            // fallback normal from closest point
            Vector3 n = (transform.position - hitPoint);
            if (n.sqrMagnitude > 0.0001f)
                normal = n.normalized;
        }

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
        transform.position = hitPoint - fwd * embedDepth;

        transform.SetParent(other.transform, true);

        if (manager != null)
            manager.Register(this);
    }

    public void DetonateNow(int damage, float radius, LayerMask whatToDamage)
    {
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
