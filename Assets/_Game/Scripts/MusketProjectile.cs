
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
        if (hasHit)
            return;

        hasHit = true;

        var damageable = collision.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.TakeDamage(damage);

        if (impactPrefab != null)
        {
            var contact = collision.GetContact(0);

            var impact = Instantiate(impactPrefab, contact.point, Quaternion.LookRotation(contact.normal));

            Destroy(impact.gameObject, 3f);
        }

        if (destroyOnImpact)
            Destroy(gameObject);
    }
}
