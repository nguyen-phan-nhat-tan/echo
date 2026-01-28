using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SettingsMenu : MonoBehaviour
{
    [Header("Audio")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("Video")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    private Resolution[] resolutions;

    void Start()
    {
        // Initialize UI values from Manager
        if (SettingsManager.Instance != null)
        {
            // Audio Defaults
            if (masterSlider)
            {
                masterSlider.value = SettingsManager.Instance.GetMasterVolume();
                masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            
            if (musicSlider)
            {
                musicSlider.value = SettingsManager.Instance.GetMusicVolume();
                musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxSlider)
            {
                sfxSlider.value = SettingsManager.Instance.GetSFXVolume();
                sfxSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
            
            // Video Defaults
            InitializeVideoSettings();
        }
    }

    private void InitializeVideoSettings()
    {
        // Fullscreen
        if (fullscreenToggle)
        {
            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }

        // Resolution
        if (resolutionDropdown)
        {
            resolutions = Screen.resolutions.Select(r => new Resolution { width = r.width, height = r.height }).Distinct().ToArray();
            resolutionDropdown.ClearOptions();

            List<string> options = new List<string>();
            int currentResolutionIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);

                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();

            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }
    }

    // --- EVENTS ---

    public void OnMasterVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMasterVolume(value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        SettingsManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        SettingsManager.Instance.SetSFXVolume(value);
    }

    public void OnFullscreenToggled(bool isFullscreen)
    {
        SettingsManager.Instance.SetFullscreen(isFullscreen);
    }

    public void OnResolutionChanged(int index)
    {
        Resolution resolution = resolutions[index];
        SettingsManager.Instance.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void CloseSettings()
    {
        // Save on Close
        if (SettingsManager.Instance != null) SettingsManager.Instance.SaveSettings();
        
        gameObject.SetActive(false);
    }
}
