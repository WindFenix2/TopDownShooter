using UnityEngine;

public class BulletSoftHoming : MonoBehaviour
{
    private Rigidbody rb;

    private Transform target;
    private Vector3 targetLocalOffset;

    private float timer;
    private float turnSpeedDeg;

    private bool active;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        enabled = false;
    }

    public void EnableHoming(Transform target, float homingTime, float turnSpeedDeg)
    {
        EnableHoming(target, Vector3.zero, homingTime, turnSpeedDeg);
    }

    public void EnableHoming(Transform target, Vector3 targetLocalOffset, float homingTime, float turnSpeedDeg)
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        this.target = target;
        this.targetLocalOffset = targetLocalOffset;

        this.timer = homingTime;
        this.turnSpeedDeg = turnSpeedDeg;

        active = target != null && homingTime > 0f && turnSpeedDeg > 0f;
        enabled = active;
    }

    public void DisableHoming()
    {
        target = null;
        targetLocalOffset = Vector3.zero;

        timer = 0f;
        turnSpeedDeg = 0f;
        active = false;
        enabled = false;
    }

    private void Update()
    {
        if (!active || rb == null)
        {
            DisableHoming();
            return;
        }

        if (target == null)
        {
            DisableHoming();
            return;
        }

        Enemy enemy = target.GetComponentInParent<Enemy>();
        if (enemy != null && enemy.IsDead)
        {
            DisableHoming();
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            DisableHoming();
            return;
        }

        Vector3 vel = rb.velocity;
        float speed = vel.magnitude;

        if (speed <= 0.01f)
            return;

        Vector3 targetWorldPos = target.TransformPoint(targetLocalOffset);

        Vector3 desiredDir = (targetWorldPos - rb.position).normalized;
        Vector3 currentDir = vel.normalized;

        float maxRadians = turnSpeedDeg * Mathf.Deg2Rad * Time.deltaTime;
        Vector3 newDir = Vector3.RotateTowards(currentDir, desiredDir, maxRadians, 0f).normalized;

        rb.velocity = newDir * speed;
        rb.rotation = Quaternion.LookRotation(newDir);
    }
}