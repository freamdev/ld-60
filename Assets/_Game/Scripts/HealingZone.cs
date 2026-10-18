
using System.Collections;
using TMPro;
using UnityEngine;

public class HealingZone : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DarknessController darknessController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform player;
    [SerializeField] private GhostSpawner ghostSpawner;

    [Header("Interaction")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI interactText;
    [SerializeField, TextArea(2, 4)]
    private string description = "I can get some rest here...";
    [SerializeField] private float textRevealDuration = 1f;
    [SerializeField] private GameObject markerObject;

    [Header("Activation")]
    [SerializeField] private float activationRadius = 5f;
    [SerializeField] private float duration = 20f;

    [Header("Healing")]
    [SerializeField] private float healingAmount = 5f;
    [SerializeField] private float healingInterval = 2f;

    [Header("Visibility")]
    [SerializeField] private float eventVisibility = 80f;
    [SerializeField] private float normalVisibility = 30f;

    [Header("Endless Mode")]
    [SerializeField, Min(0f)]
    private float endlessRespawnDelay = 300f;

    [Header("Effects")]
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private GameObject lightObject;

    private LD60_InputSystem input;
    private Coroutine textCoroutine;

    private float remainingTime;
    private float healingTimer;
    private float respawnTimer;

    private bool activated;
    private bool finished;
    private bool playerInside;

    private bool IsEndless =>
        ghostSpawner != null && ghostSpawner.IsEndless;

    public void Initialize(
        DarknessController darkness,
        PlayerHealth health,
        Transform playerTransform,
        TextMeshProUGUI descriptionGui,
        GhostSpawner spawner)
    {
        darknessController = darkness;
        playerHealth = health;
        player = playerTransform;
        descriptionText = descriptionGui;
        ghostSpawner = spawner;

        if (descriptionText != null)
        {
            descriptionText.text = description;
            descriptionText.maxVisibleCharacters = 0;
            descriptionText.gameObject.SetActive(false);
        }
    }

    private void Awake()
    {
        input = new LD60_InputSystem();

        if (interactText != null)
        {
            interactText.text = "[E]";
            interactText.gameObject.SetActive(false);
        }

        if (markerObject != null)
            markerObject.SetActive(true);

        if (particles != null)
        {
            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (lightObject != null)
            lightObject.SetActive(false);

        StartCoroutine(Wait5ThenDestroyRigidbodies());
    }

    private IEnumerator Wait5ThenDestroyRigidbodies()
    {
        yield return new WaitForSeconds(5);

        foreach (var rb in GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = true;
        }
    }

    private void OnEnable()
    {
        input.Player.Enable();
    }

    private void OnDisable()
    {
        input.Player.Disable();

        if (textCoroutine != null)
        {
            StopCoroutine(textCoroutine);
            textCoroutine = null;
        }

        if (activated && !finished && darknessController != null)
            darknessController.ResetHealingVisibility();
    }

    private void OnDestroy()
    {
        input?.Dispose();
    }

    private void Update()
    {
        // Handle endless mode cooldown even while finished.
        if (finished)
        {
            if (IsEndless)
                UpdateRespawn();

            return;
        }

        if (player == null)
            return;

        bool inside = Vector3.Distance(
            transform.position,
            player.position
        ) <= activationRadius;

        if (inside != playerInside)
        {
            playerInside = inside;

            if (!activated)
            {
                if (inside)
                    ShowInteraction();
                else
                    HideInteraction();
            }
            else if (!inside)
            {
                if (darknessController != null)
                    darknessController.ResetHealingVisibility();

                healingTimer = 0f;
            }
        }

        if (!activated)
        {
            if (playerInside &&
                input.Player.Interact.WasPressedThisFrame())
            {
                Activate();
            }

            return;
        }

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            Finish();
            return;
        }

        if (playerInside)
        {
            UpdateVisibility();
            UpdateHealing();
        }
    }

    private void UpdateRespawn()
    {
        respawnTimer -= Time.deltaTime;

        if (respawnTimer <= 0f)
            Reactivate();
    }

    private void Reactivate()
    {
        finished = false;
        activated = false;
        playerInside = false;

        remainingTime = 0f;
        healingTimer = 0f;
        respawnTimer = 0f;

        if (markerObject != null)
            markerObject.SetActive(true);

        // Player proximity is checked again next frame.
        // If they are nearby, interaction becomes available.
        Debug.Log("Healing zone is available again!");
    }

    private void ShowInteraction()
    {
        if (interactText != null)
            interactText.gameObject.SetActive(true);

        if (descriptionText != null)
        {
            if (textCoroutine != null)
                StopCoroutine(textCoroutine);

            descriptionText.text = description;
            descriptionText.maxVisibleCharacters = 0;
            descriptionText.gameObject.SetActive(true);

            textCoroutine = StartCoroutine(RevealText());
        }
    }

    private IEnumerator RevealText()
    {
        descriptionText.ForceMeshUpdate();

        int characterCount =
            descriptionText.textInfo.characterCount;

        float elapsed = 0f;

        while (elapsed < textRevealDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsed / Mathf.Max(0.01f, textRevealDuration)
            );

            descriptionText.maxVisibleCharacters =
                Mathf.FloorToInt(progress * characterCount);

            yield return null;
        }

        descriptionText.maxVisibleCharacters = characterCount;
        textCoroutine = null;
    }

    private void HideInteraction()
    {
        if (textCoroutine != null)
        {
            StopCoroutine(textCoroutine);
            textCoroutine = null;
        }

        if (descriptionText != null)
        {
            descriptionText.maxVisibleCharacters = 0;
            descriptionText.gameObject.SetActive(false);
        }

        if (interactText != null)
            interactText.gameObject.SetActive(false);
    }

    private void Activate()
    {
        activated = true;

        remainingTime = duration;
        healingTimer = 0f;

        HideInteraction();

        if (markerObject != null)
            markerObject.SetActive(false);

        if (particles != null)
        {
            particles.gameObject.SetActive(true);

            var main = particles.main;
            main.loop = true;

            particles.Clear(true);
            particles.Play(true);
        }

        if (lightObject != null)
            lightObject.SetActive(true);

        if (darknessController != null)
            darknessController.SetHealingVisibility(eventVisibility);

        Debug.Log("Healing zone activated!");
    }

    private void UpdateVisibility()
    {
        float progress = 1f -
            remainingTime / Mathf.Max(0.01f, duration);

        float visibility = Mathf.Lerp(
            eventVisibility,
            normalVisibility,
            progress
        );

        if (darknessController != null)
            darknessController.SetHealingVisibility(visibility);
    }

    private void UpdateHealing()
    {
        if (healingInterval <= 0f)
            return;

        healingTimer += Time.deltaTime;

        if (healingTimer >= healingInterval)
        {
            healingTimer -= healingInterval;

            if (playerHealth != null)
                playerHealth.TakeHealing(healingAmount);
        }
    }

    private void Finish()
    {
        finished = true;
        activated = false;
        playerInside = false;

        if (IsEndless)
            respawnTimer = endlessRespawnDelay;

        if (darknessController != null)
            darknessController.ResetHealingVisibility();

        HideInteraction();

        if (particles != null)
        {
            particles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmitting
            );
        }

        if (lightObject != null)
            lightObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            activationRadius
        );
    }
}
