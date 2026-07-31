using UnityEngine;

public enum EnemyState
{
    Idle,
    Chase,
    Reposition,
    Attack,
    Hit,
    Dead
}
[RequireComponent(typeof(EnemyHealth), typeof(EnemyTarget), typeof(EnemyMovement))]
public class EnemyCore : MonoBehaviour
{
    public EnemyState CurrentState { get; private set; } = EnemyState.Idle;

    public bool IsDead => CurrentState == EnemyState.Dead;
    public bool CanMove => CurrentState == EnemyState.Idle || CurrentState == EnemyState.Chase || CurrentState == EnemyState.Reposition;
    public bool CanAttack => CanMove;

    public void EnterIdle()
    {
        if (!CanMove)
            return;

        CurrentState = EnemyState.Idle;
    }

    public void EnterChase()
    {
        if (!CanMove)
            return;

        CurrentState = EnemyState.Chase;
    }

    public void EnterReposition()
    {
        if (!CanMove)
            return;

        CurrentState = EnemyState.Reposition;
    }

    public bool TryEnterAttack()
    {
        if (!CanAttack)
            return false;

        CurrentState = EnemyState.Attack;
        return true;
    }

    public void ExitAttack()
    {
        if (CurrentState != EnemyState.Attack)
            return;

        CurrentState = EnemyState.Idle;
    }

    public void EnterHit()
    {
        if (CurrentState == EnemyState.Dead)
            return;

        CurrentState = EnemyState.Hit;
    }

    public void ExitHit()
    {
        if (CurrentState != EnemyState.Hit)
            return;

        CurrentState = EnemyState.Idle;
    }

    public void EnterDead()
    {
        CurrentState = EnemyState.Dead;
    }
}
