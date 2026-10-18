
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuControls : MonoBehaviour
{
    [Header("Help")]
    [SerializeField] private CanvasGroup helpPanel;

    [Header("Game Duration")]
    [SerializeField] private Slider durationSlider;
    [SerializeField] private TextMeshProUGUI durationText;

    public int SelectedDuration => Mathf.RoundToInt(durationSlider.value);
    public bool IsEndless => SelectedDuration == 11;

    private void Awake()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        HideHelp();

        durationSlider.minValue = 4;
        durationSlider.maxValue = 11;
        durationSlider.wholeNumbers = true;

        UpdateDurationText();
    }

    private void Update()
    {
        UpdateDurationText();
    }

    private void UpdateDurationText()
    {
        int duration = SelectedDuration;

        durationText.text = duration == 11 ? "Endless" : $"{duration} min";
    }

    public void StartGame()
    {
        PlayerPrefs.SetInt("GameDuration", SelectedDuration);
        PlayerPrefs.Save();

        SceneManager.LoadScene(1);
    }

    public void ShowHelp()
    {
        helpPanel.alpha = 1f;
        helpPanel.blocksRaycasts = true;
        helpPanel.interactable = true;
    }

    public void HideHelp()
    {
        helpPanel.alpha = 0f;
        helpPanel.blocksRaycasts = false;
        helpPanel.interactable = false;
    }
}
