using UnityEngine;

public class WaitEMIState_Melee : EnemyState
{
    private Enemy_Melee enemy;

    private const float EXTRA_DISTANCE = 0.7f;

    public WaitEMIState_Melee(Enemy enemyBase, EnemyStateMachine stateMachine, string animBoolName)
        : base(enemyBase, stateMachine, animBoolName)
    {
        enemy = enemyBase as Enemy_Melee;
    }

    public override void Enter()
    {
        base.Enter();

        if (enemy.agent != null)
        {
            enemy.agent.isStopped = true;
            enemy.agent.velocity = Vector3.zero;
            enemy.agent.ResetPath();
        }
    }

    public override void Update()
    {
        base.Update();

        if (enemy.CanAttack)
        {
            stateMachine.ChangeState(enemy.chaseState);
            return;
        }

        if (enemy.player == null)
            return;

        float distToPlayer = Vector3.Distance(enemy.transform.position, enemy.player.position);
        float stopDist = Mathf.Max(enemy.attackData.attackRange * 0.9f, 0.6f);

        if (distToPlayer > stopDist + EXTRA_DISTANCE)
        {
            stateMachine.ChangeState(enemy.chaseState);
            return;
        }

        enemy.FaceTarget(enemy.player.position);
    }
}
