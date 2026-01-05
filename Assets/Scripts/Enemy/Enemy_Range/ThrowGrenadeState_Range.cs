using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowGrenadeState_Range : EnemyState
{
    private Enemy_Range enemy;
    public bool finishedThrowingGrenade { get; private set; } = true;


    public ThrowGrenadeState_Range(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as Enemy_Range;
    }

    public override void Enter()
    {
        base.Enter();

        finishedThrowingGrenade = false;

        enemy.visuals.EnableWeaponModel(false);
        enemy.visuals.EnableIK(false, false);
        enemy.visuals.EnableSeconoderyWeaponModel(true);
        enemy.visuals.EnableGrenadeModel(true);
    }


    public override void Update()
    {
        base.Update();

        Vector3 playerPos = enemy.player.position + Vector3.up;

        enemy.FaceTarget(playerPos);
        enemy.aim.position = playerPos;

        if (triggerCalled)
            stateMachine.ChangeState(enemy.battleState);
    }

    public override void AbilityTrigger()
    {
        base.AbilityTrigger();
        finishedThrowingGrenade = true;
        enemy.ThrowGrenade();
    }
}
