using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MusketProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool destroyOnImpact = true;

    [Header("Impact Effects")]
    [SerializeField] private ParticleSystem impactPrefab;

    private Rigidbody rb;
    private float damage;
    private bool hasHit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Initialize(Vector3 direction, float speed, float projectileDamage, Transform owner)
    {
        damage = projectileDamage;

        transform.rotation = Quaternion.LookRotation(direction);
        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        var contact = collision.GetContact(0);

        HandleImpact(
            collision.collider,
            contact.point,
            contact.normal
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        var hitPoint = other.ClosestPoint(transform.position);

        var hitNormal = -rb.linearVelocity.normalized;

        HandleImpact(other, hitPoint, hitNormal);
    }

    private void HandleImpact(Collider other, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (hasHit)
            return;

        hasHit = true;

        var damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.TakeDamage(damage, rb.linearVelocity.normalized);

        if (impactPrefab != null)
        {
            var impact = Instantiate(impactPrefab, hitPoint, Quaternion.LookRotation(hitNormal));

            Destroy(impact.gameObject, 3f);
        }

        if (destroyOnImpact)
            Destroy(gameObject);
    }
}