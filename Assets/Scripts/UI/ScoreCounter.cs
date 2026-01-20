using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Animated score counter with smooth number counting effect.
/// </summary>
public class ScoreCounter : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI scoreText;
    
    [Header("Animation Settings")]
    public float countDuration = 1f;
    public Ease countEase = Ease.OutQuad;
    public string prefix = "";
    public string suffix = "";
    public string format = "N0"; // Number format (N0 = comma separated)
    
    [Header("Punch Effect")]
    public bool punchOnComplete = true;
    public float punchScale = 1.2f;
    public float punchDuration = 0.2f;
    
    private int currentDisplayValue = 0;
    private Tween countTween;
    
    void Awake()
    {
        if (scoreText == null)
            scoreText = GetComponent<TextMeshProUGUI>();
    }
    
    /// <summary>
    /// Animate counting from current value to target
    /// </summary>
    public void CountTo(int targetValue, float duration = -1f)
    {
        if (duration < 0) duration = countDuration;
        
        countTween?.Kill();
        
        countTween = DOTween.To(
            () => currentDisplayValue,
            x => {
                currentDisplayValue = x;
                UpdateText();
            },
            targetValue,
            duration
        )
        .SetEase(countEase)
        .SetUpdate(true)
        .OnComplete(() => {
            if (punchOnComplete)
                PunchScale();
        });
    }
    
    /// <summary>
    /// Set value instantly without animation
    /// </summary>
    public void SetValue(int value)
    {
        countTween?.Kill();
        currentDisplayValue = value;
        UpdateText();
    }
    
    /// <summary>
    /// Add to current value with animation
    /// </summary>
    public void AddValue(int amount, float duration = -1f)
    {
        CountTo(currentDisplayValue + amount, duration);
    }
    
    /// <summary>
    /// Animate from 0 to target
    /// </summary>
    public void CountFromZero(int targetValue, float duration = -1f)
    {
        currentDisplayValue = 0;
        UpdateText();
        CountTo(targetValue, duration);
    }
    
    private void UpdateText()
    {
        if (scoreText != null)
        {
            scoreText.text = prefix + currentDisplayValue.ToString(format) + suffix;
        }
    }
    
    private void PunchScale()
    {
        transform.DOScale(punchScale, punchDuration / 2f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() => {
                transform.DOScale(1f, punchDuration / 2f)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true);
            });
    }
    
    /// <summary>
    /// Get current displayed value
    /// </summary>
    public int GetCurrentValue() => currentDisplayValue;
}
