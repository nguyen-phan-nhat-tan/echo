using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Chain multiple UIAnimator animations with configurable delays.
/// Attach to a parent container to animate children in sequence.
/// </summary>
public class UISequence : MonoBehaviour
{
    [Header("Sequence Settings")]
    public float staggerDelay = 0.1f; // Delay between each item
    public bool playOnEnable = false;
    public AnimationType animationType = AnimationType.SlideIn;
    
    [Header("Items (auto-populated if empty)")]
    public List<UIAnimator> items = new List<UIAnimator>();
    
    public enum AnimationType
    {
        SlideIn,
        ScalePop,
        FadeIn,
        SlideAndFade,
        ScaleAndFade
    }
    
    private Sequence currentSequence;
    private Coroutine playCoroutine;
    
    void OnEnable()
    {
        if (playOnEnable)
        {
            // Delay by one frame to ensure child UIAnimators are ready
            playCoroutine = StartCoroutine(PlaySequenceDelayed());
        }
    }
    
    IEnumerator PlaySequenceDelayed()
    {
        yield return null; // Wait one frame
        PlaySequence();
    }
    
    void OnDisable()
    {
        if (playCoroutine != null)
            StopCoroutine(playCoroutine);
        StopSequence();
    }
    
    /// <summary>
    /// Auto-populate items from children with UIAnimator components
    /// </summary>
    public void AutoPopulateItems()
    {
        items.Clear();
        UIAnimator[] animators = GetComponentsInChildren<UIAnimator>();
        items.AddRange(animators);
    }
    
    /// <summary>
    /// Play the staggered animation sequence
    /// </summary>
    public void PlaySequence()
    {
        StopSequence();
        
        if (items.Count == 0)
            AutoPopulateItems();
        
        if (items.Count == 0)
            return;
        
        currentSequence = DOTween.Sequence();
        
        for (int i = 0; i < items.Count; i++)
        {
            UIAnimator animator = items[i];
            if (animator == null) continue;
            
            float delay = i * staggerDelay;
            
            switch (animationType)
            {
                case AnimationType.SlideIn:
                    currentSequence.Insert(delay, animator.SlideIn());
                    break;
                case AnimationType.ScalePop:
                    currentSequence.Insert(delay, animator.ScalePop());
                    break;
                case AnimationType.FadeIn:
                    currentSequence.Insert(delay, animator.FadeIn());
                    break;
                case AnimationType.SlideAndFade:
                    currentSequence.Insert(delay, animator.EnterWithSlideAndFade());
                    break;
                case AnimationType.ScaleAndFade:
                    currentSequence.Insert(delay, animator.EnterWithScaleAndFade());
                    break;
            }
        }
        
        currentSequence.SetUpdate(true);
    }
    
    /// <summary>
    /// Play reverse animation to hide items
    /// </summary>
    public void PlayReverseSequence()
    {
        StopSequence();
        
        if (items.Count == 0)
            return;
        
        currentSequence = DOTween.Sequence();
        
        // Reverse order for exit
        for (int i = items.Count - 1; i >= 0; i--)
        {
            UIAnimator animator = items[i];
            if (animator == null) continue;
            
            float delay = (items.Count - 1 - i) * staggerDelay;
            
            switch (animationType)
            {
                case AnimationType.SlideIn:
                    currentSequence.Insert(delay, animator.SlideOut());
                    break;
                case AnimationType.ScalePop:
                    currentSequence.Insert(delay, animator.ScaleOut());
                    break;
                case AnimationType.FadeIn:
                    currentSequence.Insert(delay, animator.FadeOut());
                    break;
                default:
                    currentSequence.Insert(delay, animator.FadeOut());
                    break;
            }
        }
        
        currentSequence.SetUpdate(true);
    }
    
    public void StopSequence()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
    }
    
    /// <summary>
    /// Reset all items to their original state
    /// </summary>
    public void ResetAll()
    {
        foreach (var animator in items)
        {
            if (animator != null)
                animator.ResetToOriginal();
        }
    }
}
