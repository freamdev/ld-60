
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraOrbitController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float rotationSmoothSpeed = 8f;

    private float targetYaw;
    private float currentYaw;

    private void Start()
    {
        currentYaw = transform.eulerAngles.y;
        targetYaw = currentYaw;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        var delta = Mouse.current.delta.ReadValue();

        targetYaw += delta.x * mouseSensitivity;

        currentYaw = Mathf.LerpAngle(
            currentYaw,
            targetYaw,
            1f - Mathf.Exp(-rotationSmoothSpeed * Time.deltaTime)
        );
    }

    private void LateUpdate()
    {
        transform.position = player.position;
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
    }
}
