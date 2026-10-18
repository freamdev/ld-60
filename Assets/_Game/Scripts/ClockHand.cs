using UnityEngine;

public class ClockHand : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GhostSpawner ghostSpawner;
    [SerializeField] private Transform handPivot;

    [Header("Rotation")]
    [SerializeField] private float maxRotation = 270f;

    private Quaternion initialRotation;

    private void Awake()
    {
        initialRotation = handPivot.localRotation;
    }

    private void Update()
    {
        if (ghostSpawner == null || handPivot == null)
            return;

        if (ghostSpawner.IsEndless)
        {
            Destroy(gameObject);
        }

        var progress = ghostSpawner.SurvivalDuration > 0f ? Mathf.Clamp01(ghostSpawner.ElapsedTime / ghostSpawner.SurvivalDuration) : 1f;

        var angle = progress * maxRotation;

        handPivot.localRotation =
            initialRotation * Quaternion.AngleAxis(
                -angle,
                Vector3.up
            );
    }
}