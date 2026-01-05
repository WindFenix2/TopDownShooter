using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState_Melee : EnemyState
{
    private Enemy_Melee enemy;
    private Vector3 attackDirection;
    private float attackMoveSpeed;

    private const float MAX_ATTACK_DISTANCE = 50f;

    public AttackState_Melee(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as Enemy_Melee;
    }

    public override void Enter()
    {
        base.Enter();
        enemy.UpdateAttackData();
        enemy.visuals.EnableWeaponModel(true);
        enemy.visuals.EnableWeaponTrail(true);

        attackMoveSpeed = enemy.attackData.moveSpeed;
        enemy.anim.SetFloat("AttackAnimationSpeed", enemy.attackData.animationSpeed);
        enemy.anim.SetFloat("AttackIndex", enemy.attackData.attackIndex);
        enemy.anim.SetFloat("SlashAttackIndex", Random.Range(0, 6)); // we have 6 attacks with index from 0 to 5


        enemy.agent.isStopped = true;
        enemy.agent.velocity = Vector3.zero;

        attackDirection = enemy.transform.position + (enemy.transform.forward * MAX_ATTACK_DISTANCE);
    }

    public override void Exit()
    {
        base.Exit();
        SetupNextAttack();

        enemy.visuals.EnableWeaponTrail(false);
    }

    private void SetupNextAttack()
    {
        int recoveryIndex = PlayerClose() ? 1 : 0;

        enemy.anim.SetFloat("RecoveryIndex", recoveryIndex);
        enemy.attackData = UpdatedAttackData();
    }

    public override void Update()
    {
        base.Update();

        if (enemy.ManualRotationActive())
        {
            enemy.FaceTarget(enemy.player.position);
            attackDirection = enemy.transform.position + (enemy.transform.forward * MAX_ATTACK_DISTANCE);
        }
        

        if (enemy.ManualMovementActive())
        {
            enemy.transform.position = 
                Vector3.MoveTowards(enemy.transform.position, attackDirection, attackMoveSpeed * Time.deltaTime);
        }

        if (triggerCalled)
        {
            if (enemy.CanThrowAxe())
                stateMachine.ChangeState(enemy.abilityState);
            else if (enemy.PlayerInAttackRange())
                stateMachine.ChangeState(enemy.recoveryState);
            else
                stateMachine.ChangeState(enemy.chaseState);
        }
    }

    private bool PlayerClose() => Vector3.Distance(enemy.transform.position, enemy.player.position) <= 1;

    private AttackData_EnemyMelee UpdatedAttackData()
    {
        List<AttackData_EnemyMelee> validAttacks = new List<AttackData_EnemyMelee>(enemy.attackList);

        if (PlayerClose())
            validAttacks.RemoveAll(parameter => parameter.attackType == AttackType_Melee.Charge);

        int random = Random.Range(0,validAttacks.Count);
        return validAttacks[random];
    }
}
