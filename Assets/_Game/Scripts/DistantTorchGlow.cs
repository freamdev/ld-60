
using UnityEngine;

public class DistantTorchGlow : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float flickerSpeed = 8f;
    [SerializeField] private float flickerAmount = 0.12f;
    [SerializeField] private float baseScale = 1f;

    private Renderer glowRenderer;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        glowRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        transform.rotation = targetCamera.transform.rotation;

        var flicker = 1f + (Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) - 0.5f) * 2f * flickerAmount;

        transform.localScale = Vector3.one * baseScale * flicker;

        var noise = Mathf.PerlinNoise(Time.time * 7f, 0f);

        var intensity = Mathf.Lerp(3f, 6f, noise);

        glowRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_Intensity", intensity);
        glowRenderer.SetPropertyBlock(propertyBlock);
    }
}
