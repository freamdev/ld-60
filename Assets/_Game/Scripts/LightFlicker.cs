using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private float intensityVariation = 10f;
    [SerializeField] private float flickerSpeed = 5f;

    private Light targetLight;
    private float baseIntensity;
    private float noiseSeed;

    private void Awake()
    {
        targetLight = GetComponent<Light>();
        baseIntensity = targetLight.intensity;
        noiseSeed = Random.Range(0f, 1000f);
    }

    private void Update()
    {
        var noise = Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed);

        var offset = (noise * 2f - 1f) * intensityVariation;

        targetLight.intensity = Mathf.Max(0f, baseIntensity + offset);
    }
}