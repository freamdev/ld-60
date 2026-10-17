
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float movementSpeed = 5f;

    [Header("Rotation")]
    [SerializeField, Min(0f)] private float rotationSpeed = 15f;

    [Header("Mouse Raycast")]
    [SerializeField] private float groundHeightOffset = 0f;
    [SerializeField, Min(0f)] private float maxRayDistance = 1000f;

    private CharacterController controller;
    private LD60_InputSystem input;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = new LD60_InputSystem();

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void OnEnable()
    {
        input.Player.Enable();
    }

    private void OnDisable()
    {
        input.Player.Disable();
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    private void Update()
    {
        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        var movement = input.Player.Move.ReadValue<Vector2>();

        var direction = new Vector3(
            movement.x,
            0f,
            movement.y
        );

        direction = Vector3.ClampMagnitude(direction, 1f);

        controller.Move(direction * movementSpeed * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (playerCamera == null)
            return;

        var mousePosition = Mouse.current.position.ReadValue();
        var ray = playerCamera.ScreenPointToRay(mousePosition);

        var groundPlane = new Plane(
            Vector3.up,
            new Vector3(0f, transform.position.y + groundHeightOffset, 0f)
        );

        if (!groundPlane.Raycast(ray, out float distance))
            return;

        if (distance > maxRayDistance)
            return;

        var targetPoint = ray.GetPoint(distance);
        var direction = targetPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        var targetRotation = Quaternion.LookRotation(
            direction,
            Vector3.up
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Mathf.Clamp01(rotationSpeed * Time.deltaTime)
        );
    }
}
