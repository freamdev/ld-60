
using System.Collections;
using UnityEngine;

public class MuzzleFlashLight : MonoBehaviour
{
    [SerializeField] private Light flashLight;
    [SerializeField] private float maxIntensity = 100f;
    [SerializeField] private float duration = 0.18f;
    [SerializeField] private ParticleSystem muzzleSmoke;

    [SerializeField]
    private AnimationCurve intensityCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.15f, 0.9f),
            new Keyframe(0.4f, 0.25f),
            new Keyframe(1f, 0f)
        );

    private Coroutine activeFlash;

    private void Awake()
    {
        flashLight.intensity = 0f;
        flashLight.enabled = false;
    }

    public void Flash()
    {
        if (activeFlash != null)
            StopCoroutine(activeFlash);

        activeFlash = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        flashLight.enabled = true;
        muzzleSmoke.Play();

        float elapsed = 0f;

        while (elapsed < duration)
        {
            var t = elapsed / duration;

            flashLight.intensity = intensityCurve.Evaluate(t) * maxIntensity;

            elapsed += Time.deltaTime;
            yield return null;
        }

        flashLight.intensity = 0f;
        flashLight.enabled = false;
        activeFlash = null;
    }
}
