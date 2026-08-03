using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackHitbox : MonoBehaviour
{
    [Header("Hitbox")]
    [SerializeField] private BoxCollider hitboxCollider;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int maxHitCount = 16;

    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    private Collider[] hitResults;

    private int preparedDamage;
    private HitImpact preparedAttackImpact;
    private float preparedKnockbackDistance;

    private int currentDamage;
    private HitImpact currentAttackImpact;
    private float currentKnockbackDistance;

    private bool isHitboxOpen;

    private void Awake()
    {
        hitResults = new Collider[Mathf.Max(maxHitCount, 1)];

        if (hitboxCollider == null)
            Debug.LogError("[PlayerAttackHitbox] Hitbox Collider가 연결되지 않았습니다.");
    }

    private void LateUpdate()
    {
        if (!isHitboxOpen || hitboxCollider == null)
            return;

        CheckHitbox();
    }

    public void SetAttackData(int damage, HitImpact impact, float knockbackDistance)
    {
        preparedDamage = Mathf.Max(damage, 0);
        preparedAttackImpact = impact;
        preparedKnockbackDistance = Mathf.Max(knockbackDistance, 0f);

        if (preparedDamage <= 0)
            CloseAttackHitbox();
    }

    public void OpenAttackHitbox()
    {
        if (preparedDamage <= 0)
            return;

        currentDamage = preparedDamage;
        currentAttackImpact = preparedAttackImpact;
        currentKnockbackDistance = preparedKnockbackDistance;

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

        int hitCount = Physics.OverlapBoxNonAlloc(worldCenter, halfExtents, hitResults, hitboxTransform.rotation, enemyLayer, QueryTriggerInteraction.Collide);

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

    private void OnDrawGizmosSelected()
    {
        if (hitboxCollider == null)
            return;

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(hitboxCollider.transform.position, hitboxCollider.transform.rotation, hitboxCollider.transform.lossyScale);
        Gizmos.DrawWireCube(hitboxCollider.center, hitboxCollider.size);
        Gizmos.matrix = previousMatrix;
    }
}
