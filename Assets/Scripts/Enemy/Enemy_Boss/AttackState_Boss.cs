using UnityEngine;

public class AttackState_Boss : EnemyState
{
    private Enemy_Boss enemy;
    public float lastTimeAttacked { get; private set; }
    public AttackState_Boss(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName) : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as Enemy_Boss;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.bossVisuals.EnableWeaponTrail(true);

        enemy.anim.SetFloat("AttackAnimIndex", Random.Range(0, 2)); // We have two attacks with index 0 and 1
        enemy.agent.isStopped = true;

        stateTimer = 1f;
    }

    public override void Update()
    {
        base.Update();

        if (stateTimer > 0)
            enemy.FaceTarget(enemy.player.position, 20);

        if (triggerCalled)
        {
            if (enemy.PlayerInAttackRange())
                stateMachine.ChangeState(enemy.idleState);
            else
                stateMachine.ChangeState(enemy.moveState);
        }

    }

    public override void Exit()
    {
        base.Exit();
        lastTimeAttacked = Time.time;
        enemy.bossVisuals.EnableWeaponTrail(false);
    }
}
