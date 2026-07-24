public interface IAttackStateProvider
{
    HitImpact CurrentAttackImpact { get; }

    void CancelAttackForHit();
}
