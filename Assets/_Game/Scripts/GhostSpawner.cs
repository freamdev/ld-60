
using System.Collections.Generic;
using UnityEngine;

public class GhostSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GhostAI ghostPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameMessageController messageController;

    [Header("Map")]
    [SerializeField] private Vector2 mapSize = new(100f, 100f);
    [SerializeField] private Vector2 mapCenter = Vector2.zero;
    [SerializeField] private float ghostHeight = 2f;

    [Header("Spawn Distance")]
    [SerializeField, Min(0f)] private float minDistanceFromPlayer = 18f;
    [SerializeField, Min(0f)] private float maxDistanceFromPlayer = 45f;
    [SerializeField, Min(1)] private int maxSpawnAttempts = 50;

    [Header("Difficulty")]
    [SerializeField] private float survivalDuration = 480f;
    [SerializeField, Min(0)] private int startingGhosts = 3;
    [SerializeField, Min(0)] private int finalGhosts = 10;
    [SerializeField, Min(0.1f)] private float respawnInterval = 3f;
    [SerializeField]
    private AnimationCurve difficultyCurve =
        AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Endless Mode")]
    [SerializeField, Min(1f)] private float endlessDifficultyDuration = 240f;

    private readonly List<GhostAI> activeGhosts = new();

    private float elapsedTime;
    private float spawnTimer;
    private bool endTriggered;

    public float SurvivalDuration => survivalDuration;
    public float ElapsedTime => elapsedTime;
    public bool IsEndless => survivalDuration < 0f;
    public bool IsIntroOver { get; set; }

    public float DifficultyProgress
    {
        get
        {
            float duration = IsEndless
                ? endlessDifficultyDuration
                : survivalDuration;

            return duration > 0f
                ? Mathf.Clamp01(elapsedTime / duration)
                : 1f;
        }
    }

    public int ActiveGhostCount
    {
        get
        {
            CleanupGhosts();
            return activeGhosts.Count;
        }
    }

    private void Awake()
    {
        var selectedTime = PlayerPrefs.GetInt("GameDuration", 4);

        if (selectedTime != 11)
        {
            survivalDuration = selectedTime * 60;
        }
        else
        {
            survivalDuration = -1;
        }
    }

    private void Start()
    {
        if (ghostPrefab == null || player == null)
        {
            Debug.LogError(
                "GhostSpawner: Missing prefab or player.",
                this
            );

            enabled = false;
            return;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;

        for (int i = 0; i < startingGhosts; i++)
        {
            if (!TrySpawnGhost())
                break;
        }
    }

    private void Update()
    {
        if (!IsIntroOver || endTriggered)
            return;

        elapsedTime += Time.deltaTime;

        // Only end the game in normal mode.
        if (!IsEndless && elapsedTime >= survivalDuration)
        {
            elapsedTime = survivalDuration;
            TriggerWin();
            return;
        }

        CleanupGhosts();

        int targetCount = GetTargetGhostCount();

        spawnTimer += Time.deltaTime;

        if (activeGhosts.Count >= targetCount)
        {
            spawnTimer = 0f;
            return;
        }

        if (spawnTimer >= respawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnGhost();
        }
    }

    private void TriggerWin()
    {
        if (endTriggered)
            return;

        endTriggered = true;

        foreach (var ghost in activeGhosts)
        {
            if (ghost != null)
                Destroy(ghost.gameObject);
        }

        activeGhosts.Clear();

        if (messageController != null)
            messageController.ShowWin();
    }

    private int GetTargetGhostCount()
    {
        float difficulty = Mathf.Clamp01(
            difficultyCurve.Evaluate(DifficultyProgress)
        );

        return Mathf.Max(0, Mathf.RoundToInt(
            Mathf.Lerp(startingGhosts, finalGhosts, difficulty)
        ));
    }

    private bool TrySpawnGhost()
    {
        for (int i = 0; i < maxSpawnAttempts; i++)
        {
            Vector3 position = RandomPosition();

            if (!IsValidSpawnPosition(position))
                continue;

            var ghost = Instantiate(
                ghostPrefab,
                position,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            );

            activeGhosts.Add(ghost);
            return true;
        }

        return false;
    }

    private Vector3 RandomPosition()
    {
        float x = Random.Range(
            mapCenter.x - mapSize.x * 0.5f,
            mapCenter.x + mapSize.x * 0.5f
        );

        float z = Random.Range(
            mapCenter.y - mapSize.y * 0.5f,
            mapCenter.y + mapSize.y * 0.5f
        );

        return new Vector3(x, ghostHeight, z);
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        Vector2 spawnXZ = new(position.x, position.z);
        Vector2 playerXZ = new(player.position.x, player.position.z);

        float distance = Vector2.Distance(spawnXZ, playerXZ);

        if (distance < minDistanceFromPlayer ||
            distance > maxDistanceFromPlayer)
        {
            return false;
        }

        if (playerCamera != null)
        {
            Vector3 viewport =
                playerCamera.WorldToViewportPoint(position);

            if (viewport.z > 0f &&
                viewport.x >= -0.1f && viewport.x <= 1.1f &&
                viewport.y >= -0.1f && viewport.y <= 1.1f)
            {
                return false;
            }
        }

        return true;
    }

    private void CleanupGhosts()
    {
        activeGhosts.RemoveAll(ghost => ghost == null);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireCube(
            new Vector3(mapCenter.x, ghostHeight, mapCenter.y),
            new Vector3(mapSize.x, 0.1f, mapSize.y)
        );
    }
}
