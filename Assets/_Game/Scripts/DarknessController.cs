using UnityEngine;

public class DarknessController : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private float normalVisibility = 30f;
    [SerializeField] private float eventVisibility = 55f;
    [SerializeField] private float transitionSpeed = 30f;

    private float eventTargetVisibility;
    private float healingTargetVisibility;

    private void Awake()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogStartDistance = 12f;
        RenderSettings.fogEndDistance = normalVisibility;

        eventTargetVisibility = normalVisibility;
        healingTargetVisibility = normalVisibility;
    }

    private void Update()
    {
        var targetVisibility = Mathf.Max(normalVisibility, eventTargetVisibility, healingTargetVisibility);

        RenderSettings.fogEndDistance = Mathf.MoveTowards(
            RenderSettings.fogEndDistance,
            targetVisibility,
            transitionSpeed * Time.deltaTime
        );
    }

    public void SetVisibility(float distance)
    {
        eventTargetVisibility = distance;
    }

    public void ResetVisibility()
    {
        eventTargetVisibility = normalVisibility;
    }

    public void SetHealingVisibility(float distance)
    {
        healingTargetVisibility = distance;
    }

    public void ResetHealingVisibility()
    {
        healingTargetVisibility = normalVisibility;
    }
}