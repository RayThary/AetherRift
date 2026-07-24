using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackHitbox : MonoBehaviour
{
    [Header("Hitbox")]
    [SerializeField] private BoxCollider hitboxCollider;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private int maxHitCount = 8;

    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    private EnemyCombat enemyCombat;
    private Collider[] hitResults;

    private int currentDamage;
    private HitImpact currentAttackImpact;
    private float currentKnockbackDistance;

    private bool isHitboxOpen;

    private void Awake()
    {
        enemyCombat = GetComponentInParent<EnemyCombat>();
        hitResults = new Collider[Mathf.Max(maxHitCount, 1)];
    }

    private void LateUpdate()
    {
        if (!isHitboxOpen || hitboxCollider == null)
            return;

        CheckHitbox();
    }

    public void OpenAttackHitbox()
    {
        if (enemyCombat == null || enemyCombat.CurrentAttackDamage <= 0)
            return;

        currentDamage = enemyCombat.CurrentAttackDamage;
        currentAttackImpact = enemyCombat.CurrentAttackImpact;
        currentKnockbackDistance = enemyCombat.CurrentAttackKnockbackDistance;

        hitTargets.Clear();
        isHitboxOpen = true;
    }

    public void CloseAttackHitbox()
    {
        isHitboxOpen = false;

        currentDamage = 0;
        currentAttackImpact = HitImpact.None;
        currentKnockbackDistance = 0f;

        hitTargets.Clear();
    }

    private void CheckHitbox()
    {
        Transform hitboxTransform = hitboxCollider.transform;

        Vector3 worldCenter = hitboxTransform.TransformPoint(hitboxCollider.center);
        Vector3 scale = hitboxTransform.lossyScale;

        scale.x = Mathf.Abs(scale.x);
        scale.y = Mathf.Abs(scale.y);
        scale.z = Mathf.Abs(scale.z);

        Vector3 halfExtents = Vector3.Scale(hitboxCollider.size, scale) * 0.5f;

        int hitCount = Physics.OverlapBoxNonAlloc(
            worldCenter,
            halfExtents,
            hitResults,
            hitboxTransform.rotation,
            playerLayer,
            QueryTriggerInteraction.Collide
        );

        for (int i = 0; i < hitCount; i++)
        {
            IDamageable damageable = hitResults[i].GetComponentInParent<IDamageable>();

            if (damageable == null || hitTargets.Contains(damageable))
                continue;

            Vector3 hitDirection = hitResults[i].transform.position - transform.position;
            hitDirection.y = 0f;

            if (hitDirection.sqrMagnitude > 0.01f)
                hitDirection.Normalize();

            hitTargets.Add(damageable);
            damageable.TakeDamage(new DamageInfo(currentDamage, currentAttackImpact, hitDirection, currentKnockbackDistance));
        }
    }
}