using UnityEngine;

public class Revolver_StuckBullet : MonoBehaviour
{
    [Header("Stick")]
    [SerializeField] private float embedDepth = 0.015f;
    [SerializeField] private float surfaceOffset = 0.003f;
    [SerializeField] private float sweepExtra = 0.25f;
    [SerializeField] private float autoReturnAfter = 0f;

    [Header("Stick filtering (Cars)")]
    [SerializeField] private bool useStickableMarkerOnCars = true;

    [SerializeField]
    private string[] carAllowedNames =
    {
        "jeep_body",
        "jeep_front_wheel_R",
        "jeep_front_wheel_L",
        "jeep_back_wheel_R",
        "jeep_back_wheel_L"
    };

    [Header("Detonation FX")]
    [SerializeField] private GameObject detonationFx;
    [SerializeField] private float detonationFxLife = 1f;
    [SerializeField] private float detonationFxScale = 0.7f;

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
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void OnEnable()
    {
        stuck = false;
        spawnTime = Time.time;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.detectCollisions = true;
        }

        if (col != null)
            col.isTrigger = true;

        transform.SetParent(null);
        lastFixedPos = transform.position;
    }

    public void Setup(Revolver_StickyBulletsManager newManager)
    {
        manager = newManager;

        if (manager != null)
            manager.Register(this);
    }

    private void OnDisable()
    {
        if (manager != null)
            manager.Unregister(this);
    }

    private void Update()
    {
        if (!stuck && autoReturnAfter > 0f && Time.time - spawnTime > autoReturnAfter)
            gameObject.SetActive(false);

        if (stuck && transform.parent == null)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.detectCollisions = false;
            }
            stuck = false;
        }
    }

    private void FixedUpdate()
    {
        if (stuck)
            return;

        Vector3 currentPos = transform.position;
        Vector3 delta = currentPos - lastFixedPos;

        float dist = delta.magnitude;
        if (dist > 0.0001f)
        {
            Vector3 dir = delta / dist;
            float castDist = dist + Mathf.Max(0f, sweepExtra);

            RaycastHit[] hits = Physics.RaycastAll(lastFixedPos, dir, castDist, ~0, QueryTriggerInteraction.Collide);

            if (hits != null && hits.Length > 0)
            {
                int bestIndex = -1;
                float bestDist = float.MaxValue;

                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null) continue;
                    if (col != null && hits[i].collider == col) continue;

                    if (hits[i].collider.GetComponentInParent<Flamethrow_DamageArea>() != null) continue;

                    Transform stickParent = ResolveStickParent(hits[i].collider.transform, hits[i].collider);
                    if (stickParent == null)
                        continue;

                    bool isCar = hits[i].collider.GetComponentInParent<Car_Controller>() != null;
                    if (!isCar && hits[i].collider.isTrigger)
                        continue;

                    if (hits[i].distance < bestDist)
                    {
                        bestDist = hits[i].distance;
                        bestIndex = i;
                    }
                }

                if (bestIndex != -1)
                {
                    Transform parent = ResolveStickParent(hits[bestIndex].collider.transform, hits[bestIndex].collider);
                    StickAt(hits[bestIndex], parent);
                    lastFixedPos = currentPos;
                    return;
                }
            }
        }

        lastFixedPos = currentPos;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (stuck) return;
        if (other == null) return;
        if (col != null && other == col) return;

        if (other.GetComponentInParent<Flamethrow_DamageArea>() != null) return;

        bool isCar = other.GetComponentInParent<Car_Controller>() != null;
        if (!isCar && other.isTrigger) return;

        Transform parent = ResolveStickParent(other.transform, other);
        if (parent == null)
            return;

        Vector3 dir = rb != null && rb.velocity.sqrMagnitude > 0.0001f ? rb.velocity.normalized : transform.forward;
        RaycastHit hit;

        if (Physics.Raycast(transform.position - dir * 0.2f, dir, out hit, 0.7f, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider == other || hit.collider.transform.IsChildOf(other.transform) || other.transform.IsChildOf(hit.collider.transform))
                StickAt(hit, parent);
        }
    }

    private Transform ResolveStickParent(Transform hitTr, Collider hitCol)
    {
        Car_Controller car = hitTr.GetComponentInParent<Car_Controller>();
        if (car != null)
        {
            if (useStickableMarkerOnCars)
            {
                Revolver_StickableSurface marker = hitTr.GetComponentInParent<Revolver_StickableSurface>();

                if (marker != null)
                    return marker.transform;

                if (hitCol != null && hitCol.isTrigger)
                    return null;
            }

            Transform t = hitTr;
            while (t != null)
            {
                for (int i = 0; i < carAllowedNames.Length; i++)
                {
                    if (t.name == carAllowedNames[i])
                        return t;
                }

                if (t == car.transform)
                    break;

                t = t.parent;
            }

            return null;
        }

        return hitTr;
    }

    private void StickAt(RaycastHit hit, Transform parent)
    {
        if (parent == null || hit.collider == null)
            return;

        stuck = true;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.detectCollisions = false;
        }

        Vector3 normal = hit.normal;
        if (normal.sqrMagnitude < 0.0001f)
            normal = Vector3.up;

        Vector3 fwd = -normal;
        if (fwd.sqrMagnitude < 0.0001f)
            fwd = transform.forward;

        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);

        float depth = Mathf.Max(0f, embedDepth);
        float outOffset = Mathf.Max(0f, surfaceOffset);

        transform.position = hit.point + normal * outOffset - fwd * depth;
        transform.SetParent(parent, true);

        if (manager != null)
            manager.Register(this);
    }

    public void DetonateNow(int damage, float radius, LayerMask whatToDamage)
    {
        if (detonationFx != null)
        {
            GameObject fx = Instantiate(detonationFx, transform.position, Quaternion.identity);
            fx.transform.localScale = fx.transform.localScale * Mathf.Max(0.01f, detonationFxScale);
            Destroy(fx, detonationFxLife);
        }

        if (manager != null)
            manager.ApplyExplosionDamage(transform.position, damage, radius, whatToDamage);

        if (manager != null)
            manager.Unregister(this);

        gameObject.SetActive(false);
    }
}
