using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState_Range : EnemyState
{
    private Enemy_Range enemy;

    public IdleState_Range(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as Enemy_Range;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.anim.SetFloat("IdleAnimIndex", Random.Range(0, 3)); // we have 3 animtions with index 0 to 2

        enemy.visuals.EnableIK(true, false);

        if (enemy.weaponType == Enemy_RangeWeaponType.Pistol)
            enemy.visuals.EnableIK(false, false);

        stateTimer = enemy.idleTime;
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer < 0)
            stateMachine.ChangeState(enemy.moveState);
    }
}
