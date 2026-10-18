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
    [SerializeField] private float mouseSensitivity = 0.15f;

    [Header("Mouse Raycast")]
    [SerializeField] private float groundHeightOffset = 0f;
    [SerializeField] private float maxRayDistance = 1000f;

    public float MovementMultiplier { get; set; } = 1f;
    public float RotationMultiplier { get; set; } = 1f;
    public bool RotationLocked { get; set; }

    public bool GameEnded { get; set; }

    private CharacterController controller;
    private LD60_InputSystem input;


    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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
        if (GameEnded)
            return;

        HandleMovement();

        if (!RotationLocked)
            HandleRotation();
    }

    private void HandleMovement()
    {
        var movement = input.Player.Move.ReadValue<Vector2>();

        if (playerCamera == null)
            return;

        var forward = playerCamera.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        var right = playerCamera.transform.right;
        right.y = 0f;
        right.Normalize();

        var direction = forward * movement.y + right * movement.x;
        direction = Vector3.ClampMagnitude(direction, 1f);

        controller.Move(direction * movementSpeed * MovementMultiplier * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (Mouse.current == null)
            return;

        var mouseDeltaX = Mouse.current.delta.ReadValue().x;

        transform.Rotate(
            Vector3.up,
            mouseDeltaX * mouseSensitivity * RotationMultiplier,
            Space.World
        );
    }
}