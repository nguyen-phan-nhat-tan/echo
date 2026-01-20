using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Reusable UI animation component for vectorheart-style effects.
/// Attach to any UI element to enable quick animations.
/// </summary>
public class UIAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float defaultDuration = 0.3f;
    public Ease defaultEase = Ease.OutQuad;
    
    [Header("Slide Settings")]
    public Vector2 slideOffset = new Vector2(-100f, 0f);
    
    [Header("Scale Settings")]
    public float scaleFrom = 0f;
    public Ease scaleEase = Ease.OutBack;
    
    [Header("Glow Settings")]
    public float glowIntensity = 1.2f;
    public float glowDuration = 0.5f;
    
    // Cached references
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image image;
    private TextMeshProUGUI tmpText;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Color originalColor;
    
    void Awake()
    {
        CacheComponents();
    }
    
    void CacheComponents()
    {
        if (rectTransform != null) return; // Already cached
        
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
        tmpText = GetComponent<TextMeshProUGUI>();
        
        if (rectTransform) originalPosition = rectTransform.anchoredPosition;
        originalScale = transform.localScale;
        
        if (image) originalColor = image.color;
        else if (tmpText) originalColor = tmpText.color;
    }
    
    void EnsureCached()
    {
        if (rectTransform == null) CacheComponents();
    }
    
    // --- SLIDE IN ---
    public Tween SlideIn(float duration = -1f, Ease ease = Ease.Unset)
    {
        EnsureCached();
        if (rectTransform == null) return null;
        
        if (duration < 0) duration = defaultDuration;
        if (ease == Ease.Unset) ease = defaultEase;
        
        // Start off-screen
        rectTransform.anchoredPosition = originalPosition + slideOffset;
        
        // Slide to original
        return rectTransform.DOAnchorPos(originalPosition, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }
    
    public Tween SlideOut(float duration = -1f, Ease ease = Ease.Unset)
    {
        EnsureCached();
        if (rectTransform == null) return null;
        
        if (duration < 0) duration = defaultDuration;
        if (ease == Ease.Unset) ease = Ease.InQuad;
        
        return rectTransform.DOAnchorPos(originalPosition + slideOffset, duration)
            .SetEase(ease)
            .SetUpdate(true);
    }
    
    // --- SCALE POP ---
    public Tween ScalePop(float duration = -1f)
    {
        EnsureCached();
        if (duration < 0) duration = defaultDuration;
        
        transform.localScale = Vector3.one * scaleFrom;
        
        return transform.DOScale(originalScale, duration)
            .SetEase(scaleEase)
            .SetUpdate(true);
    }
    
    public Tween ScaleOut(float duration = -1f)
    {
        EnsureCached();
        if (duration < 0) duration = defaultDuration;
        
        return transform.DOScale(0f, duration)
            .SetEase(Ease.InBack)
            .SetUpdate(true);
    }
    
    // --- FADE ---
    public Tween FadeIn(float duration = -1f)
    {
        EnsureCached();
        if (duration < 0) duration = defaultDuration;
        
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            return canvasGroup.DOFade(1f, duration).SetUpdate(true);
        }
        else if (image)
        {
            image.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            return image.DOFade(originalColor.a, duration).SetUpdate(true);
        }
        else if (tmpText)
        {
            tmpText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            return tmpText.DOFade(originalColor.a, duration).SetUpdate(true);
        }
        return null;
    }
    
    public Tween FadeOut(float duration = -1f)
    {
        EnsureCached();
        if (duration < 0) duration = defaultDuration;
        
        if (canvasGroup)
            return canvasGroup.DOFade(0f, duration).SetUpdate(true);
        else if (image)
            return image.DOFade(0f, duration).SetUpdate(true);
        else if (tmpText)
            return tmpText.DOFade(0f, duration).SetUpdate(true);
        return null;
    }
    
    // --- GLOW PULSE ---
    public Tween GlowPulse(bool loop = true)
    {
        EnsureCached();
        if (image)
        {
            return image.DOColor(originalColor * glowIntensity, glowDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(loop ? -1 : 2, LoopType.Yoyo)
                .SetUpdate(true);
        }
        else if (tmpText)
        {
            return tmpText.DOColor(originalColor * glowIntensity, glowDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(loop ? -1 : 2, LoopType.Yoyo)
                .SetUpdate(true);
        }
        return null;
    }
    
    public void StopGlow()
    {
        DOTween.Kill(image);
        DOTween.Kill(tmpText);
        if (image) image.color = originalColor;
        if (tmpText) tmpText.color = originalColor;
    }
    
    // --- COMBINED ENTRANCE ---
    public Sequence EnterWithSlideAndFade(float duration = -1f)
    {
        EnsureCached();
        if (rectTransform == null) return null;
        
        if (duration < 0) duration = defaultDuration;
        
        Sequence seq = DOTween.Sequence();
        
        // Start state
        rectTransform.anchoredPosition = originalPosition + slideOffset;
        if (canvasGroup) canvasGroup.alpha = 0f;
        
        // Animate
        seq.Append(rectTransform.DOAnchorPos(originalPosition, duration).SetEase(defaultEase));
        if (canvasGroup) seq.Join(canvasGroup.DOFade(1f, duration));
        
        seq.SetUpdate(true);
        return seq;
    }
    
    public Sequence EnterWithScaleAndFade(float duration = -1f)
    {
        EnsureCached();
        if (duration < 0) duration = defaultDuration;
        
        Sequence seq = DOTween.Sequence();
        
        // Start state
        transform.localScale = Vector3.one * scaleFrom;
        if (canvasGroup) canvasGroup.alpha = 0f;
        
        // Animate
        seq.Append(transform.DOScale(originalScale, duration).SetEase(scaleEase));
        if (canvasGroup) seq.Join(canvasGroup.DOFade(1f, duration));
        
        seq.SetUpdate(true);
        return seq;
    }
    
    // --- RESET ---
    public void ResetToOriginal()
    {
        EnsureCached();
        if (rectTransform) rectTransform.anchoredPosition = originalPosition;
        transform.localScale = originalScale;
        if (image) image.color = originalColor;
        if (tmpText) tmpText.color = originalColor;
        if (canvasGroup) canvasGroup.alpha = 1f;
    }
}
