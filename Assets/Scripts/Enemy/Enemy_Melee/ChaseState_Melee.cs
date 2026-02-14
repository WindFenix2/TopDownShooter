using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaseState_Melee : EnemyState
{
    private Enemy_Melee enemy;
    private float lastTimeUpdatedDistanation;

    public ChaseState_Melee(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as Enemy_Melee;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.agent.isStopped = false;
        enemy.agent.speed = enemy.runSpeed * enemy.SpeedMultiplier;

        enemy.agent.stoppingDistance = Mathf.Max(0.6f, enemy.attackData.attackRange * 0.9f);
    }

    public override void Update()
    {
        base.Update();

        enemy.agent.speed = enemy.runSpeed * enemy.SpeedMultiplier;

        float distToPlayer = Vector3.Distance(enemy.transform.position, enemy.player.position);
        float stopDist = Mathf.Max(enemy.agent.stoppingDistance, 0.6f);

        if (!enemy.CanAttack)
        {
            if (distToPlayer <= stopDist + 0.15f)
            {
                stateMachine.ChangeState(enemy.waitEMIState);
                return;
            }

            enemy.agent.isStopped = false;

            if (CanUpdateDestination())
                enemy.agent.SetDestination(enemy.player.position);

            enemy.FaceTarget(GetNextPathPoint());
            return;
        }

        if (enemy.PlayerInAttackRange() && enemy.CanAttack)
        {
            stateMachine.ChangeState(enemy.attackState);
            return;
        }

        enemy.FaceTarget(GetNextPathPoint());

        if (CanUpdateDestination())
            enemy.agent.destination = enemy.player.transform.position;
    }

    private bool CanUpdateDestination()
    {
        if (Time.time > lastTimeUpdatedDistanation + .25f)
        {
            lastTimeUpdatedDistanation = Time.time;
            return true;
        }

        return false;
    }
}
