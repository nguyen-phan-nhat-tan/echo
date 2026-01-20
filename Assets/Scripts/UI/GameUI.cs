using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance;

    [Header("HUD")]
    public TextMeshProUGUI timerText; 
    public Color warningColor = Color.red;
    public Color normalColor = Color.white;
    public GameObject pauseButton; 

    [Header("Cinematic Shutters")]
    public RectTransform topShutter;
    public RectTransform bottomShutter;
    public CanvasGroup shutterContent;
    public Image screenFlash; // Full-screen white Image for impact flash

    [Header("Pause Menu")]
    public CanvasGroup pauseGroup; 

    [Header("Shutter Content - Intro")]
    public TextMeshProUGUI introLoopText;
    public TextMeshProUGUI introWeaponText;

    [Header("Shutter Content - Win Summary")]
    public TextMeshProUGUI summaryScoreText; 
    public TextMeshProUGUI summaryTimeText;  
    public TextMeshProUGUI summaryTotalText; 
    public GameObject nextLoopButton;

    [Header("Shutter Content - Game Over")]
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalLoopText; 
    public GameObject newRecordVisual;
    public GameObject retryButton;
    public GameObject homeButton;

    // Internal tracking
    private enum ShutterState { None, Intro, Summary, GameOver }
    private ShutterState currentShutterState = ShutterState.None;
    
    void Awake()
    {
        Instance = this;
        
        if (pauseGroup) 
        { 
            pauseGroup.alpha = 0; 
            pauseGroup.blocksRaycasts = false; 
            pauseGroup.gameObject.SetActive(false); 
        }

        if (newRecordVisual != null) newRecordVisual.SetActive(false);

        // Start with shutters CLOSED (for Intro)
        if (topShutter) topShutter.anchoredPosition = Vector2.zero;
        if (bottomShutter) bottomShutter.anchoredPosition = Vector2.zero;
        
        // Hide all content initially
        HideAllShutterContent();
    }

    void HideAllShutterContent()
    {
        // Hide Intro
        if (introLoopText) introLoopText.gameObject.SetActive(false);
        if (introWeaponText) introWeaponText.gameObject.SetActive(false);
        
        // Hide Summary
        if (summaryScoreText) summaryScoreText.gameObject.SetActive(false);
        if (summaryTimeText) summaryTimeText.gameObject.SetActive(false);
        if (summaryTotalText) summaryTotalText.gameObject.SetActive(false);
        if (nextLoopButton) nextLoopButton.SetActive(false);
        
        // Hide Game Over
        if (finalScoreText) finalScoreText.gameObject.SetActive(false);
        if (finalLoopText) finalLoopText.gameObject.SetActive(false);
        if (newRecordVisual) newRecordVisual.SetActive(false);
        if (retryButton) retryButton.SetActive(false);
        if (homeButton) homeButton.SetActive(false);
        
        if (shutterContent)
        {
            shutterContent.alpha = 0;
            shutterContent.blocksRaycasts = false; // Allow input to pass through
        }
    }

    void ShowShutterContent(ShutterState state)
    {
        HideAllShutterContent();
        currentShutterState = state;
        
        switch (state)
        {
            case ShutterState.Intro:
                if (introLoopText) introLoopText.gameObject.SetActive(true);
                if (introWeaponText) introWeaponText.gameObject.SetActive(true);
                break;
            case ShutterState.Summary:
                if (summaryScoreText) summaryScoreText.gameObject.SetActive(true);
                if (summaryTimeText) summaryTimeText.gameObject.SetActive(true);
                if (summaryTotalText) summaryTotalText.gameObject.SetActive(true);
                if (nextLoopButton) nextLoopButton.SetActive(true);
                break;
            case ShutterState.GameOver:
                if (finalScoreText) finalScoreText.gameObject.SetActive(true);
                if (finalLoopText) finalLoopText.gameObject.SetActive(true);
                if (retryButton) retryButton.SetActive(true);
                if (homeButton) homeButton.SetActive(true);
                break;
        }
        
        if (shutterContent) 
        {
            shutterContent.alpha = 0;
            shutterContent.blocksRaycasts = true; // Enable button interaction
            shutterContent.DOFade(1f, 0.3f).SetDelay(0.3f);
        }
    }

    // --- PAUSE ---
    public void OnPausePressed()
    {
        if (GameManager.Instance == null) return;
        
        if (pauseGroup)
        {
            pauseGroup.gameObject.SetActive(true);
            pauseGroup.DOFade(1f, 0.2f).SetUpdate(true).SetLink(pauseGroup.gameObject);
            pauseGroup.blocksRaycasts = true;
        }
        
        if (pauseButton) pauseButton.SetActive(false);
        GameManager.Instance.TogglePause();
        
        Debug.Log("PAUSE BUTTON CLICKED!");
    }

    public void OnResumePressed()
    {
        if (GameManager.Instance == null) return;

        if (pauseGroup)
        {
            pauseGroup.DOFade(0f, 0.2f).SetUpdate(true).SetLink(pauseGroup.gameObject).OnComplete(() => 
            {
                pauseGroup.gameObject.SetActive(false);
            });
            pauseGroup.blocksRaycasts = false;
        }

        if (pauseButton) pauseButton.SetActive(true);
        GameManager.Instance.TogglePause();
    }

    public void OnHomePressed()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMenu();
    }

    // --- HUD ---
    public void UpdateTimer(float timeRemaining)
    {
        if (timerText == null) return;

        int seconds = Mathf.FloorToInt(timeRemaining % 60F);
        int milliseconds = Mathf.FloorToInt((timeRemaining * 100) % 100);
        timerText.text = string.Format("{0:00}:{1:00}", seconds, milliseconds); 

        if (timeRemaining <= 10f && timerText.color != warningColor)
        {
            timerText.color = warningColor;
            timerText.transform.DOScale(1.2f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(timerText.gameObject); 
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

    // --- INTRO (Shutters start CLOSED, show intro text, then OPEN) ---
    public void ShowLoopStart(int loopCount, string weaponName, Action onIntroFinished)
    {
        if (introLoopText) introLoopText.text = "LOOP " + loopCount;
        if (introWeaponText) introWeaponText.text = "WEAPON: " + weaponName;
        
        // Shutters are already closed from Awake or previous transition
        ShowShutterContent(ShutterState.Intro);
        
        // ELASTIC TEXT ANIMATION
        if (introLoopText)
        {
            introLoopText.transform.localScale = Vector3.zero;
            introLoopText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.3f);
        }
        if (introWeaponText)
        {
            introWeaponText.transform.localScale = Vector3.zero;
            introWeaponText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.5f);
        }
        
        // Sequence: Show content, wait, then open shutters
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.AppendInterval(1.5f); // Show intro for 1.5s
        seq.AppendCallback(() => {
            if (shutterContent) shutterContent.DOFade(0f, 0.2f);
        });
        seq.AppendInterval(0.3f);
        seq.AppendCallback(() => {
            OpenShutters();
            if (pauseButton) pauseButton.SetActive(true);
        });
        seq.AppendInterval(0.7f); // Wait for shutters to open
        seq.OnComplete(() => onIntroFinished?.Invoke());
    }

    // --- WIN SUMMARY (Close shutters, show summary) ---
    public void ShowWinSummary(int baseScore, float timeLeft, int totalScore)
    {
        if (pauseButton) pauseButton.SetActive(false);

        // Shutters should already be closing (called from GameManager)
        // Wait a moment then show content
        DOVirtual.DelayedCall(0.5f, () => {
            if (summaryScoreText) summaryScoreText.text = baseScore.ToString("N0");
            if (summaryTimeText) summaryTimeText.text = "+" + (timeLeft * 100).ToString("N0"); 
            if (summaryTotalText) summaryTotalText.text = "CALCULATING...";
            
            ShowShutterContent(ShutterState.Summary);

            if (summaryTotalText)
            {
                DOTween.To(() => baseScore, x => summaryTotalText.text = x.ToString("N0"), totalScore, 1f)
                    .SetDelay(0.5f) 
                    .SetEase(Ease.OutExpo)
                    .SetUpdate(true) 
                    .SetLink(gameObject);
            }
        });
    }

    public void HideSummary()
    {
        if (shutterContent) shutterContent.DOFade(0f, 0.2f);
    }
    
    // --- GAME OVER (Close shutters, show game over) ---
    public void ShowGameOver(int totalScore, int loopsSurvived, int highScore, bool isNewRecord)
    {
        if (pauseButton) pauseButton.SetActive(false);

        // Close shutters first
        CloseShutters();
        
        DOVirtual.DelayedCall(0.5f, () => {
            if (finalScoreText != null)
            {
                string textDisplay = "SCORE: " + totalScore.ToString("N0");
                if (isNewRecord) textDisplay += " <color=yellow>(NEW!)</color>";
                finalScoreText.text = textDisplay;
            }

            if (finalLoopText != null) finalLoopText.text = "LOOPS: " + loopsSurvived;

            if (newRecordVisual != null)
            {
                newRecordVisual.SetActive(isNewRecord);
                if(isNewRecord)
                    newRecordVisual.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetLink(newRecordVisual);
            }
            
            ShowShutterContent(ShutterState.GameOver);
        });
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --- SHUTTER CONTROLS ---
    public void CloseShutters(bool impactful = true)
    {
        // Screen flash for impact
        if (impactful && screenFlash != null)
        {
            screenFlash.color = new Color(1, 1, 1, 0.8f);
            screenFlash.DOFade(0f, 0.3f).SetUpdate(true);
        }
        
        // Fast slam (no overshoot)
        float duration = impactful ? 0.3f : 0.5f;
        Ease ease = impactful ? Ease.OutQuad : Ease.OutExpo;
        
        if (topShutter) topShutter.DOAnchorPosY(0, duration).SetEase(ease).SetUpdate(true);
        if (bottomShutter) bottomShutter.DOAnchorPosY(0, duration).SetEase(ease).SetUpdate(true);
    }

    public void OpenShutters()
    {
        HideAllShutterContent();
        if (topShutter) topShutter.DOAnchorPosY(topShutter.rect.height, 0.7f).SetEase(Ease.InExpo);
        if (bottomShutter) bottomShutter.DOAnchorPosY(-bottomShutter.rect.height, 0.7f).SetEase(Ease.InExpo);
    }
    
    public void ScreenFlash(Color color, float duration = 0.3f)
    {
        if (screenFlash == null) return;
        screenFlash.color = color;
        screenFlash.DOFade(0f, duration).SetUpdate(true);
    }

    public void OnNextLoopPressed()
    {
        HideSummary();
        if(GameManager.Instance != null)
            GameManager.Instance.ConfirmNextLoop();
    }
}