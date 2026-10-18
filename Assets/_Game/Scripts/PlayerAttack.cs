
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerLocomotion))]
public class PlayerAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerLocomotion locomotion;
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

    [Header("Accuracy")]
    [SerializeField, Range(0f, 45f)] private float hipFireSpread = 15f;

    [Header("Aiming")]
    [SerializeField, Range(0f, 1f)] private float aimingMovementMultiplier = 0.3f;

    [SerializeField, Range(0f, 1f)] private float aimingRotationMultiplier = 0.35f;

    [Header("Cooldown")]
    [SerializeField, Min(0f)] private float cooldown = 2f;
    [SerializeField, Min(0f)] private float hipCooldown = 2f;

    [Header("Aiming Line")]
    [SerializeField] private LineRenderer aimLine;
    [SerializeField, Min(0f)] private float maxAimDistance = 100f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private AudioSource gunshotAudio;
    [SerializeField] private CinemachineImpulseSource cameraImpulse;
    [SerializeField] private AudioSource reloadAudioSource;
    [SerializeField] private AudioClip reloadCompleteClip;

    [Header("Weapon Recoil")]
    [SerializeField] private Transform weaponVisual;
    [SerializeField, Min(0f)] private float recoilDistance = 0.15f;
    [SerializeField, Min(0f)] private float recoilRecoverySpeed = 12f;

    [Header("Reload UI")]
    [SerializeField] private CanvasGroup reloadCanvasGroup;
    [SerializeField] private RectTransform reloadTextTransform;
    [SerializeField, Min(0f)] private float reloadFadeSpeed = 4f;
    [SerializeField, Min(0f)] private float reloadPopSpeed = 12f;
    [SerializeField] private float reloadPopScale = 1.3f;

    [Header("Reload Text Fill")]
    [SerializeField] private RectTransform reloadFillArea;
    [SerializeField] private TMPro.TextMeshProUGUI reloadColoredText;
    [SerializeField] private Color reloadFillColor = Color.red;

    private float reloadStartTime;
    private float reloadDuration;
    private float reloadFullWidth;

    public bool GameEnded { get; set; }

    private float reloadScale = 1f;

    private LD60_InputSystem input;

    private bool isAiming;
    private float nextFireTime;

    private Vector3 weaponRestPosition;
    private float currentRecoil;

    private bool reloadSoundPending;

    private void Awake()
    {
        input = new LD60_InputSystem();

        if (locomotion == null)
            locomotion = GetComponent<PlayerLocomotion>();

        if (weaponVisual != null)
            weaponRestPosition = weaponVisual.localPosition;

        if (aimLine != null)
        {
            aimLine.positionCount = 2;
            aimLine.useWorldSpace = true;
            aimLine.enabled = false;
        }

        if (reloadFillArea != null)
        {
            reloadFullWidth = reloadFillArea.rect.width;
            reloadFillArea.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal, 0f);
        }

        if (reloadColoredText != null)
            reloadColoredText.color = reloadFillColor;
    }

    private void OnEnable()
    {
        input.Player.Attack.performed += OnAttack;
        input.Player.Aim.performed += OnAimStarted;
        input.Player.Aim.canceled += OnAimStopped;

        input.Player.Enable();

        SetAiming(false);
    }

    private void OnDisable()
    {
        input.Player.Attack.performed -= OnAttack;
        input.Player.Aim.performed -= OnAimStarted;
        input.Player.Aim.canceled -= OnAimStopped;

        input.Player.Disable();

        SetAiming(false);

        if (aimLine != null)
            aimLine.enabled = false;

        if (weaponVisual != null)
            weaponVisual.localPosition = weaponRestPosition;

        currentRecoil = 0f;
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    private void Update()
    {
        if (GameEnded)
            return;

        UpdateAimLine();
        UpdateRecoil();
        UpdateReloadUI();
    }

    private void UpdateReloadUI()
    {
        if (reloadSoundPending && Time.time >= nextFireTime - .7f)
        {
            reloadSoundPending = false;

            if (reloadAudioSource != null && reloadCompleteClip != null)
                reloadAudioSource.PlayOneShot(reloadCompleteClip);
        }

        if (reloadCanvasGroup == null)
            return;

        var progress = reloadDuration > 0f ? Mathf.Clamp01((Time.time - reloadStartTime) / reloadDuration) : 1f;

        if (reloadFillArea != null)
        {
            reloadFillArea.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                reloadFullWidth * progress
            );
        }

        var isReloading = Time.time < nextFireTime - 0.5f;

        var targetAlpha = isReloading ? 1f : 0f;

        reloadCanvasGroup.alpha = Mathf.MoveTowards(
            reloadCanvasGroup.alpha,
            targetAlpha,
            reloadFadeSpeed * Time.deltaTime
        );

        if (reloadTextTransform != null)
        {
            reloadScale = Mathf.MoveTowards(
                reloadScale,
                1f,
                reloadPopSpeed * Time.deltaTime
            );

            reloadTextTransform.localScale = Vector3.one * reloadScale;
        }
    }

    private void OnAimStarted(InputAction.CallbackContext context)
    {
        SetAiming(true);
    }

    private void OnAimStopped(InputAction.CallbackContext context)
    {
        SetAiming(false);
    }

    private void SetAiming(bool aiming)
    {
        isAiming = aiming;

        if (locomotion != null)
        {
            locomotion.MovementMultiplier = aiming
                ? aimingMovementMultiplier
                : 1f;

            locomotion.RotationMultiplier = aiming
                ? aimingRotationMultiplier
                : 1f;
        }

        SetShoulderCamera(aiming);

        if (!aiming && aimLine != null)
            aimLine.enabled = false;
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (GameEnded)
            return;

        if (Time.time < nextFireTime)
            return;

        if (projectilePrefab == null || muzzlePoint == null)
            return;

        FireProjectile();

        if (isAiming)
        {
            nextFireTime = Time.time + cooldown;
        }
        else
        {
            nextFireTime = Time.time + hipCooldown;
        }

        reloadStartTime = Time.time;
        reloadDuration = isAiming ? cooldown : hipCooldown;

        reloadSoundPending = true;

        if (reloadFillArea != null)
            reloadFillArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);

        if (reloadCanvasGroup != null)
            reloadCanvasGroup.alpha = 1f;

        reloadScale = reloadPopScale;

        if (reloadTextTransform != null)
            reloadTextTransform.localScale = Vector3.one * reloadScale;
    }

    private void FireProjectile()
    {
        var direction = transform.forward;

        if (!isAiming)
        {
            var angle = Random.Range(-hipFireSpread, hipFireSpread);

            direction = Quaternion.AngleAxis(
                angle,
                Vector3.up
            ) * direction;
        }

        direction.y = 0f;
        direction.Normalize();

        var projectile = Instantiate(projectilePrefab, muzzlePoint.position, Quaternion.LookRotation(direction));

        projectile.Initialize(
            direction,
            projectileSpeed,
            projectileDamage,
            transform
        );

        PlayFireEffects();

        GhostAI.ReportGunshot(muzzlePoint.position);
    }

    private void PlayFireEffects()
    {
        if (muzzleFlash != null)
            muzzleFlash.Play();

        if (gunshotAudio != null)
            gunshotAudio.Play();

        if (cameraImpulse != null)
            cameraImpulse.GenerateImpulse();

        if (muzzleFlashLight != null)
            muzzleFlashLight.Flash();

        currentRecoil = recoilDistance;
    }

    private void UpdateAimLine()
    {
        if (aimLine == null)
            return;

        if (!isAiming || muzzlePoint == null)
        {
            aimLine.enabled = false;
            return;
        }

        aimLine.enabled = true;

        var direction = transform.forward;
        direction.y = 0f;
        direction.Normalize();

        aimLine.SetPosition(0, muzzlePoint.position);
        aimLine.SetPosition(
            1,
            muzzlePoint.position + direction * maxAimDistance
        );
    }

    private void UpdateRecoil()
    {
        if (weaponVisual == null)
            return;

        currentRecoil = Mathf.MoveTowards(
            currentRecoil,
            0f,
            recoilRecoverySpeed * Time.deltaTime
        );

        weaponVisual.localPosition =
            weaponRestPosition - Vector3.forward * currentRecoil;
    }

    private void SetShoulderCamera(bool active)
    {
        if (normalCamera != null)
            normalCamera.Priority = normalPriority;

        if (shoulderCamera != null)
            shoulderCamera.Priority = active
                ? shoulderPriority
                : normalPriority - 1;
    }
}
