
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerLocomotion))]
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerLocomotion locomotion;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private MuzzleFlashLight muzzleFlashLight;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera normalCamera;
    [SerializeField] private CinemachineCamera shoulderCamera;
    [SerializeField] private int normalPriority = 10;
    [SerializeField] private int shoulderPriority = 20;

    [Header("Projectile")]
    [SerializeField] private MusketProjectile projectilePrefab;
    [SerializeField, Min(0f)] private float projectileSpeed = 50f;
    [SerializeField, Min(0f)] private float projectileDamage = 50f;

    [Header("Firing Sequence")]
    [SerializeField, Range(0f, 1f)]
    private float firingMovementMultiplier = 0.3f;

    [SerializeField, Min(0f)]
    private float turnSpeed = 360f;

    [SerializeField, Range(0f, 45f)]
    private float firingAngleTolerance = 3f;

    [SerializeField, Min(0f)]
    private float cameraTransitionTime = 0.25f;

    [SerializeField, Min(0f)]
    private float recoveryTime = 0.5f;

    [SerializeField, Min(0f)]
    private float cooldown = 0.5f;

    [Header("Aiming Line")]
    [SerializeField] private LineRenderer aimLine;
    [SerializeField] private float groundHeightOffset = 0f;
    [SerializeField, Min(0f)] private float maxAimDistance = 100f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource gunshotAudio;
    [SerializeField] private CinemachineImpulseSource cameraImpulse;

    [Header("Weapon Recoil")]
    [SerializeField] private Transform weaponVisual;
    [SerializeField, Min(0f)] private float recoilDistance = 0.15f;
    [SerializeField, Min(0f)] private float recoilRecoverySpeed = 12f;

    private LD60_InputSystem input;

    private bool isFiring;
    private bool isAiming;
    private float nextFireTime;

    private Vector3 lockedDirection;
    private Vector3 weaponRestPosition;
    private float currentRecoil;

    private void Awake()
    {
        input = new LD60_InputSystem();

        if (locomotion == null)
            locomotion = GetComponent<PlayerLocomotion>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (weaponVisual != null)
            weaponRestPosition = weaponVisual.localPosition;

        if (aimLine != null)
        {
            aimLine.positionCount = 2;
            aimLine.useWorldSpace = true;
            aimLine.enabled = false;
        }
    }

    private void OnEnable()
    {
        input.Player.Enable();
        input.Player.Attack.performed += OnAttack;
        input.Player.Aim.performed += OnAimStarted;
        input.Player.Aim.canceled += OnAimStopped;

        SetShoulderCamera(false);
    }

    private void OnDisable()
    {
        input.Player.Attack.performed -= OnAttack;
        input.Player.Aim.performed -= OnAimStarted;
        input.Player.Aim.canceled -= OnAimStopped;

        input.Player.Disable();

        StopAllCoroutines();
        isAiming = false;
        isFiring = false;

        if (locomotion != null)
        {
            locomotion.RotationLocked = false;
            locomotion.MovementMultiplier = 1f;
        }

        if (aimLine != null)
            aimLine.enabled = false;

        if (weaponVisual != null)
            weaponVisual.localPosition = weaponRestPosition;

        currentRecoil = 0f;
        SetShoulderCamera(false);
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    private void Update()
    {
        UpdateAimLine();
        UpdateRecoil();
    }

    private void OnAimStarted(InputAction.CallbackContext context)
    {
        isAiming = true;
    }

    private void OnAimStopped(InputAction.CallbackContext context)
    {
        isAiming = false;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (!isAiming || isFiring || Time.time < nextFireTime)
            return;

        if (muzzlePoint == null || projectilePrefab == null)
            return;

        if (!TryGetAimPoint(out Vector3 aimPoint))
            return;

        lockedDirection = aimPoint - transform.position;
        lockedDirection.y = 0f;

        if (lockedDirection.sqrMagnitude < 0.001f)
            return;

        lockedDirection.Normalize();

        StartCoroutine(FiringSequence());
    }

    private IEnumerator FiringSequence()
    {
        isFiring = true;

        locomotion.RotationLocked = true;
        locomotion.MovementMultiplier = firingMovementMultiplier;

        if (aimLine != null)
            aimLine.enabled = false;

        SetShoulderCamera(true);

        var targetRotation = Quaternion.LookRotation(lockedDirection);

        var elapsed = 0f;

        while (true)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            elapsed += Time.deltaTime;

            float angle = Quaternion.Angle(transform.rotation, targetRotation);

            if (angle <= firingAngleTolerance && elapsed >= cameraTransitionTime)
                break;

            yield return null;
        }

        FireProjectile();

        yield return new WaitForSeconds(recoveryTime);

        SetShoulderCamera(false);

        locomotion.RotationLocked = false;
        locomotion.MovementMultiplier = 1f;

        nextFireTime = Time.time + cooldown;
        isFiring = false;
    }

    private void FireProjectile()
    {
        var direction = transform.forward;
        direction.y = 0f;
        direction.Normalize();

        var projectile = Instantiate(projectilePrefab, muzzlePoint.position, Quaternion.LookRotation(direction));

        projectile.Initialize(direction, projectileSpeed, projectileDamage, transform);

        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (gunshotAudio != null)
            gunshotAudio.Play();

        if (cameraImpulse != null)
            cameraImpulse.GenerateImpulse();

        currentRecoil = recoilDistance;
        muzzleFlashLight.Flash();
    }

    private void UpdateAimLine()
    {
        if (aimLine == null)
            return;

        if (!isAiming || isFiring || muzzlePoint == null)
        {
            aimLine.enabled = false;
            return;
        }

        if (!TryGetAimPoint(out Vector3 aimPoint))
        {
            aimLine.enabled = false;
            return;
        }

        aimLine.enabled = true;

        var direction = aimPoint - transform.position;
        direction.y = 0f;
        direction.Normalize();

        aimLine.SetPosition(0, muzzlePoint.position);
        aimLine.SetPosition(1, muzzlePoint.position + direction * maxAimDistance);
    }

    private bool TryGetAimPoint(out Vector3 point)
    {
        point = Vector3.zero;

        if (playerCamera == null || Mouse.current == null)
            return false;

        var ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        var ground = new Plane(Vector3.up, transform.position + Vector3.up * groundHeightOffset);

        if (!ground.Raycast(ray, out float distance))
            return false;

        if (distance > maxAimDistance)
            return false;

        point = ray.GetPoint(distance);
        return true;
    }

    private void UpdateRecoil()
    {
        if (weaponVisual == null)
            return;

        currentRecoil = Mathf.MoveTowards(currentRecoil, 0f, recoilRecoverySpeed * Time.deltaTime);

        weaponVisual.localPosition = weaponRestPosition - Vector3.forward * currentRecoil;
    }

    private void SetShoulderCamera(bool active)
    {
        if (normalCamera != null)
            normalCamera.Priority = active ? normalPriority : shoulderPriority;

        if (shoulderCamera != null)
            shoulderCamera.Priority = active ? shoulderPriority : normalPriority;
    }
}
