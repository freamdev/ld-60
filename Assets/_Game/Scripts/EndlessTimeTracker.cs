using TMPro;
using UnityEngine;

public class EndlessTimeTracker : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GhostSpawner ghostSpawner;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float rollDuration = 0.5f;

    private float elapsedTime;
    private int displayedSeconds = -1;

    private void Start()
    {
        if (!ghostSpawner.IsEndless)
        {
            Destroy(gameObject);
        }

        elapsedTime = 0f;
        UpdateTimer(0);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        int totalSeconds = Mathf.FloorToInt(elapsedTime);

        if (totalSeconds != displayedSeconds)
        {
            UpdateTimer(totalSeconds);
        }
    }

    private void UpdateTimer(int totalSeconds)
    {
        displayedSeconds = totalSeconds;

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public float ElapsedTime => elapsedTime;
}
