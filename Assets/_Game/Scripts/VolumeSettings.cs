using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MusicParameter = "MusicVolume";
    private const string SFXParameter = "SFXVolume";

    private void Start()
    {
        var musicVolume = PlayerPrefs.GetFloat(MusicParameter, .7f);
        var sfxVolume = PlayerPrefs.GetFloat(SFXParameter, .7f);

        musicSlider.SetValueWithoutNotify(musicVolume);
        sfxSlider.SetValueWithoutNotify(sfxVolume);

        SetMusicVolume(musicVolume);
        SetSFXVolume(sfxVolume);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    private void OnDestroy()
    {
        musicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        sfxSlider.onValueChanged.RemoveListener(SetSFXVolume);
    }

    public void SetMusicVolume(float volume)
    {
        SetMixerVolume(MusicParameter, volume);
    }

    public void SetSFXVolume(float volume)
    {
        SetMixerVolume(SFXParameter, volume);
    }

    private void SetMixerVolume(string parameter, float volume)
    {
        var decibels = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20f;

        audioMixer.SetFloat(parameter, decibels);

        PlayerPrefs.SetFloat(parameter, volume);
    }
}