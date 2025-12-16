using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
    
public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    [Header("HUD (Always Visible)")]
    public TextMeshProUGUI timerText; 
    public Color warningColor = Color.red;
    public Color normalColor = Color.white;

    [Header("Loop Intro UI")]
    public CanvasGroup introGroup; 
    public TextMeshProUGUI introLoopText;
    public TextMeshProUGUI introWeaponText;

    [Header("Win Summary UI")]
    public CanvasGroup summaryGroup; 
    public TextMeshProUGUI summaryScoreText; 
    public TextMeshProUGUI summaryTimeText;  
    public TextMeshProUGUI summaryTotalText; 

    void Awake()
    {
        Instance = this;
        
        // 1. INITIAL SETUP: Hide everything and UNBLOCK input
        introGroup.alpha = 0;
        introGroup.blocksRaycasts = false; // Crucial: Mouse clicks pass through

        summaryGroup.alpha = 0;
        summaryGroup.blocksRaycasts = false; // Crucial: Mouse clicks pass through
    }

    // --- HUD METHODS (Keep these to fix GameManager errors) ---

    public void UpdateTimer(float timeRemaining)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(timeRemaining / 60F);
        int seconds = Mathf.FloorToInt(timeRemaining % 60F);
        int milliseconds = Mathf.FloorToInt((timeRemaining * 100) % 100);
        timerText.text = string.Format("{0:00}:{1:00}", seconds, milliseconds); 

        if (timeRemaining <= 10f && timerText.color != warningColor)
        {
            timerText.color = warningColor;
            timerText.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo);
        }
        else if (timeRemaining > 10f && timerText.color != normalColor)
        {
            timerText.color = normalColor;
            timerText.transform.DOKill();
            timerText.transform.localScale = Vector3.one;
        }
    }

    public void UpdateLoop(int loopCount) { } // Hidden intentionally
    public void UpdateScore(int score) { }    // Hidden intentionally

    // ---------------------------------------------------------

    public void ShowLoopStart(int loopCount, string weaponName, Action onIntroFinished)
    {
        introLoopText.text = "LOOP " + loopCount;
        introWeaponText.text = "WEAPON: " + weaponName;
        
        Sequence seq = DOTween.Sequence();
        seq.Append(introGroup.DOFade(1f, 0.5f));
        seq.AppendInterval(1.5f); 
        seq.Append(introGroup.DOFade(0f, 0.5f));
        seq.OnComplete(() => onIntroFinished?.Invoke());
    }

    public void ShowWinSummary(int baseScore, float timeLeft, int totalScore)
    {
        // 2. BLOCK INPUT: Player cannot move while reading stats
        summaryGroup.blocksRaycasts = true; 

        summaryScoreText.text = baseScore.ToString("N0");
        summaryTimeText.text = "+" + (timeLeft * 100).ToString("N0"); 
        summaryTotalText.text = "CALCULATING...";

        summaryGroup.DOFade(1f, 0.2f);

        DOTween.To(() => baseScore, x => summaryTotalText.text = x.ToString("N0"), totalScore, 1f)
            .SetDelay(0.5f) 
            .SetEase(Ease.OutExpo);
    }

    public void HideSummary()
    {
        // 3. UNBLOCK INPUT: Restore joystick control
        summaryGroup.blocksRaycasts = false; 
        
        summaryGroup.DOFade(0f, 0.2f);
    }
}