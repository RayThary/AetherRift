using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ArrowProjectile : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private Rigidbody arrowRigidbody;
    [SerializeField] private Collider arrowCollider;

    [Header("Projectile")]
    [SerializeField] private float speed = 18f;
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private Vector3 visualRotationOffset;

    private Transform owner;
    private Vector3 launchDirection;
    private int damage;
    private HitImpact impact;
    private float knockbackDistance;
    private float destroyTime;

    private bool isLaunched;
    private bool hasHit;

    private void Awake()
    {
        if (arrowRigidbody == null)
            arrowRigidbody = GetComponent<Rigidbody>();

        if (arrowCollider == null)
            arrowCollider = GetComponent<Collider>();

        arrowCollider.isTrigger = true;
        arrowCollider.enabled = false;

        arrowRigidbody.useGravity = false;
        arrowRigidbody.isKinematic = true;
    }

    private void Update()
    {
        if (!isLaunched)
            return;

        UpdateRotation();

        if (Time.time >= destroyTime)
            Destroy(gameObject);
    }

    public void Prepare(Transform attachPoint)
    {
        isLaunched = false;
        hasHit = false;
        owner = null;

        if (!arrowRigidbody.isKinematic)
        {
            arrowRigidbody.linearVelocity = Vector3.zero;
            arrowRigidbody.angularVelocity = Vector3.zero;
        }

        arrowRigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        arrowRigidbody.isKinematic = true;
        arrowCollider.enabled = false;

        transform.SetParent(attachPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(visualRotationOffset);
    }

    public void Launch(Vector3 direction, Transform newOwner, int newDamage, HitImpact newImpact, float newKnockbackDistance)
    {
        launchDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;
        owner = newOwner;
        damage = Mathf.Max(newDamage, 0);
        impact = newImpact;
        knockbackDistance = Mathf.Max(newKnockbackDistance, 0f);
        destroyTime = Time.time + Mathf.Max(lifetime, 0.1f);
        isLaunched = true;

        transform.SetParent(null, true);
        transform.rotation = Quaternion.LookRotation(launchDirection) * Quaternion.Euler(visualRotationOffset);

        arrowRigidbody.isKinematic = false;
        arrowRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        arrowCollider.enabled = true;
        arrowRigidbody.linearVelocity = launchDirection * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isLaunched || hasHit || !IsHitLayer(other.gameObject.layer) || IsOwnerCollider(other))
            return;

        hasHit = true;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.TakeDamage(new DamageInfo(damage, impact, launchDirection, knockbackDistance));

        Destroy(gameObject);
    }

    private void UpdateRotation()
    {
        Vector3 velocity = arrowRigidbody.linearVelocity;

        if (velocity.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.LookRotation(velocity.normalized) * Quaternion.Euler(visualRotationOffset);
    }

    private bool IsHitLayer(int layer)
    {
        return (hitLayers.value & (1 << layer)) != 0;
    }

    private bool IsOwnerCollider(Collider other)
    {
        return owner != null && (other.transform == owner || other.transform.IsChildOf(owner));
    }
}
