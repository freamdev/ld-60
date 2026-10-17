
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Mouse Raycast")]
    [SerializeField] private float groundHeightOffset = 0f;
    [SerializeField] private float maxRayDistance = 1000f;

    public float MovementMultiplier { get; set; } = 1f;
    public bool RotationLocked { get; set; }

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

        if (!RotationLocked)
            HandleRotation();
    }

    private void HandleMovement()
    {
        Vector2 movement = input.Player.Move.ReadValue<Vector2>();

        Vector3 direction = new Vector3(movement.x, 0f, movement.y);

        direction = Vector3.ClampMagnitude(direction, 1f);

        controller.Move(direction * movementSpeed * MovementMultiplier * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (playerCamera == null || Mouse.current == null)
            return;

        var ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        var ground = new Plane(Vector3.up, transform.position + Vector3.up * groundHeightOffset);

        if (!ground.Raycast(ray, out float distance))
            return;

        if (distance > maxRayDistance)
            return;

        var direction = ray.GetPoint(distance) - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        var target = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(transform.rotation, target, Mathf.Clamp01(rotationSpeed * Time.deltaTime));
    }
}
