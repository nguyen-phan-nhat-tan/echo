using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RewindFX : MonoBehaviour
{
    public static RewindFX Instance;
    
    [Header("UI Overlay")]
    public Image noiseOverlay;
    public float overlayAlpha = 0.3f;
    
    [Header("Post-Processing (Optional)")]
    public Material vhsMaterial;
    
    [Header("Animation")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.5f;
    
    void Awake()
    {
        Instance = this;
        
        if (noiseOverlay) 
        {
            Color c = noiseOverlay.color;
            c.a = 0;
            noiseOverlay.color = c;
        }
    }
    
    void OnEnable()
    {
        GameEvents.OnStateChanged += OnGameStateChanged;
    }
    
    void OnDisable()
    {
        GameEvents.OnStateChanged -= OnGameStateChanged;
    }
    
    void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Rewinding)
        {
            ShowRewindEffect();
        }
        else if (state == GameState.Playing || state == GameState.Intro)
        {
            HideRewindEffect();
        }
    }
    
    public void ShowRewindEffect()
    {
        // UI Overlay
        if (noiseOverlay)
        {
            noiseOverlay.DOFade(overlayAlpha, fadeInDuration);
        }
        
        // VHS Material (if using Blit)
        if (vhsMaterial)
        {
            DOTween.To(() => vhsMaterial.GetFloat("_NoiseStrength"), 
                       x => vhsMaterial.SetFloat("_NoiseStrength", x), 
                       0.15f, fadeInDuration);
            DOTween.To(() => vhsMaterial.GetFloat("_ScanlineStrength"), 
                       x => vhsMaterial.SetFloat("_ScanlineStrength", x), 
                       0.4f, fadeInDuration);
            DOTween.To(() => vhsMaterial.GetFloat("_ChromaticStrength"), 
                       x => vhsMaterial.SetFloat("_ChromaticStrength", x), 
                       0.01f, fadeInDuration);
        }
    }
    
    public void HideRewindEffect()
    {
        // UI Overlay
        if (noiseOverlay)
        {
            noiseOverlay.DOFade(0f, fadeOutDuration);
        }
        
        // VHS Material
        if (vhsMaterial)
        {
            DOTween.To(() => vhsMaterial.GetFloat("_NoiseStrength"), 
                       x => vhsMaterial.SetFloat("_NoiseStrength", x), 
                       0f, fadeOutDuration);
            DOTween.To(() => vhsMaterial.GetFloat("_ScanlineStrength"), 
                       x => vhsMaterial.SetFloat("_ScanlineStrength", x), 
                       0f, fadeOutDuration);
            DOTween.To(() => vhsMaterial.GetFloat("_ChromaticStrength"), 
                       x => vhsMaterial.SetFloat("_ChromaticStrength", x), 
                       0f, fadeOutDuration);
        }
    }
}
