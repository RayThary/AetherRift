using UnityEngine;

public enum HitImpact
{
    None,
    Light,
    Heavy
}

public readonly struct DamageInfo
{
    public int Damage { get; }
    public HitImpact Impact { get; }
    public Vector3 HitDirection { get; }
    public float KnockbackDistance { get; }

    public DamageInfo(int damage, HitImpact impact, Vector3 hitDirection, float knockbackDistance)
    {
        Damage = damage;
        Impact = impact;
        HitDirection = hitDirection;
        KnockbackDistance = knockbackDistance;
    }
}
