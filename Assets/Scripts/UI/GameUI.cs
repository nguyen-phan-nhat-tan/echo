using UnityEngine;
using TMPro;
using DG.Tweening;
using System;
using UnityEngine.SceneManagement;

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
    
    [Header("Game Over UI")]
    public CanvasGroup gameOverGroup;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalLoopText;
    
    void Awake()
    {
        Instance = this;
        
        introGroup.alpha = 0;
        summaryGroup.alpha = 0;
        gameOverGroup.alpha = 0;
        
        introGroup.blocksRaycasts = false;
        summaryGroup.blocksRaycasts = false;
        gameOverGroup.blocksRaycasts = false;
    }

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

    public void UpdateLoop(int loopCount) { }
    public void UpdateScore(int score) { }

    public void ShowLoopStart(int loopCount, string weaponName, Action onIntroFinished)
    {
        introLoopText.text = "LOOP " + loopCount;
        introWeaponText.text = "WEAPON: " + weaponName;
        introGroup.alpha = 1f;
        Sequence seq = DOTween.Sequence();
        seq.Append(introGroup.DOFade(1f, 0.5f));
        seq.AppendInterval(1.5f); 
        seq.Append(introGroup.DOFade(0f, 0.5f));
        seq.OnComplete(() => onIntroFinished?.Invoke());
    }

    public void ShowWinSummary(int baseScore, float timeLeft, int totalScore)
    {
        summaryGroup.blocksRaycasts = true; 

        summaryScoreText.text = baseScore.ToString("N0");
        summaryTimeText.text = "+" + (timeLeft * 100).ToString("N0"); 
        summaryTotalText.text = "CALCULATING...";
    
        summaryGroup.alpha = 1f;

        DOTween.To(() => baseScore, x => summaryTotalText.text = x.ToString("N0"), totalScore, 1f)
            .SetDelay(0.5f) 
            .SetEase(Ease.OutExpo);
    }

    public void HideSummary()
    {
        summaryGroup.blocksRaycasts = false; 
        
        summaryGroup.DOFade(0f, 0.2f);
    }
    
    public void ShowGameOver(int totalScore, int loopsSurvived)
    {
        gameOverGroup.blocksRaycasts = true;

        finalScoreText.text = "FINAL SCORE: " + totalScore.ToString("N0");
        finalLoopText.text = "GAME OVER";
        gameOverGroup.DOFade(1f, 1f).SetEase(Ease.OutExpo);
    }
    
    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}