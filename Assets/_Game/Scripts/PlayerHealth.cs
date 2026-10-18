
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Health Display")]
    [SerializeField, Min(0f)] private float healthAnimationDuration = 0.7f;

    [Header("Passive Sanity Drain")]
    [SerializeField] private float sanityDrainInterval = 5f;
    [SerializeField] private float sanityDrainAmount = 1f;

    [Header("Sanity Vignette")]
    [SerializeField] private Volume globalVolume;
    [SerializeField, Range(0f, 1f)] private float healthyVignette = 0.2f;
    [SerializeField, Range(0f, 1f)] private float criticalVignette = 0.6f;

    private Vignette vignette;

    [Header("Events")]
    public UnityEvent onHealthChanged;
    public UnityEvent onDeath;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public float HealthPercent => CurrentHealth / maxHealth;
    public bool IsDead => CurrentHealth <= 0f;

    public bool GameEnded { get; set; }

    private Coroutine healthAnimation;
    private float displayedHealth;
    private float sanityDrainTimer;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        displayedHealth = CurrentHealth;

        if (globalVolume != null && globalVolume.profile.TryGet<Vignette>(out var effect))
        {
            vignette = effect;
            vignette.intensity.overrideState = true;
            UpdateVignette();
        }

        UpdateHealthText();
    }

    private void Start()
    {
        onHealthChanged?.Invoke();
    }

    private void Update()
    {
        if (GameEnded)
            return;

        if (IsDead || sanityDrainInterval <= 0f)
            return;

        sanityDrainTimer += Time.deltaTime;

        if (sanityDrainTimer >= sanityDrainInterval)
        {
            sanityDrainTimer -= sanityDrainInterval;
            TakeDamage(sanityDrainAmount);
        }
    }

    private void UpdateVignette()
    {
        if (vignette == null)
            return;

        var sanityPercent = Mathf.Clamp01(displayedHealth / maxHealth);

        vignette.intensity.value = Mathf.Lerp(
            criticalVignette,
            healthyVignette,
            sanityPercent
        );
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f)
            return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

        AnimateHealthText();
        onHealthChanged?.Invoke();

        if (IsDead)
            onDeath?.Invoke();
    }

    public void TakeHealing(float healing)
    {
        if (IsDead || healing <= 0f)
            return;

        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + healing);

        AnimateHealthText();
        onHealthChanged?.Invoke();

        if (IsDead)
            onDeath?.Invoke();
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);

        AnimateHealthText();
        onHealthChanged?.Invoke();
    }

    public void RestoreFullHealth()
    {
        CurrentHealth = maxHealth;

        AnimateHealthText();
        onHealthChanged?.Invoke();
    }

    private void AnimateHealthText()
    {
        if (healthAnimation != null)
            StopCoroutine(healthAnimation);

        healthAnimation = StartCoroutine(AnimateHealth());
    }

    private IEnumerator AnimateHealth()
    {
        var startHealth = displayedHealth;
        var targetHealth = CurrentHealth;

        if (healthAnimationDuration <= 0f)
        {
            displayedHealth = targetHealth;
            UpdateHealthText();
            UpdateVignette();
            healthAnimation = null;
            yield break;
        }

        var elapsed = 0f;

        while (elapsed < healthAnimationDuration)
        {
            elapsed += Time.deltaTime;

            var t = Mathf.Clamp01(elapsed / healthAnimationDuration);

            displayedHealth = Mathf.Lerp(startHealth, targetHealth, t);
            UpdateHealthText();
            UpdateVignette();

            yield return null;
        }

        displayedHealth = targetHealth;
        UpdateHealthText();
        UpdateVignette();
        healthAnimation = null;
    }

    private void UpdateHealthText()
    {
        if (healthText == null)
            return;

        healthText.text = $"Sanity: {Mathf.CeilToInt(displayedHealth)}%";
    }
}
