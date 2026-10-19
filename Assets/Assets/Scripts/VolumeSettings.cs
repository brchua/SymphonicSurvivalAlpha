using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MusicKey = "MusicVolume";
    private const string SfxKey = "SFXVolume";

    private void Start()
    {
        musicSlider.minValue = 0.0001f;
        sfxSlider.minValue = 0.0001f;
        musicSlider.maxValue = 1f;
        sfxSlider.maxValue = 1f;

        musicSlider.value = PlayerPrefs.GetFloat(MusicKey, 1f);
        sfxSlider.value = PlayerPrefs.GetFloat(SfxKey, 1f);

        SetMusicVolume(musicSlider.value);
        SetSfxVolume(sfxSlider.value);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    public void SetMusicVolume(float value)
    {
        mixer.SetFloat(MusicKey, Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(MusicKey, value);
    }

    public void SetSfxVolume(float value)
    {
        mixer.SetFloat(SfxKey, Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(SfxKey, value);
    }
}