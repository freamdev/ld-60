
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameMessageController : MonoBehaviour
{
    [System.Serializable]
    public class MessagePanel
    {
        public CanvasGroup canvasGroup;
        public TextMeshProUGUI text;
    }

    [Header("Message Panels")]
    [SerializeField] private MessagePanel startPanel;
    [SerializeField] private MessagePanel winPanel;
    [SerializeField] private MessagePanel losePanel;
    [SerializeField] private MessagePanel endlessLosePanel;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float typingDuration = 3f;
    [SerializeField] private float startHoldDuration = 2f;

    [Header("References")]
    [SerializeField] private GhostSpawner spawner;
    [SerializeField] private PlayerLocomotion playerMovement;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private EndlessTimeTracker endlessTimeTracker;

    private LD60_InputSystem input;
    private bool canReturnToMenu;
    private Coroutine currentAnimation;

    private string endlessLoseTemplate;

    private void Awake()
    {
        input = new LD60_InputSystem();

        endlessLoseTemplate = endlessLosePanel.text.text;

        DisableUser();

        startPanel.canvasGroup.alpha = 1f;
        HideImmediately(winPanel);
        HideImmediately(losePanel);
        HideImmediately(endlessLosePanel);
    }

    private void OnEnable()
    {
        input.Player.Skip.performed += OnSkip;
        input.Player.Skip.Enable();
    }

    private void OnDisable()
    {
        input.Player.Skip.performed -= OnSkip;
        input.Player.Skip.Disable();
    }

    private void OnSkip(InputAction.CallbackContext context)
    {
        if (!canReturnToMenu)
            return;

        canReturnToMenu = false;
        SceneManager.LoadScene(0);
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    private void Start()
    {
        ShowStart();
    }

    public void ShowStart()
    {
        PlayMessage(startPanel, true, true);
    }

    public void ShowWin()
    {
        DisableUser();

        PlayerPrefs.SetInt("win", 1);
        PlayerPrefs.Save();

        PlayMessage(winPanel, false, false);
    }

    public void ShowLose()
    {
        DisableUser();

        var selectedTime = PlayerPrefs.GetInt("GameDuration", 4);

        if (selectedTime == 11)
        {
            UpdateEndlessLoseText();
            PlayMessage(endlessLosePanel, false, false);
        }
        else
        {
            PlayMessage(losePanel, false, false);
        }
    }

    private void UpdateEndlessLoseText()
    {
        int totalSeconds = Mathf.FloorToInt(
            endlessTimeTracker.ElapsedTime
        );

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        endlessLosePanel.text.text = endlessLoseTemplate
            .Replace("{mm}", minutes.ToString())
            .Replace("{ss}", seconds.ToString("D2"));
    }

    private void EnableUser()
    {
        playerMovement.GameEnded = false;
        playerAttack.GameEnded = false;
        playerHealth.GameEnded = false;
    }

    private void DisableUser()
    {
        playerMovement.GameEnded = true;
        playerAttack.GameEnded = true;
        playerHealth.GameEnded = true;
    }

    private void PlayMessage(
        MessagePanel panel,
        bool autoHide,
        bool enableSpawns)
    {
        canReturnToMenu = false;

        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        HideImmediately(startPanel);
        HideImmediately(winPanel);
        HideImmediately(losePanel);
        HideImmediately(endlessLosePanel);

        currentAnimation = StartCoroutine(
            AnimateMessage(panel, autoHide, enableSpawns)
        );
    }

    private IEnumerator AnimateMessage(
        MessagePanel panel,
        bool autoHide,
        bool enableSpawns)
    {
        var group = panel.canvasGroup;
        var text = panel.text;

        var elapsed = 0f;

        text.ForceMeshUpdate();
        var characterCount = text.textInfo.characterCount;
        text.maxVisibleCharacters = 0;

        if (!autoHide)
        {
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = true;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Clamp01(
                    elapsed / fadeDuration
                );
                yield return null;
            }
        }

        group.alpha = 1f;
        elapsed = 0f;

        while (elapsed < typingDuration)
        {
            elapsed += Time.deltaTime;

            var progress = Mathf.Clamp01(
                elapsed / typingDuration
            );

            text.maxVisibleCharacters = Mathf.FloorToInt(
                characterCount * progress
            );

            yield return null;
        }

        text.maxVisibleCharacters = int.MaxValue;

        if (autoHide)
        {
            yield return new WaitForSeconds(startHoldDuration);

            elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;

                group.alpha = 1f -
                    Mathf.Clamp01(elapsed / fadeDuration);

                yield return null;
            }

            HideImmediately(panel);
        }
        else
        {
            canReturnToMenu = true;
        }

        if (enableSpawns)
        {
            spawner.IsIntroOver = true;
            EnableUser();
        }

        currentAnimation = null;
    }

    private void HideImmediately(MessagePanel panel)
    {
        if (panel.canvasGroup == null)
            return;

        panel.canvasGroup.alpha = 0f;
        panel.canvasGroup.blocksRaycasts = false;
        panel.canvasGroup.interactable = false;
    }
}
