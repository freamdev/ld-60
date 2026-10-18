
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TreeCameraTransparency : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask treeLayer;

    [Header("Transparency")]
    [SerializeField, Range(0f, 1f)] private float targetAlpha = 0.2f;

    [SerializeField, Min(0.01f)] private float fadeSpeed = 5f;

    [SerializeField, Min(0f)] private float detectionRadius = 0.5f;

    [Header("Performance")]
    [SerializeField, Min(0f)] private float detectionInterval = 0f;

    [SerializeField, Min(1)] private int maxRaycastHits = 128;

    private RaycastHit[] hitBuffer;
    private float nextDetectionTime;

    private readonly Dictionary<Renderer, FadeData> fadingRenderers = new();

    private readonly HashSet<Renderer> blockedRenderers = new();

    private readonly List<Renderer> finishedRenderers = new();

    private readonly Dictionary<TreeTransparencyTarget, Renderer[]> treeRendererCache = new();

    private class FadeData
    {
        public Material[] originalMaterials;
        public Material[] fadeMaterials;
        public float currentAlpha = 1f;
    }

    private void Awake()
    {
        hitBuffer = new RaycastHit[Mathf.Max(1, maxRaycastHits)];
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        if (detectionInterval <= 0f || Time.time >= nextDetectionTime)
        {
            DetectBlockingTrees();

            nextDetectionTime = Time.time + detectionInterval;
        }

        UpdateFading();
    }

    private void DetectBlockingTrees()
    {
        blockedRenderers.Clear();

        var direction = player.position - transform.position;
        var distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
            return;

        var hitCount = Physics.SphereCastNonAlloc(transform.position, detectionRadius, direction / distance, hitBuffer, distance, treeLayer, QueryTriggerInteraction.Collide);

        for (var i = 0; i < hitCount; i++)
        {
            var collider = hitBuffer[i].collider;

            if (collider == null)
                continue;

            var tree = collider.GetComponentInParent<TreeTransparencyTarget>();

            if (tree == null)
                continue;

            if (!treeRendererCache.TryGetValue(tree, out var renderers))
            {
                renderers = tree.GetComponentsInChildren<Renderer>();

                treeRendererCache.Add(tree, renderers);
            }

            foreach (var renderer in renderers)
            {
                if (renderer == null)
                    continue;

                blockedRenderers.Add(renderer);

                if (!fadingRenderers.ContainsKey(renderer))
                    RegisterRenderer(renderer);
            }
        }
    }

    private void UpdateFading()
    {
        finishedRenderers.Clear();

        foreach (var pair in fadingRenderers)
        {
            var renderer = pair.Key;
            var data = pair.Value;

            if (renderer == null)
            {
                DestroyFadeMaterials(data);
                finishedRenderers.Add(renderer);
                continue;
            }

            var desiredAlpha = blockedRenderers.Contains(renderer) ? targetAlpha : 1f;

            var previousAlpha = data.currentAlpha;

            data.currentAlpha = Mathf.MoveTowards(
                data.currentAlpha,
                desiredAlpha,
                fadeSpeed * Time.deltaTime
            );

            if (!Mathf.Approximately(previousAlpha, data.currentAlpha))
            {
                foreach (var material in data.fadeMaterials)
                {
                    if (material != null && material.HasProperty("_BaseColor"))
                    {
                        var color = material.GetColor("_BaseColor");
                        color.a = data.currentAlpha;
                        material.SetColor("_BaseColor", color);
                    }
                }
            }

            if (desiredAlpha >= 1f && data.currentAlpha >= 1f)
            {
                renderer.sharedMaterials = data.originalMaterials;

                DestroyFadeMaterials(data);

                finishedRenderers.Add(renderer);
            }
        }

        foreach (var renderer in finishedRenderers)
            fadingRenderers.Remove(renderer);
    }

    private void RegisterRenderer(Renderer renderer)
    {
        var originals = renderer.sharedMaterials;
        var fades = new Material[originals.Length];

        for (var i = 0; i < originals.Length; i++)
        {
            var original = originals[i];

            if (original == null)
                continue;

            var material = new Material(original);

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);

                material.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.SrcAlpha
                );

                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha
                );

                material.SetFloat("_ZWrite", 0f);

                material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                material.renderQueue = (int)RenderQueue.Transparent;
            }

            fades[i] = material;
        }

        renderer.sharedMaterials = fades;

        fadingRenderers.Add(renderer, new FadeData
        {
            originalMaterials = originals,
            fadeMaterials = fades,
            currentAlpha = 1f
        });
    }

    private void DestroyFadeMaterials(FadeData data)
    {
        foreach (var material in data.fadeMaterials)
        {
            if (material != null)
                Destroy(material);
        }
    }

    private void OnDestroy()
    {
        foreach (var pair in fadingRenderers)
        {
            if (pair.Key != null)
                pair.Key.sharedMaterials = pair.Value.originalMaterials;

            DestroyFadeMaterials(pair.Value);
        }

        fadingRenderers.Clear();
        blockedRenderers.Clear();
        finishedRenderers.Clear();
        treeRendererCache.Clear();
    }
}
