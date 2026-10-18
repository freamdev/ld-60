
using System.Collections;
using UnityEngine;

public class GhostAI : MonoBehaviour, IDamageable
{
    private enum GhostState
    {
        Wandering,
        Stalking,
        Staring,
        Charging,
        Finished
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform visual;

    [Header("Detection")]
    [SerializeField, Min(0f)] private float detectionRange = 25f;

    [SerializeField, Min(0f)] private float visibilityRange = 25f;

    [SerializeField, Range(0f, 0.25f)] private float viewportMargin = 0.05f;

    [Header("Wandering")]
    [SerializeField, Min(0f)] private float wanderRadius = 12f;

    [SerializeField, Min(0f)] private float wanderSpeed = 1.5f;

    [SerializeField, Min(0f)] private float waypointReachedDistance = 1f;

    [SerializeField] private Vector2 wanderLifetime = new(12f, 20f);

    [Header("Stalking")]
    [SerializeField, Min(0f)] private float stalkingSpeed = 3f;

    [SerializeField, Min(0f)] private float behindPlayerDistance = 4f;

    [SerializeField, Min(0f)] private float ambushDistance = 4f;

    [SerializeField, Min(0f)] private float circlingDistance = 10f;

    [SerializeField, Min(0f)] private float circlingSpeed = 2f;

    [SerializeField, Range(0f, 1f)] private float behindThreshold = 0.5f;

    [Header("Staring")]
    [SerializeField, Min(0f)] private float stareDuration = 3f;

    [SerializeField, Min(0f)] private float rotationSpeed = 10f;

    [Header("Charge")]
    [SerializeField] private float minimumDashDuration = 0.25f;
    [SerializeField] private float chargeHitDelay = 0.1f;
    [SerializeField, Min(0.1f)] private float chargeSpeed = 22f;

    [SerializeField, Min(0f)] private float chargeOvershoot = 5f;

    [SerializeField, Min(0.1f)] private float minimumChargeDistance = 5f;

    [SerializeField, Min(0.01f)] private float hitRadius = 1.2f;

    [SerializeField, Min(0f)] private float chargeDamage = 35f;

    [Header("Gunshot Attraction")]
    [SerializeField, Min(0f)] private float gunshotHearingRange = 45f;

    [SerializeField, Min(0f)] private float gunshotAttractionSpeed = 5f;

    [SerializeField, Min(0f)] private float gunshotAttractionDuration = 1.5f;

    [SerializeField, Min(0f)] private float gunshotStopDistance = 5f;

    private float gunshotAttractionTimer;
    private Vector3 lastGunshotPosition;

    public static event System.Action<Vector3> OnGunshot;

    [Header("Visual")]
    [SerializeField, Min(0f)] private float hoverAmplitude = 0.2f;
    [SerializeField] private EnemyDissolve enemyDissolve;

    [SerializeField, Min(0f)] private float hoverFrequency = 1.5f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem hitParticles;
    [SerializeField] private ParticleSystem deathParticles;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip screamClip;
    [SerializeField] private AudioClip stalkingClip;
    [SerializeField] private AudioClip deathClip;

    private GhostState state;
    private Vector3 spawnPosition;
    private Vector3 wanderTarget;
    private Vector3 visualInitialLocalPosition;

    private float stateTimer;
    private float hoverOffset;

    private Vector3 chargeDirection;
    private float chargeRemainingDistance;

    private float maxWanderDuration;

    private bool wasKilled;

    private void Awake()
    {
        spawnPosition = transform.position;
        hoverOffset = Random.Range(0f, Mathf.PI * 2f);

        maxWanderDuration = Random.Range(
            wanderLifetime.x,
            wanderLifetime.y
        );


        if (visual != null)
            visualInitialLocalPosition = visual.localPosition;

        if (audioSource == null)
            audioSource = Camera.main.GetComponent<AudioSource>();

        state = GhostState.Wandering;
        PickWanderTarget();
    }

    private void Start()
    {
        if (player == null && playerHealth != null)
            player = playerHealth.transform;

        if (player == null)
        {
            var health = FindFirstObjectByType<PlayerHealth>();

            if (health != null)
            {
                player = health.transform;
                playerHealth = health;
            }
        }

        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<PlayerHealth>();

        if (playerCamera == null)
            playerCamera = Camera.main;

        if (player == null || playerHealth == null || playerCamera == null)
        {
            Debug.LogError(
                "GhostAI: Missing player, health or camera.",
                this
            );

            enabled = false;
        }
    }

    private void Update()
    {
        if (playerHealth == null || playerHealth.IsDead)
            return;

        stateTimer += Time.deltaTime;

        switch (state)
        {
            case GhostState.Wandering:
                UpdateWandering();
                break;

            case GhostState.Stalking:
                UpdateStalking();
                break;

            case GhostState.Staring:
                UpdateStaring();
                break;

            case GhostState.Charging:
                UpdateCharging();
                break;
        }

        UpdateHover();
    }

    public static void ReportGunshot(Vector3 position)
    {
        OnGunshot?.Invoke(position);
    }

    private void OnEnable()
    {
        OnGunshot += HearGunshot;
    }

    private void OnDisable()
    {
        OnGunshot -= HearGunshot;
    }

    private void HearGunshot(Vector3 position)
    {
        if (state == GhostState.Staring || state == GhostState.Charging || state == GhostState.Finished)
        {
            return;
        }

        var distance = HorizontalDistance(transform.position, position);

        if (distance > gunshotHearingRange)
            return;

        lastGunshotPosition = position;

        gunshotAttractionTimer =
            gunshotAttractionDuration;
    }

    private bool UpdateGunshotAttraction()
    {
        if (gunshotAttractionTimer <= 0f)
            return false;

        if (IsSpotted())
        {
            gunshotAttractionTimer = 0f;
            EnterStaring();
            return true;
        }

        gunshotAttractionTimer -= Time.deltaTime;

        var distance = HorizontalDistance(transform.position, lastGunshotPosition);

        if (distance > gunshotStopDistance)
        {
            MoveTowards(
                lastGunshotPosition,
                gunshotAttractionSpeed
            );
        }

        return true;
    }


    private void UpdateWandering()
    {
        if (stateTimer >= maxWanderDuration)
        {
            Disappear();
            return;
        }

        if (UpdateGunshotAttraction())
            return;

        if (IsSpotted())
        {
            EnterStaring();
            return;
        }

        if (HorizontalDistance(transform.position, player.position) <= detectionRange)
        {
            ChangeState(GhostState.Stalking);
            return;
        }

        MoveTowards(wanderTarget, wanderSpeed);

        if (HorizontalDistance(transform.position, wanderTarget) <= waypointReachedDistance)
        {
            PickWanderTarget();
        }
    }

    private void PickWanderTarget()
    {
        var offset = Random.insideUnitCircle * wanderRadius;

        wanderTarget = spawnPosition +
            new Vector3(offset.x, 0f, offset.y);
    }


    private void UpdateStalking()
    {
        if (UpdateGunshotAttraction())
            return;

        if (IsSpotted())
        {
            EnterStaring();
            return;
        }

        var toGhost = transform.position - player.position;
        toGhost.y = 0f;

        var distance = toGhost.magnitude;

        if (distance <= ambushDistance)
        {
            EnterStaring();
            return;
        }

        var cameraForward = playerCamera.transform.forward;
        cameraForward.y = 0f;
        cameraForward.Normalize();

        if (cameraForward.sqrMagnitude < 0.001f)
            cameraForward = player.forward;

        cameraForward.y = 0f;
        cameraForward.Normalize();

        var ghostDirection = toGhost.normalized;

        var dot = Vector3.Dot(cameraForward, ghostDirection);

        if (dot > -behindThreshold)
        {
            var tangent = Vector3.Cross(Vector3.up, ghostDirection);

            var side = Vector3.Cross(cameraForward, ghostDirection).y;

            if (side < 0f)
                tangent = -tangent;

            var radialError = distance - circlingDistance;

            var radialCorrection = -ghostDirection * radialError * 0.5f;

            var movementDirection = (tangent + radialCorrection).normalized;

            transform.position +=
                movementDirection *
                circlingSpeed *
                Time.deltaTime;

            FacePlayer();
            return;
        }

        var behindPosition = player.position - cameraForward * behindPlayerDistance;

        MoveTowards(behindPosition, stalkingSpeed);
    }


    private bool IsSpotted()
    {
        var targetPosition = transform.position;

        if (visual != null)
            targetPosition = visual.position;

        if (Vector3.Distance(playerCamera.transform.position, targetPosition) > visibilityRange)
        {
            return false;
        }

        var viewport = playerCamera.WorldToViewportPoint(targetPosition);

        if (viewport.z <= 0f)
            return false;

        return viewport.x >= viewportMargin &&
               viewport.x <= 1f - viewportMargin &&
               viewport.y >= viewportMargin &&
               viewport.y <= 1f - viewportMargin;
    }


    private void EnterStaring()
    {
        ChangeState(GhostState.Staring);
    }

    private void UpdateStaring()
    {
        FacePlayer();

        if (stateTimer >= stareDuration)
            EnterCharging();
    }


    private void EnterCharging()
    {
        var direction = player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        direction.y = 0f;
        chargeDirection = direction.normalized;

        var distanceToPlayer = direction.magnitude;

        chargeRemainingDistance = Mathf.Max(
            minimumChargeDistance,
            distanceToPlayer + chargeOvershoot
        );

        ChangeState(GhostState.Charging);

        transform.rotation =
            Quaternion.LookRotation(chargeDirection);

        if (audioSource != null && screamClip != null)
            audioSource.PlayOneShot(screamClip);
    }

    private void UpdateCharging()
    {
        var movement = Mathf.Min(chargeSpeed * Time.deltaTime, chargeRemainingDistance);

        var previousPosition = transform.position;

        transform.position += chargeDirection * movement;

        chargeRemainingDistance -= movement;

        if (stateTimer >= chargeHitDelay && DidDashHitPlayer(previousPosition, transform.position))
        {
            HitPlayer();
            return;
        }

        if (chargeRemainingDistance <= 0f && stateTimer >= minimumDashDuration)
        {
            Disappear();
        }
    }

    private bool DidDashHitPlayer(
        Vector3 start,
        Vector3 end)
    {
        var a = new Vector2(start.x, start.z);
        var b = new Vector2(end.x, end.z);
        var p = new Vector2(player.position.x, player.position.z);

        var segment = b - a;

        var lengthSquared = segment.sqrMagnitude;

        var t = lengthSquared > 0.00001f ? Mathf.Clamp01(Vector2.Dot(p - a, segment) / lengthSquared) : 0f;

        var closest = a + segment * t;

        return Vector2.Distance(
            closest,
            p
        ) <= hitRadius;
    }

    private void HitPlayer()
    {
        if (wasKilled)
            return;

        if (state == GhostState.Finished)
            return;

        playerHealth.TakeDamage(chargeDamage);

        SpawnParticles(
            hitParticles,
            player.position + Vector3.up
        );

        Disappear();
    }


    public void Kill()
    {
        if (wasKilled || state == GhostState.Finished)
            return;

        wasKilled = true;

        SpawnParticles(
            deathParticles,
            transform.position
        );

        if (deathClip != null)
        {
            AudioSource.PlayClipAtPoint(
                deathClip,
                transform.position
            );
        }

        StartCoroutine(DissolveAndDisappear());
    }

    private IEnumerator DissolveAndDisappear()
    {
        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        yield return enemyDissolve.Dissolve();

        Disappear();
    }


    private void Disappear()
    {
        if (state == GhostState.Finished)
            return;

        ChangeState(GhostState.Finished);
        Destroy(gameObject);
    }


    private void ChangeState(GhostState newState)
    {
        state = newState;
        stateTimer = 0f;

        if (state == GhostState.Stalking)
        {
            if (audioSource != null && stalkingClip != null)
                audioSource.PlayOneShot(stalkingClip);
        }
    }

    private void MoveTowards(
        Vector3 target,
        float speed)
    {
        target.y = transform.position.y;

        var direction = target - transform.position;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );

        RotateTowards(direction);
    }

    private void FacePlayer()
    {
        var direction = player.position - transform.position;

        RotateTowards(direction);
    }

    private void RotateTowards(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        var targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void UpdateHover()
    {
        if (visual == null)
            return;

        var position = visualInitialLocalPosition;

        position.y += Mathf.Sin(
            Time.time * hoverFrequency * Mathf.PI * 2f
            + hoverOffset
        ) * hoverAmplitude;

        visual.localPosition = position;
    }

    private void SpawnParticles(
        ParticleSystem prefab,
        Vector3 position)
    {
        if (prefab == null)
            return;

        var instance = Instantiate(prefab, position, Quaternion.identity);

        instance.Play();

        var main = instance.main;

        var lifetime = main.duration + main.startLifetime.constantMax + 1f;

        Destroy(instance.gameObject, lifetime);
    }

    private float HorizontalDistance(
        Vector3 a,
        Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            detectionRange
        );

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            visibilityRange
        );

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            ambushDistance
        );
    }

    public void TakeDamage(float damage)
    {
        if (damage <= 0f)
            return;

        Kill();
    }

    public void TakeDamage(float damage, Vector3 hitDirection)
    {
        TakeDamage(damage);
    }
}
