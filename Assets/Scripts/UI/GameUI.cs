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
    public GameObject settingsMenu; // NEW

    [Header("Shutter Content - Intro")]
    public TextMeshProUGUI introLoopText;
    public TextMeshProUGUI introWeaponText;
    public TextMeshProUGUI introDebuffText; // NEW

    [Header("Shutter Content - Win Summary")]
    public TextMeshProUGUI summaryScoreText; 
    public TextMeshProUGUI summaryTimeText;  
    public TextMeshProUGUI summaryTotalText; 
    public TextMeshProUGUI summaryDebuffText; // NEW
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
        if (introDebuffText) introDebuffText.gameObject.SetActive(false); // NEW
        
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
                if (introDebuffText) introDebuffText.gameObject.SetActive(true); // NEW
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

        // Ensure settings are closed
        if (settingsMenu != null) settingsMenu.SetActive(false);

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
    
    public void OnSettingsPressed()
    {
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(true);
        }
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
    public void UpdateScore(float score) { }

    // --- INTRO (Shutters start CLOSED, show intro text, then OPEN) ---
    public void ShowLoopStart(int loopCount, string weaponName, string debuffName, Action onIntroFinished)
    {
        if (introLoopText) introLoopText.text = "LOOP " + loopCount;
        if (introWeaponText) introWeaponText.text = "WEAPON: " + weaponName;
        
        // Handle Debuff Text
        if (introDebuffText)
        {
            if (!string.IsNullOrEmpty(debuffName))
            {
                introDebuffText.text = "CLAUSE: " + debuffName;
                introDebuffText.gameObject.SetActive(true);
            }
            else
            {
                introDebuffText.text = "";
                introDebuffText.gameObject.SetActive(false);
            }
        }
        
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
        if (introDebuffText && introDebuffText.gameObject.activeSelf)
        {
            introDebuffText.transform.localScale = Vector3.zero;
            introDebuffText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.7f); // Delay slightly more
        }
        
        // Sequence: Show content, wait, then open shutters
        Sequence seq = DOTween.Sequence().SetLink(gameObject);
        seq.AppendInterval(1.8f); // Show intro slightly longer to read debuff
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
    public void ShowWinSummary(float baseScore, float timeLeft, float totalScore, string debuffName)
    {
        if (pauseButton) pauseButton.SetActive(false);

        // Shutters should already be closing (called from GameManager)
        // Wait a moment then show content
        DOVirtual.DelayedCall(0.5f, () => {
             // Summary: Show Base (F0?) or F2? User cares about decimal on total.
            if (summaryScoreText) summaryScoreText.text = baseScore.ToString("F2");
            if (summaryTimeText) summaryTimeText.text = "+" + timeLeft.ToString("F2"); 
            
            // SHOW DEBUFF IF EXISTS
            if (summaryDebuffText)
            {
                if (!string.IsNullOrEmpty(debuffName))
                {
                    summaryDebuffText.text = "<color=red>ACTIVE CLAUSE:</color> " + debuffName;
                    summaryDebuffText.gameObject.SetActive(true);
                }
                else
                {
                    summaryDebuffText.gameObject.SetActive(false);
                }
            }
            
            if (summaryTotalText) summaryTotalText.text = "CALCULATING...";
            
            ShowShutterContent(ShutterState.Summary);

            if (summaryTotalText)
            {
                // Tween Float
                DOTween.To(() => baseScore, x => summaryTotalText.text = x.ToString("F2"), totalScore, 1f)
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
    public void ShowGameOver(float totalScore, int loopsSurvived, float highScore, bool isNewRecord)
    {
        if (pauseButton) pauseButton.SetActive(false);

        // Close shutters first
        CloseShutters();
        
        DOVirtual.DelayedCall(0.5f, () => {
            if (finalScoreText != null)
            {
                string textDisplay = "SCORE: " + totalScore.ToString("F2");
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
    
    public void ToggleFog(bool enable)
    {
        // Reuse ScreenFlash or add a dedicated Vignette Image
        if (screenFlash != null)
        {
            if (enable)
            {
                screenFlash.color = new Color(0, 0, 0, 0.9f); // Dark Fog
                screenFlash.DOFade(0.85f, 1f).SetUpdate(true); // Fade to 85% opacity black
            }
            else
            {
                // Reset if not impactful flash
                if (screenFlash.color.a > 0 && screenFlash.color == new Color(0,0,0, 0.9f)) 
                    screenFlash.DOFade(0f, 0.5f);
            }
        }
    }

    public void OnNextLoopPressed()
    {
        HideSummary();
        if(GameManager.Instance != null)
            GameManager.Instance.ConfirmNextLoop();
    }
}