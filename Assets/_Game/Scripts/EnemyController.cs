using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    [SerializeField] private SimpleEnemyRagdoll simpleEnemyRagdoll;
    [SerializeField] private EnemyHeadLook enemyHeadLock;

    public void TakeDamage(float damage, Vector3 hitDirection)
    {

    }
}
