using UnityEngine;

public class MenuAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Min(0f)] private float fadeOutDuration = 3f;

    private float originalVolume;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null || audioSource.clip == null)
            return;

        originalVolume = audioSource.volume;
        audioSource.loop = false;
    }

    private void Update()
    {
        if (audioSource == null || audioSource.clip == null || !audioSource.isPlaying)
            return;

        var remainingTime = audioSource.clip.length - audioSource.time;

        if (remainingTime <= fadeOutDuration)
        {
            var fade = fadeOutDuration > 0f ? Mathf.Clamp01(remainingTime / fadeOutDuration) : 0f;

            audioSource.volume = originalVolume * fade;
        }
    }
}