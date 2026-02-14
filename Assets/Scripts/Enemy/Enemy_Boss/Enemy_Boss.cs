using System.Collections.Generic;
using UnityEngine;

public enum BossWeaponType { Flamethrower, Hummer }

public class Enemy_Boss : Enemy
{
    [Header("Boss details")]
    public BossWeaponType bossWeaponType;
    public float actionCooldown = 10;
    public float attackRange;

    [Header("Ability")]
    public float minAbilityDistance;
    public float abilityCooldown;
    private float lastTimeUsedAbility;

    [Header("Flamethrower")]
    public int flameDamage;
    public float flameDamageCooldown;
    public ParticleSystem flamethrower;
    public float flamethrowDuration;
    public bool flamethrowActive { get; private set; }

    [Header("Hummer")]
    public int hummerActiveDamage;
    public GameObject activationPrefab;
    [SerializeField] private float hummerCheckRadius;

    [Header("Jump attack")]
    public int jumpAttackDamage;
    public float jumpAttackCooldown = 10;
    private float lastTimeJumped;
    public float travelTimeToTarget = 1;
    public float minJumpDistanceRequired;
    [Space]
    public float impactRadius = 2.5f;
    public float impactPower = 5;
    public Transform impactPoint;
    [SerializeField] private float upforceMultiplier = 10;
    [Space]
    [SerializeField] private LayerMask whatToIngore;

    [Header("Attack")]
    [SerializeField] private int meleeAttackDamage;
    [SerializeField] private Transform[] damagePoints;
    [SerializeField] private float attackCheckRadius;
    [SerializeField] private GameObject meleeAttackFx;

    public IdleState_Boss idleState { get; private set; }
    public MoveState_Boss moveState { get; private set; }
    public AttackState_Boss attackState { get; private set; }
    public JumpAttackState_Boss jumpAttackState { get; private set; }
    public AbilityState_Boss abilityState { get; private set; }
    public DeadState_Boss deadState { get; private set; }

    public Enemy_BossVisuals bossVisuals { get; private set; }

    public bool IsJumpAttackActive { get; private set; }
    public void SetJumpAttackActive(bool active) => IsJumpAttackActive = active;

    protected override void Awake()
    {
        base.Awake();

        bossVisuals = GetComponent<Enemy_BossVisuals>();

        idleState = new IdleState_Boss(this, stateMachine, "Idle");
        moveState = new MoveState_Boss(this, stateMachine, "Move");
        attackState = new AttackState_Boss(this, stateMachine, "Attack");
        jumpAttackState = new JumpAttackState_Boss(this, stateMachine, "JumpAttack");
        abilityState = new AbilityState_Boss(this, stateMachine, "Ability");
        deadState = new DeadState_Boss(this, stateMachine, "Idle");
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    protected override void Update()
    {
        base.Update();

        stateMachine.currentState.Update();

        if (ShouldEnterBattleMode())
            EnterBattleMode();

        MeleeAttackCheck(damagePoints, attackCheckRadius, meleeAttackFx, meleeAttackDamage);

        if (!CanUseAbilities && flamethrowActive)
            ActivateFlamethrower(false);
    }

    public override void Die()
    {
        base.Die();

        if (stateMachine.currentState != deadState)
            stateMachine.ChangeState(deadState);
    }

    public override void EnterBattleMode()
    {
        if (inBattleMode)
            return;

        base.EnterBattleMode();
        stateMachine.ChangeState(moveState);
    }

    public void ActivateFlamethrower(bool activate)
    {
        if (activate && !CanUseAbilities)
            return;

        flamethrowActive = activate;

        if (!activate)
        {
            if (flamethrower != null)
                flamethrower.Stop();

            if (anim != null)
                anim.SetTrigger("StopFlamethrower");

            return;
        }

        if (flamethrower == null)
            return;

        var mainModule = flamethrower.main;
        var extraModule = flamethrower.transform.GetChild(0).GetComponent<ParticleSystem>().main;

        mainModule.duration = flamethrowDuration;
        extraModule.duration = flamethrowDuration;

        flamethrower.Clear();
        flamethrower.Play();
    }

    public void ActivateHummer()
    {
        if (!CanUseAbilities)
            return;

        GameObject newActivation = ObjectPool.instance.GetObject(activationPrefab, impactPoint);
        ObjectPool.instance.ReturnObject(newActivation, 1);

        MassDamage(damagePoints[0].position, hummerCheckRadius, hummerActiveDamage);
    }

    public bool CanDoAbility()
    {
        if (!CanUseAbilities)
            return false;

        bool playerWithinDistance = Vector3.Distance(transform.position, player.position) < minAbilityDistance;
        if (!playerWithinDistance)
            return false;

        return Time.time > lastTimeUsedAbility + abilityCooldown;
    }

    public void SetAbilityOnCooldown() => lastTimeUsedAbility = Time.time;

    public void JumpImpact()
    {
        Transform ip = impactPoint != null ? impactPoint : transform;
        MassDamage(ip.position, impactRadius, jumpAttackDamage);
    }

    private void MassDamage(Vector3 impactPoint, float impactRadius, int damage)
    {
        HashSet<GameObject> uniqueEntities = new HashSet<GameObject>();
        Collider[] colliders = Physics.OverlapSphere(impactPoint, impactRadius, ~whatIsAlly);

        foreach (Collider hit in colliders)
        {
            IDamagable damagable = hit.GetComponent<IDamagable>();
            if (damagable != null)
            {
                GameObject rootEntity = hit.transform.root.gameObject;
                if (!uniqueEntities.Add(rootEntity))
                    continue;

                damagable.TakeDamage(damage);
            }

            ApplyPhysicalForceTo(impactPoint, impactRadius, hit);
        }
    }

    private void ApplyPhysicalForceTo(Vector3 impactPoint, float impactRadius, Collider hit)
    {
        Rigidbody rb = hit.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddExplosionForce(impactPower, impactPoint, impactRadius, upforceMultiplier, ForceMode.Impulse);
    }

    public bool CanDoJumpAttack()
    {
        if (!CanUseAbilities)
            return false;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < minJumpDistanceRequired)
            return false;

        return Time.time > lastTimeJumped + jumpAttackCooldown && IsPlayerInClearSight();
    }

    public void SetJumpAttackOnCooldown() => lastTimeJumped = Time.time;

    public bool IsPlayerInClearSight()
    {
        Vector3 myPos = transform.position + new Vector3(0, 1.5f, 0);
        Vector3 playerPos = player.position + Vector3.up;
        Vector3 directionToPlayer = (playerPos - myPos).normalized;

        if (Physics.Raycast(myPos, directionToPlayer, out RaycastHit hit, 100, ~whatToIngore))
        {
            if (hit.transform.root == player.root)
                return true;
        }

        return false;
    }

    public bool PlayerInAttackRange() => Vector3.Distance(transform.position, player.position) < attackRange;

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minAbilityDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, impactRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minJumpDistanceRequired);

        if (damagePoints != null && damagePoints.Length > 0)
        {
            foreach (var damagePoint in damagePoints)
                Gizmos.DrawWireSphere(damagePoint.position, attackCheckRadius);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(damagePoints[0].position, hummerCheckRadius);
        }
    }
}
