
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraTarget : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;

    [Header("Lookahead")]
    [SerializeField] private float maxLookahead = 3f;
    [SerializeField] private float mouseInfluence = 0.5f;
    [SerializeField] private float smoothSpeed = 8f;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (player != null)
            transform.position = player.position;
    }

    private void LateUpdate()
    {
        if (player == null || playerCamera == null || Mouse.current == null)
            return;

        var ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        var ground = new Plane(Vector3.up, player.position);

        var targetPosition = player.position;

        if (ground.Raycast(ray, out float distance))
        {
            var mouseWorld = ray.GetPoint(distance);

            var offset = mouseWorld - player.position;
            offset.y = 0f;

            targetPosition += Vector3.ClampMagnitude(
                offset * mouseInfluence,
                maxLookahead
            );
        }

        var t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
    }
}
