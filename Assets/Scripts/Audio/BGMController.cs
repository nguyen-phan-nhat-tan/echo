using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class BGMController : MonoBehaviour
{
    public static BGMController Instance;
    
    [Header("Game Audio Clips")]
    public AudioClip introClip;
    public AudioClip loopClip;
    
    [Header("Menu Audio")]
    public AudioClip menuClip;
    public string menuSceneName = "MainMenu"; // Scene name that uses menu music
    
    [Header("Settings")]
    [Range(0f, 1f)]
    public float volume = 0.5f;
    public float fadeOutDuration = 2f;
    
    private AudioSource audioSource;
    private bool hasPlayedIntro = false;
    private float originalVolume;
    private bool isPlayingMenuMusic = false;
    
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
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        originalVolume = volume;
    }
    
    void OnEnable()
    {
        GameEvents.OnPlayerDeath += OnPlayerDeath;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        GameEvents.OnPlayerDeath -= OnPlayerDeath;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if this is the menu scene
        if (scene.name == menuSceneName)
        {
            PlayMenuMusic();
        }
        else
        {
            PlayGameMusic();
        }
    }
    
    void Start()
    {
        // Check initial scene
        if (SceneManager.GetActiveScene().name == menuSceneName)
            PlayMenuMusic();
        else
            PlayGameMusic();
    }
    
    public void PlayMenuMusic()
    {
        if (menuClip == null || isPlayingMenuMusic) return;
        
        audioSource.Stop();
        audioSource.volume = originalVolume;
        audioSource.pitch = 1f;
        audioSource.clip = menuClip;
        audioSource.loop = true;
        audioSource.Play();
        isPlayingMenuMusic = true;
        hasPlayedIntro = true;
    }
    
    public void PlayGameMusic()
    {
        isPlayingMenuMusic = false;
        PlayBGM();
    }
    
    public void PlayBGM()
    {
        // Reset volume and pitch in case they were modified
        audioSource.volume = originalVolume;
        audioSource.pitch = 1f;
        
        if (introClip != null)
        {
            audioSource.clip = introClip;
            audioSource.loop = false;
            audioSource.Play();
            hasPlayedIntro = false;
        }
        else if (loopClip != null)
        {
            // No intro, just play loop
            audioSource.clip = loopClip;
            audioSource.loop = true;
            audioSource.Play();
            hasPlayedIntro = true;
        }
    }
    
    void Update()
    {
        // Check if intro finished, then start loop
        if (!hasPlayedIntro && !audioSource.isPlaying && introClip != null)
        {
            hasPlayedIntro = true;
            
            if (loopClip != null)
            {
                audioSource.clip = loopClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
    }
    
    void OnPlayerDeath()
    {
        SlowMoPitch();
    }
    
    public void SlowMoPitch(float targetPitch = 0f, float duration = -1f)
    {
        if (duration < 0) duration = fadeOutDuration;
        
        // DJ scratch stop effect - pitch drops to 0
        DOTween.To(() => audioSource.pitch, x => audioSource.pitch = x, targetPitch, duration)
            .SetUpdate(true)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                if (targetPitch <= 0.1f) audioSource.Stop();
            });
        
        // Also fade volume slightly
        DOTween.To(() => audioSource.volume, x => audioSource.volume = x, originalVolume * 0.3f, duration)
            .SetUpdate(true);
    }
    
    public void FadeOut(float duration = -1f)
    {
        if (duration < 0) duration = fadeOutDuration;
        
        DOTween.To(() => audioSource.volume, x => audioSource.volume = x, 0f, duration)
            .SetUpdate(true) // Works even if game is paused
            .OnComplete(() => audioSource.Stop());
    }
    
    public void FadeIn(float duration = 1f)
    {
        audioSource.volume = 0f;
        DOTween.To(() => audioSource.volume, x => audioSource.volume = x, originalVolume, duration);
    }
    
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
        originalVolume = volume;
        if (audioSource != null)
            audioSource.volume = volume;
    }
    
    public void StopBGM()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
    
    public void PauseBGM()
    {
        if (audioSource != null)
            audioSource.Pause();
    }
    
    public void ResumeBGM()
    {
        if (audioSource != null)
            audioSource.UnPause();
    }
}
