using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Animated scanline overlay for vectorheart/retro effect.
/// Attach to a full-screen UI Image with a scanline texture.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ScanlineOverlay : MonoBehaviour
{
    [Header("Scroll Settings")]
    public float scrollSpeed = 0.1f;
    public Vector2 scrollDirection = new Vector2(0f, 1f);
    
    [Header("Visibility")]
    [Range(0f, 1f)]
    public float intensity = 0.3f;
    public bool animateIntensity = false;
    public float intensityPulseSpeed = 1f;
    
    private RawImage rawImage;
    private Vector2 offset;
    private float baseIntensity;
    
    void Awake()
    {
        rawImage = GetComponent<RawImage>();
        baseIntensity = intensity;
        
        // Make sure raycast is disabled
        rawImage.raycastTarget = false;
        
        UpdateAlpha();
    }
    
    void Update()
    {
        // Scroll the texture
        offset += scrollDirection * scrollSpeed * Time.unscaledDeltaTime;
        rawImage.uvRect = new Rect(offset, Vector2.one);
        
        // Pulse intensity
        if (animateIntensity)
        {
            intensity = baseIntensity + Mathf.Sin(Time.unscaledTime * intensityPulseSpeed) * 0.1f;
            UpdateAlpha();
        }
    }
    
    void UpdateAlpha()
    {
        Color c = rawImage.color;
        c.a = intensity;
        rawImage.color = c;
    }
    
    public void SetIntensity(float value)
    {
        intensity = Mathf.Clamp01(value);
        baseIntensity = intensity;
        UpdateAlpha();
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
    }
    
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
