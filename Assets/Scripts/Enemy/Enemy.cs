using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum EnemyType { Melee, Range, Boss, Random }

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour
{
    public EnemyType enemyType;
    public LayerMask whatIsAlly;
    public LayerMask whatIsPlayer;

    [Header("Idle data")]
    public float idleTime;
    public float aggresionRange;

    [Header("Move data")]
    public float walkSpeed = 1.5f;
    public float runSpeed = 3;
    public float turnSpeed;
    private bool manualMovement;
    private bool manualRotation;

    [SerializeField] private Transform[] patrolPoints;
    private Vector3[] patrolPointsPosition;
    private int currentPatrolIndex;

    public bool inBattleMode { get; private set; }
    protected bool isMeleeAttackReady;

    public Transform player { get; private set; }
    public Animator anim { get; private set; }
    public NavMeshAgent agent { get; private set; }
    public EnemyStateMachine stateMachine { get; private set; }
    public Enemy_Visuals visuals { get; private set; }

    public Enemy_Health health { get; private set; }
    public Ragdoll ragdoll { get; private set; }

    public Enemy_DropController dropController { get; private set; }
    public AudioManager audioManager { get; private set; }

    public bool IsDead { get; private set; }

    [Header("Melee check (stable hit)")]
    [SerializeField] private int meleeNonAllocBufferSize = 16;

    private Collider[] meleeHitsBuffer;
    private bool meleeAlreadyHitThisSwing;

    protected virtual void Awake()
    {
        stateMachine = new EnemyStateMachine();

        health = GetComponent<Enemy_Health>();
        ragdoll = GetComponent<Ragdoll>();
        visuals = GetComponent<Enemy_Visuals>();
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        dropController = GetComponent<Enemy_DropController>();
        player = GameObject.Find("Player").GetComponent<Transform>();

        meleeHitsBuffer = new Collider[Mathf.Max(4, meleeNonAllocBufferSize)];
    }

    protected virtual void Start()
    {
        InitializePatrolPoints();
        audioManager = AudioManager.instance;
    }

    protected virtual void Update()
    {
        if (ShouldEnterBattleMode())
            EnterBattleMode();
    }

    protected virtual void InitializePerk() { }

    public virtual void MakeEnemyVIP()
    {
        int additionalHealth = Mathf.RoundToInt(health.currentHealth * 1.5f);
        health.currentHealth += additionalHealth;
        transform.localScale = transform.localScale * 1.15f;
    }

    protected bool ShouldEnterBattleMode()
    {
        if (IsDead)
            return false;

        if (IsPlayerInAgrresionRange() && !inBattleMode)
        {
            EnterBattleMode();
            return true;
        }

        return false;
    }

    public virtual void EnterBattleMode()
    {
        if (IsDead)
            return;

        inBattleMode = true;
    }

    public virtual void GetHit(int damage)
    {
        if (IsDead)
            return;

        EnterBattleMode();
        health.ReduceHealth(damage);

        if (health.ShouldDie())
            Die();
    }

    public virtual void Die()
    {
        if (IsDead)
            return;

        IsDead = true;

        dropController.DropItems();

        if (anim != null)
            anim.enabled = false;

        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (ragdoll != null)
            ragdoll.RagdollActive(true);

        MissionObject_HuntTarget huntTarget = GetComponent<MissionObject_HuntTarget>();
        huntTarget?.InvokeOnTargetKilled();
    }

    // ВАЖНО: Эту функцию надо вызывать КАЖДЫЙ КАДР во время окна удара (см. Enemy_Melee.Update)
    public virtual void MeleeAttackCheck(Transform[] damagePoints, float attackCheckRadius, GameObject fx, int damage)
    {
        if (IsDead)
            return;

        if (!isMeleeAttackReady)
            return;

        if (meleeAlreadyHitThisSwing)
            return;

        if (damagePoints == null || damagePoints.Length == 0)
            return;

        for (int p = 0; p < damagePoints.Length; p++)
        {
            Transform attackPoint = damagePoints[p];
            if (attackPoint == null)
                continue;

            int count = Physics.OverlapSphereNonAlloc(
                attackPoint.position,
                attackCheckRadius,
                meleeHitsBuffer,
                whatIsPlayer
            );

            for (int i = 0; i < count; i++)
            {
                Collider col = meleeHitsBuffer[i];
                if (col == null) continue;

                IDamagable damagable = col.GetComponent<IDamagable>();
                if (damagable == null)
                    damagable = col.GetComponentInParent<IDamagable>();

                if (damagable != null)
                {
                    damagable.TakeDamage(damage);

                    meleeAlreadyHitThisSwing = true;
                    isMeleeAttackReady = false;

                    if (ObjectPool.instance != null && fx != null)
                    {
                        GameObject newAttackFx = ObjectPool.instance.GetObject(fx, attackPoint);
                        ObjectPool.instance.ReturnObject(newAttackFx, 1);
                    }

                    return;
                }
            }
        }
    }

    // Эту функцию дергают Animation Events: BeginMeleeAttackCheck / FinishMeleeAttackCheck
    public void EnableMeleeAttackCheck(bool enable)
    {
        isMeleeAttackReady = enable;

        if (enable)
            meleeAlreadyHitThisSwing = false;
    }

    public virtual void BulletImpact(Vector3 force, Vector3 hitPoint, Rigidbody rb)
    {
        if (health.ShouldDie())
            StartCoroutine(DeathImpactCourutine(force, hitPoint, rb));
    }

    private IEnumerator DeathImpactCourutine(Vector3 force, Vector3 hitPoint, Rigidbody rb)
    {
        yield return new WaitForSeconds(.1f);
        rb.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);
    }

    public void FaceTarget(Vector3 target, float turnSpeed = 0)
    {
        if (IsDead)
            return;

        Vector3 dir = target - transform.position;
        if (dir.sqrMagnitude < 0.000001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(dir);
        Vector3 currentEulerAngels = transform.rotation.eulerAngles;

        if (turnSpeed == 0)
            turnSpeed = this.turnSpeed;

        float yRotation =
            Mathf.LerpAngle(currentEulerAngels.y, targetRotation.eulerAngles.y, turnSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(currentEulerAngels.x, yRotation, currentEulerAngels.z);
    }

    #region Animation events
    public void ActivateManualMovement(bool manualMovement) => this.manualMovement = manualMovement;
    public bool ManualMovementActive() => manualMovement;

    public void ActivateManualRotation(bool manualRotation) => this.manualRotation = manualRotation;
    public bool ManualRotationActive() => manualRotation;

    public void AnimationTrigger() => stateMachine.currentState.AnimationTrigger();

    public virtual void AbilityTrigger()
    {
        stateMachine.currentState.AbilityTrigger();
    }
    #endregion

    #region Patrol logic
    public Vector3 GetPatrolDestination()
    {
        Vector3 destination = patrolPointsPosition[currentPatrolIndex];

        currentPatrolIndex++;

        if (currentPatrolIndex >= patrolPoints.Length)
            currentPatrolIndex = 0;

        return destination;
    }

    private void InitializePatrolPoints()
    {
        patrolPointsPosition = new Vector3[patrolPoints.Length];

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            patrolPointsPosition[i] = patrolPoints[i].position;
            patrolPoints[i].gameObject.SetActive(false);
        }
    }
    #endregion

    public bool IsPlayerInAgrresionRange() => Vector3.Distance(transform.position, player.position) < aggresionRange;

    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, aggresionRange);
    }
}
