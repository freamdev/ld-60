using UnityEngine;

public class SimpleEnemyRagdoll : MonoBehaviour
{
    [SerializeField] private Rigidbody body;
    [SerializeField] private Rigidbody head;

    [SerializeField] private Collider mainCollider;
    [SerializeField] private MonoBehaviour[] scriptsToDisable;

    [SerializeField] private float deathImpulse = 4f;

    private bool isDead;

    private void Awake()
    {
        body.isKinematic = true;
        head.isKinematic = true;
    }

    public void Die(Vector3 hitDirection)
    {
        if (isDead) return;
        isDead = true;

        foreach (var script in scriptsToDisable)
            script.enabled = false;

        if (mainCollider != null)
            mainCollider.enabled = false;

        body.useGravity = true;
        head.useGravity = true;

        body.isKinematic = false;
        head.isKinematic = false;

        body.AddForce(
            hitDirection.normalized * deathImpulse,
            ForceMode.Impulse
        );


        head.AddTorque(
            Random.onUnitSphere * 2f,
            ForceMode.Impulse
        );

        Destroy(gameObject, 8f);
    }
}