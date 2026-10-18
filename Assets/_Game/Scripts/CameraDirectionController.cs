
using UnityEngine;

public class CameraDirectionController : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float minimumSpeed = 0.2f;

    private void LateUpdate()
    {
        var direction = characterController.velocity;
        direction.y = 0f;

        if (direction.sqrMagnitude < minimumSpeed * minimumSpeed)
            return;

        var targetRotation = Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
