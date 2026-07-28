public static class HitReactionResolver
{
    public static HitImpact Resolve(HitImpact incomingImpact, HitImpact currentAttackImpact)
    {
        if (incomingImpact == HitImpact.None || incomingImpact == HitImpact.Invincible)
            return HitImpact.None;

        if (currentAttackImpact == HitImpact.Invincible)
            return HitImpact.None;

        if (GetPriority(incomingImpact) < GetPriority(currentAttackImpact))
            return HitImpact.None;

        return incomingImpact;
    }

    private static int GetPriority(HitImpact impact)
    {
        return impact switch
        {
            HitImpact.Light => 1,
            HitImpact.Middle => 2,
            HitImpact.Heavy => 3,
            _ => 0
        };
    }
}
