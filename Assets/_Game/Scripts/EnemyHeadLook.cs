
using UnityEngine;

public class EnemyHeadLook : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform player;

    [Header("Settings")]
    [SerializeField] private float detectionRange = 30f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float maxAngle = 80f;

    private Quaternion initialLocalRotation;
    private bool isDead;

    private void Awake()
    {
        initialLocalRotation = head.localRotation;

        if (player == null)
            player = FindAnyObjectByType<PlayerLocomotion>().transform;
    }

    private void LateUpdate()
    {
        if (isDead || player == null)
            return;

        var targetRotation = initialLocalRotation;

        var direction = player.position - head.position;

        if (direction.sqrMagnitude <= detectionRange * detectionRange)
        {
            var localDirection = head.parent.InverseTransformDirection(direction);

            var restForward = initialLocalRotation * Vector3.forward;

            var angle = Vector3.Angle(restForward, localDirection);

            if (angle > maxAngle)
            {
                localDirection = Vector3.RotateTowards(
                    restForward,
                    localDirection,
                    maxAngle * Mathf.Deg2Rad,
                    0f
                );
            }

            targetRotation =
                Quaternion.LookRotation(localDirection, Vector3.up);
        }

        head.localRotation = Quaternion.Slerp(
            head.localRotation,
            targetRotation,
            1f - Mathf.Exp(-rotationSpeed * Time.deltaTime)
        );
    }

    public void OnDeath()
    {
        isDead = true;
        enabled = false;
    }
}
