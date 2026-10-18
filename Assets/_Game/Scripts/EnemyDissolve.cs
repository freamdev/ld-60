
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDissolve : MonoBehaviour
{
    [Header("Dissolve")]
    [SerializeField] private Material dissolveMaterial;
    [SerializeField] private float duration = 1.5f;

    private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private readonly List<Material> materials = new();

    private bool isDissolving;

    public IEnumerator Dissolve()
    {
        if (isDissolving)
            yield break;

        isDissolving = true;

        PrepareMaterials();

        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            var progress = Mathf.Clamp01(elapsed / duration);

            foreach (var material in materials)
                material.SetFloat(DissolveId, progress);

            yield return null;
        }

        foreach (var material in materials)
            material.SetFloat(DissolveId, 1f);
    }

    private void PrepareMaterials()
    {
        foreach (var renderer in (Renderer[])(GetComponentsInChildren<MeshRenderer>()))
        {
            var originals = renderer.sharedMaterials;
            var replacements = new Material[originals.Length];

            for (var i = 0; i < originals.Length; i++)
            {
                var original = originals[i];

                if (original == null)
                    continue;

                var replacement = new Material(dissolveMaterial);

                var originalColor = Color.white;

                if (original.HasProperty(BaseColorId))
                {
                    originalColor = original.GetColor(BaseColorId);
                }
                else if (original.HasProperty("_Color"))
                {
                    originalColor = original.GetColor("_Color");
                }

                replacement.SetColor(BaseColorId, originalColor);
                replacement.SetFloat(DissolveId, 0f);

                replacements[i] = replacement;
                materials.Add(replacement);
            }

            renderer.materials = replacements;
        }
    }

    private void OnDestroy()
    {
        foreach (var material in materials)
        {
            if (material != null)
                Destroy(material);
        }
    }
}
