using TMPro;
using UnityEngine;

public class HealingZoneSpawner : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private DarknessController darknessController;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform player;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GhostSpawner ghostSpawner;

    [Header("References")]
    [SerializeField] private GameObject healingZonePrefab;

    [Header("Map")]
    [SerializeField] private Vector2 mapSize = new(100f, 100f);
    [SerializeField] private Vector2 mapCenter = Vector2.zero;
    [SerializeField] private float groundHeight = 0f;

    [Header("Placement")]
    [SerializeField] private float minCornerDistance = 10f;
    [SerializeField] private float maxCornerDistance = 15f;

    [Header("Tree Clearing")]
    [SerializeField] private float clearingRadius = 8f;

    private void Start()
    {
        GenerateHealingZones();
    }

    private void GenerateHealingZones()
    {
        if (healingZonePrefab == null)
        {
            Debug.LogWarning("HealingZone prefab is missing!");
            return;
        }

        var halfSize = mapSize * 0.5f;

        var corners = new Vector2[] { mapCenter + new Vector2(-halfSize.x, -halfSize.y), mapCenter + new Vector2(-halfSize.x, halfSize.y), mapCenter + new Vector2(halfSize.x, -halfSize.y), mapCenter + new Vector2(halfSize.x, halfSize.y) };

        foreach (var corner in corners)
        {
            var position = GetRandomCornerPosition(corner);

            ClearTrees(position);

            var instance = Instantiate(healingZonePrefab, position, Quaternion.identity, transform);

            var zone = instance.GetComponent<HealingZone>();

            zone.Initialize(
                darknessController,
                playerHealth,
                player,
                descriptionText,
                ghostSpawner
            );
        }
    }

    private Vector3 GetRandomCornerPosition(Vector2 corner)
    {
        var directionToCenter = (mapCenter - corner).normalized;

        var distance = Random.Range(minCornerDistance, maxCornerDistance);

        var angle = Random.Range(-35f, 35f);

        var direction = (Vector2)(Quaternion.Euler(0f, 0f, angle) * directionToCenter);

        var position = corner + direction * distance;

        var halfSize = mapSize * 0.5f;

        position.x = Mathf.Clamp(
            position.x,
            mapCenter.x - halfSize.x,
            mapCenter.x + halfSize.x
        );

        position.y = Mathf.Clamp(
            position.y,
            mapCenter.y - halfSize.y,
            mapCenter.y + halfSize.y
        );

        return new Vector3(
            position.x,
            groundHeight,
            position.y
        );
    }

    private void ClearTrees(Vector3 position)
    {
        var objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var radiusSquared = clearingRadius * clearingRadius;

        foreach (var obj in objects)
        {
            if (obj.name != "Tree")
                continue;

            var offset = obj.transform.position - position;
            offset.y = 0f;

            if (offset.sqrMagnitude <= radiusSquared)
                Destroy(obj);
        }
    }

    private void OnDrawGizmosSelected()
    {
        var halfSize = mapSize * 0.5f;

        Gizmos.color = Color.yellow;

        Gizmos.DrawWireCube(
            new Vector3(mapCenter.x, groundHeight, mapCenter.y),
            new Vector3(mapSize.x, 0.1f, mapSize.y)
        );

        var corners = new Vector2[] { mapCenter + new Vector2(-halfSize.x, -halfSize.y), mapCenter + new Vector2(-halfSize.x, halfSize.y), mapCenter + new Vector2(halfSize.x, -halfSize.y), mapCenter + new Vector2(halfSize.x, halfSize.y) };

        Gizmos.color = Color.green;

        foreach (var corner in corners)
        {
            var cornerPosition = new Vector3(corner.x, groundHeight, corner.y);

            Gizmos.DrawWireSphere(
                cornerPosition,
                minCornerDistance
            );

            Gizmos.DrawWireSphere(
                cornerPosition,
                maxCornerDistance
            );
        }
    }
}
