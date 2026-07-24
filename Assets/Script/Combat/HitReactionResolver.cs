public static class HitReactionResolver
{
    public static HitImpact Resolve(HitImpact incomingImpact, HitImpact currentAttackImpact)
    {
        if (incomingImpact == HitImpact.None)
            return HitImpact.None;

        if (incomingImpact == HitImpact.Light && currentAttackImpact == HitImpact.Heavy)
            return HitImpact.None;

        return incomingImpact;
    }
}
