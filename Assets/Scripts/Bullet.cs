using UnityEngine;

public class Bullet : MonoBehaviour
{
    private int bulletDamage;
    private float impactForce;

    private BoxCollider cd;
    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private TrailRenderer trailRenderer;

    [SerializeField] private GameObject bulletImpactFX;

    private Vector3 startPosition;
    private float flyDistance;
    private bool bulletDisabled;

    private LayerMask allyLayerMask;

    private Transform ownerRoot;

    protected virtual void Awake()
    {
        cd = GetComponent<BoxCollider>();
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        trailRenderer = GetComponent<TrailRenderer>();
    }
    public void BulletSetup(LayerMask allyLayerMask, int bulletDamage, float flyDistance = 100, float impactForce = 100, Transform owner = null)
    {
        this.allyLayerMask = allyLayerMask;
        this.impactForce = impactForce;
        this.bulletDamage = bulletDamage;

        ownerRoot = owner != null ? owner.root : null;

        bulletDisabled = false;
        cd.enabled = true;
        meshRenderer.enabled = true;

        trailRenderer.Clear();
        trailRenderer.time = .25f;
        startPosition = transform.position;
        this.flyDistance = flyDistance + .5f; // tip length
    }

    protected virtual void Update()
    {
        FadeTrailIfNeeded();
        DisableBulletIfNeeded();
        ReturnToPoolIfNeeded();
    }

    protected void ReturnToPoolIfNeeded()
    {
        if (trailRenderer.time < 0)
            ReturnBulletToPool();
    }

    protected void DisableBulletIfNeeded()
    {
        if (Vector3.Distance(startPosition, transform.position) > flyDistance && !bulletDisabled)
        {
            cd.enabled = false;
            meshRenderer.enabled = false;
            bulletDisabled = true;
        }
    }

    protected void FadeTrailIfNeeded()
    {
        if (Vector3.Distance(startPosition, transform.position) > flyDistance - 1.5f)
            trailRenderer.time -= 2 * Time.deltaTime;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (ownerRoot != null && collision.transform.root == ownerRoot)
        {
            ReturnBulletToPool();
            return;
        }

        if (FriendlyFare() == false)
        {
            // check if collision layer is in allyLayerMask
            if ((allyLayerMask.value & (1 << collision.gameObject.layer)) > 0)
            {
                ReturnBulletToPool(10);
                return;
            }
        }

        CreateImpactFx();
        ReturnBulletToPool();

        IDamagable damagable = collision.gameObject.GetComponent<IDamagable>();
        damagable?.TakeDamage(bulletDamage);

        ApplyBulletImpactToEnemy(collision);
    }

    private void ApplyBulletImpactToEnemy(Collision collision)
    {
        Enemy enemy = collision.gameObject.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            Vector3 force = rb.velocity.normalized * impactForce;
            Rigidbody hitRigidbody = collision.collider.attachedRigidbody;
            enemy.BulletImpact(force, collision.contacts[0].point, hitRigidbody);
        }
    }

    protected void ReturnBulletToPool(float delay = 0) => ObjectPool.instance.ReturnObject(gameObject, delay);

    protected void CreateImpactFx()
    {
        GameObject newFx = Instantiate(bulletImpactFX);
        newFx.transform.position = transform.position;

        Destroy(newFx, 1);
    }

    private bool FriendlyFare() => GameManager.instance.friendlyFire;
}
