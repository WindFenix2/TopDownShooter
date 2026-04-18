using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct AttackData_EnemyMelee
{
    public int attackDamage;
    public string attackName;
    public float attackRange;
    public float moveSpeed;
    public float attackIndex;
    [Range(1, 2)]
    public float animationSpeed;
    public AttackType_Melee attackType;
}

public enum AttackType_Melee { Close, Charge }
public enum EnemyMelee_Type { Regular, Shield, Dodge, AxeThrow }

public class Enemy_Melee : Enemy
{
    public Enemy_MeleeSFX meleeSFX { get; private set; }

    #region States
    public IdleState_Melee idleState { get; private set; }
    public MoveState_Melee moveState { get; private set; }
    public RecoveryState_Melee recoveryState { get; private set; }
    public ChaseState_Melee chaseState { get; private set; }
    public AttackState_Melee attackState { get; private set; }
    public DeadState_Melee deadState { get; private set; }
    public AbilityState_Melee abilityState { get; private set; }

    public WaitEMIState_Melee waitEMIState { get; private set; }
    #endregion

    [Header("Enemy Settings")]
    public EnemyMelee_Type meleeType;
    public Enemy_MeleeWeaponType weaponType;

    [Header("Shield")]
    public int shieldDurability;
    public Transform shieldTransform;

    [Header("Dodge")]
    public float dodgeCooldown;
    private float lastTimeDodge = -10;

    [Header("Axe throw ability")]
    public int axeDamage;
    public GameObject axePrefab;
    public float axeFlySpeed;
    public float axeAimTimer;
    public float axeThrowCooldown;
    private float lastTimeAxeThrown;
    public Transform axeStartPoint;

    [Header("Attack Data")]
    public AttackData_EnemyMelee attackData;
    public List<AttackData_EnemyMelee> attackList;

    private Enemy_WeaponModel currentWeapon;

    [Space]
    [SerializeField] private GameObject meleeAttackFx;

    protected override void Awake()
    {
        base.Awake();

        idleState = new IdleState_Melee(this, stateMachine, "Idle");
        moveState = new MoveState_Melee(this, stateMachine, "Move");
        recoveryState = new RecoveryState_Melee(this, stateMachine, "Recovery");
        chaseState = new ChaseState_Melee(this, stateMachine, "Chase");
        attackState = new AttackState_Melee(this, stateMachine, "Attack");
        deadState = new DeadState_Melee(this, stateMachine, "Idle"); // placeholder (ragdoll)
        abilityState = new AbilityState_Melee(this, stateMachine, "AxeThrow");

        waitEMIState = new WaitEMIState_Melee(this, stateMachine, "Idle");

        meleeSFX = GetComponent<Enemy_MeleeSFX>();
    }

    protected override void Start()
    {
        base.Start();

        stateMachine.Initialize(idleState);
        ResetCooldown();

        InitializePerk();
        visuals.SetupLook();
        UpdateAttackData();
    }

    protected override void Update()
    {
        base.Update();

        if (IsDead)
            return;

        stateMachine.currentState.Update();

        if (currentWeapon != null)
            MeleeAttackCheck(currentWeapon.damagePoints, currentWeapon.attackRadius, meleeAttackFx, attackData.attackDamage);
    }

    public override void EnterBattleMode()
    {
        if (inBattleMode)
            return;

        base.EnterBattleMode();
        stateMachine.ChangeState(recoveryState);
    }

    public override void AbilityTrigger()
    {
        base.AbilityTrigger();

        walkSpeed = walkSpeed * .6f;
        visuals.EnableWeaponModel(false);
    }

    public void UpdateAttackData()
    {
        if (visuals == null || visuals.currentWeaponModel == null)
            return;

        currentWeapon = visuals.currentWeaponModel.GetComponent<Enemy_WeaponModel>();

        if (currentWeapon != null && currentWeapon.weaponData != null)
        {
            attackList = new List<AttackData_EnemyMelee>(currentWeapon.weaponData.attackData);
            turnSpeed = currentWeapon.weaponData.turnSpeed;
        }
    }

    protected override void InitializePerk()
    {
        if (meleeType == EnemyMelee_Type.AxeThrow)
            weaponType = Enemy_MeleeWeaponType.Throw;

        if (meleeType == EnemyMelee_Type.Shield)
        {
            anim.SetFloat("ChaseIndex", 1);
            if (shieldTransform != null) shieldTransform.gameObject.SetActive(true);
            weaponType = Enemy_MeleeWeaponType.OneHand;
        }

        if (meleeType == EnemyMelee_Type.Dodge)
            weaponType = Enemy_MeleeWeaponType.Unarmed;
    }

    public override void Die()
    {
        base.Die();

        if (stateMachine.currentState != deadState)
            stateMachine.ChangeState(deadState);
    }

    public void ActivateDodgeRoll()
    {
        if (!CanUseAbilities)
            return;

        if (meleeType != EnemyMelee_Type.Dodge)
            return;

        if (stateMachine.currentState != chaseState)
            return;

        if (Vector3.Distance(transform.position, player.position) < 2f)
            return;

        float dodgeAnimationDuration = GetAnimationClipDuration("Dodge roll");

        if (Time.time > dodgeCooldown + dodgeAnimationDuration + lastTimeDodge)
        {
            lastTimeDodge = Time.time;
            anim.SetTrigger("Dodge");
        }
    }

    public void ThrowAxe()
    {
        if (!CanUseAbilities)
            return;

        GameObject newAxe = ObjectPool.instance.GetObject(axePrefab, axeStartPoint);
        newAxe.GetComponent<Enemy_Axe>().AxeSetup(axeFlySpeed, player, axeAimTimer, axeDamage);
    }

    public bool CanThrowAxe()
    {
        if (!CanUseAbilities)
            return false;

        if (meleeType != EnemyMelee_Type.AxeThrow)
            return false;

        if (Time.time > axeThrowCooldown + lastTimeAxeThrown)
        {
            lastTimeAxeThrown = Time.time;
            return true;
        }

        return false;
    }

    private void ResetCooldown()
    {
        lastTimeDodge -= dodgeCooldown;
        lastTimeAxeThrown -= axeThrowCooldown;
    }

    private float GetAnimationClipDuration(string clipName)
    {
        AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;

        foreach (AnimationClip clip in clips)
        {
            if (clip.name == clipName)
                return clip.length;
        }

        Debug.Log(clipName + " animation not found!");
        return 0;
    }

    public bool PlayerInAttackRange()
    {
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        if (distToPlayer < attackData.attackRange)
            return true;

        Car_Controller car = player.GetComponentInParent<Car_Controller>();
        if (car != null)
        {
            Collider carCol = car.GetComponent<Collider>();
            if (carCol != null)
            {
                Vector3 closest = carCol.ClosestPoint(transform.position);
                float distToCar = Vector3.Distance(transform.position, closest);
                return distToCar < attackData.attackRange;
            }

            float distToCarCenter = Vector3.Distance(transform.position, car.transform.position);
            return distToCarCenter < attackData.attackRange + 1f;
        }

        return false;
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
    }
}
