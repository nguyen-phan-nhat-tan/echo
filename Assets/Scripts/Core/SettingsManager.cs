using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("Audio Settings")]
    public AudioMixer mainMixer;
    public const string MASTER_VOL = "MasterVolume";
    public const string MUSIC_VOL = "MusicVolume";
    public const string SFX_VOL = "SFXVolume";

    [Header("Defaults")]
    public float defaultVolume = 0.8f;

    // Runtime State
    private float currentMasterVol;
    private float currentMusicVol;
    private float currentSFXVol;
    
    // Resolution State
    private Resolution[] resolutions;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadSettings();
    }

    void Start()
    {
        // Apply loaded settings again to ensure Mixer is ready (sometimes Awake is too early for Mixer)
        ApplyAudioSettings(); 
    }

    private void LoadSettings()
    {
        // 1. Audio
        currentMasterVol = PlayerPrefs.GetFloat(MASTER_VOL, defaultVolume);
        currentMusicVol = PlayerPrefs.GetFloat(MUSIC_VOL, defaultVolume);
        currentSFXVol = PlayerPrefs.GetFloat(SFX_VOL, defaultVolume);
        
        ApplyAudioSettings();

        // 2. Video (Resolution/Fullscreen)
        // Unity automatically saves resolution/fullscreen in PlayerPrefs on Windows usually, 
        // but we can enforce it if needed. For now, rely on Unity's built-in persistence for window mode,
        // or add explicit saving if the user requests it.
    }

    public void ApplyAudioSettings()
    {
        if (mainMixer == null) return;

        SetMixerVolume(MASTER_VOL, currentMasterVol);
        SetMixerVolume(MUSIC_VOL, currentMusicVol);
        SetMixerVolume(SFX_VOL, currentSFXVol);
    }

    // Helper: Converts 0-1 slider value to -80dB to 0dB logarithmic
    private void SetMixerVolume(string paramName, float normalizedValue)
    {
        // Formula: log10(value) * 20. 
        // If value is 0, we set to -80dB (effectively mute)
        float dbValue = (normalizedValue <= 0.001f) ? -80f : Mathf.Log10(normalizedValue) * 20f;
        mainMixer.SetFloat(paramName, dbValue);
    }

    // --- PUBLIC SETTERS (Called from UI) ---

    public void SetMasterVolume(float value)
    {
        currentMasterVol = value;
        SetMixerVolume(MASTER_VOL, value);
        PlayerPrefs.SetFloat(MASTER_VOL, value);
    }

    public void SetMusicVolume(float value)
    {
        currentMusicVol = value;
        SetMixerVolume(MUSIC_VOL, value);
        PlayerPrefs.SetFloat(MUSIC_VOL, value);
    }

    public void SetSFXVolume(float value)
    {
        currentSFXVol = value;
        SetMixerVolume(SFX_VOL, value);
        PlayerPrefs.SetFloat(SFX_VOL, value);
    }
    
    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }

    // --- GETTERS ---
    public float GetMasterVolume() => currentMasterVol;
    public float GetMusicVolume() => currentMusicVol;
    public float GetSFXVolume() => currentSFXVol;

    // --- RESOLUTION HELPERS ---
    public Resolution[] GetResolutions()
    {
        if (resolutions == null) resolutions = Screen.resolutions;
        return resolutions;
    }

    public void SetResolution(int width, int height, bool isFullscreen)
    {
        Screen.SetResolution(width, height, isFullscreen);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}
