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
    public TextMeshProUGUI scoreText; // NEW: HUD Score
    public TextMeshProUGUI echoCountText; // NEW: Remaining Echoes
    public TextMeshProUGUI activeDebuffHUDText; 
    public Color warningColor = Color.red;
    public Color normalColor = Color.white;
    public GameObject pauseButton; 
    public GameObject hudPanel; // NEW: Parent for HUD elements

    [Header("Mini Map")]
    public Image miniMapBackground;
    public RectTransform miniMapContainer;
    private MiniMapController miniMapController; 

    [Header("Win Visuals")]
    public TextMeshProUGUI loopClearText; // NEW: CENTER "CLEAR"
    public TextMeshProUGUI scoreBonusText; // NEW: Floating text for score addition

    [Header("Cinematic Shutters")]
    public RectTransform topShutter;
    public RectTransform bottomShutter;
    
    private bool hasInitializedColor = false;

    void Start()
    {
         // Redundant check to ensure we capture the Inspector color
         if (!hasInitializedColor && timerText != null)
         {
             normalColor = timerText.color;
             hasInitializedColor = true;
         }
    }
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
    public TextMeshProUGUI gameOverTitleText; // NEW: The big title
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

        // Setup mini map
        if (miniMapContainer != null)
        {
            miniMapController = miniMapContainer.GetComponent<MiniMapController>();
            if (miniMapController == null)
            {
                miniMapController = miniMapContainer.gameObject.AddComponent<MiniMapController>();
            }
            if (miniMapBackground != null) miniMapController.miniMapBackground = miniMapBackground;
            miniMapController.miniMapContainer = miniMapContainer;
        }
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
        if (gameOverTitleText) gameOverTitleText.gameObject.SetActive(false); // NEW
        if (newRecordVisual) newRecordVisual.SetActive(false);
        if (retryButton) retryButton.SetActive(false);
        if (homeButton) homeButton.SetActive(false);
        
        // Hide Win Visuals (New)
        if (loopClearText) loopClearText.gameObject.SetActive(false);
        if (scoreBonusText) scoreBonusText.gameObject.SetActive(false);
        
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
                if (gameOverTitleText) gameOverTitleText.gameObject.SetActive(true); // NEW
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

        int seconds = Mathf.FloorToInt(timeRemaining);
        int milliseconds = Mathf.FloorToInt((timeRemaining * 100) % 100);
        timerText.text = string.Format("{0:00}.{1:00}", seconds, milliseconds); 

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
    
    public void UpdateScore(float score) 
    { 
        if (scoreText != null) scoreText.text = score.ToString("F2"); // Display as Int during play? Or F0.
    }

    public void UpdateDebuffHUD(string debuffName)
    {
        if (activeDebuffHUDText != null)
        {
            if (!string.IsNullOrEmpty(debuffName))
            {
                activeDebuffHUDText.text = "CAUTION: " + debuffName;
                activeDebuffHUDText.gameObject.SetActive(true);
                // Optional: Pulse animation
                activeDebuffHUDText.transform.DOKill();
                activeDebuffHUDText.transform.localScale = Vector3.one;
                activeDebuffHUDText.transform.DOScale(1.1f, 1f).SetLoops(-1, LoopType.Yoyo).SetLink(activeDebuffHUDText.gameObject);
            }
            else
            {
                activeDebuffHUDText.gameObject.SetActive(false);
            }
        }
    }

    [Header("Win Animation Settings")]
    public float clearTextScaleDuration = 2.0f;
    public Ease clearTextEase = Ease.OutBack;
    public float clearTextStayDuration = 3.0f;

    // --- NEW: Loop Clear Animation (No Shutters) ---
    public void ShowLoopClear(float baseScore, float bonusScore, float totalScore)
    {
        Debug.Log($"[GameUI] ShowLoopClear | Scale: {clearTextScaleDuration}, Ease: {clearTextEase}, Stay: {clearTextStayDuration}");

        // 1. CLEAR Text
        if (loopClearText != null)
        {
            // Kill previous tweens to prevent conflicts
            loopClearText.transform.DOKill();
            loopClearText.DOKill();

            loopClearText.gameObject.SetActive(true);
            loopClearText.alpha = 1f; 
            loopClearText.transform.localScale = Vector3.zero;
            
            // Scale Up
            loopClearText.transform
                .DOScale(1f, clearTextScaleDuration)
                .SetEase(clearTextEase)
                .SetUpdate(true);
            
            // Fade Out after delay
            loopClearText
                .DOFade(0f, 0.5f)
                .SetDelay(clearTextStayDuration)
                .SetUpdate(true)
                .OnComplete(()=> loopClearText.gameObject.SetActive(false));
        }

        // 2. Score Addition Animation
        if (scoreBonusText != null && scoreText != null)
        {
            scoreBonusText.text = bonusScore.ToString("F2");
            scoreBonusText.gameObject.SetActive(true);
            scoreBonusText.alpha = 1f;
            
            // Force Start Position: 40 units BELOW the score text
            // We use world position offset since they might have different parents
            scoreBonusText.transform.position = scoreText.transform.position + (Vector3.down * 40f); 
            
            Sequence seq = DOTween.Sequence().SetUpdate(true);
            
            seq.AppendInterval(0.5f); 
            
            // Slide Up TO the ScoreText position & Fade Out
            seq.Append(scoreBonusText.transform.DOMove(scoreText.transform.position, 1.5f).SetEase(Ease.OutCubic));
            seq.Join(scoreBonusText.DOFade(0f, 1.0f).SetDelay(0.5f)); // Start fading halfway through move
            
            // Count Up Main Score
            seq.InsertCallback(1.5f, () => {
                scoreBonusText.gameObject.SetActive(false);
                
                DOTween.To(() => baseScore, x => scoreText.text = x.ToString("F2"), totalScore, 1.0f) // Slower count up
                    .SetEase(Ease.OutExpo)
                    .SetUpdate(true)
                    .SetLink(scoreText.gameObject);
                    
                // Pulse Score
                scoreText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f).SetUpdate(true);
            });
        }
    }
    
    // --- INTRO (Shutters start CLOSED, show intro text, then OPEN) ---
    public void ShowLoopStart(int loopCount, string weaponName, string debuffName, string debuffDesc, Action onIntroFinished)
    {
        if (introLoopText) introLoopText.text = "LOOP " + loopCount;
        if (introWeaponText) introWeaponText.text = "WEAPON: " + weaponName;
        
        // Handle Debuff Text
        if (introDebuffText)
        {
            if (!string.IsNullOrEmpty(debuffName))
            {
                introDebuffText.text = "DEBUFF: " + debuffName + "\n<size=70%>" + debuffDesc + "</size>";
                introDebuffText.gameObject.SetActive(true);
            }
            else if (loopCount <= 5)
            {
                string tutorialText = GetLoopTutorialText(loopCount);
                if (!string.IsNullOrEmpty(tutorialText))
                {
                    introDebuffText.text = tutorialText;
                    introDebuffText.gameObject.SetActive(true);
                }
                else
                {
                    introDebuffText.text = "";
                    introDebuffText.gameObject.SetActive(false);
                }
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
            if (hudPanel) hudPanel.SetActive(true); // Re-enable HUD
            if (pauseButton) pauseButton.SetActive(true);
        });
        seq.AppendInterval(0.7f); // Wait for shutters to open
        seq.OnComplete(() => onIntroFinished?.Invoke());
    }

    private string GetLoopTutorialText(int loopCount)
    {
        switch (loopCount)
        {
            case 1:
                return "SHOOT ALL ENEMIES TO WIN";
            case 2:
                return "THE ENEMIES MOVE EXACTLY \nHOW YOU MOVED";
            case 3:
                return "RANDOM WEAPON EACH LOOP";
            case 4:
                return "YOU ARE INVULNERABLE WHILE DASHING";
            case 5:
                return "RANDOM DEBUFFS STARTING NEXT LOOP";
            default:
                return string.Empty;
        }
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
                    summaryDebuffText.text = "<color=red>ACTIVE DEBUFF:</color> " + debuffName;
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
        
        // HIDE HUD PANEL
        if (hudPanel) hudPanel.SetActive(false);
        // Fallback for elements if they are outside panel
        if(timerText) timerText.gameObject.SetActive(false); // Can remove if inside panel, kept for safety
        if(scoreText) scoreText.gameObject.SetActive(false);
        if(echoCountText) echoCountText.gameObject.SetActive(false);
        if(activeDebuffHUDText) activeDebuffHUDText.gameObject.SetActive(false);

        // CloseShutters(0.5f, false); // DISABLED as per User Request
        
        DOVirtual.DelayedCall(0.5f, () => {
            if (pauseButton) pauseButton.SetActive(false); // Ensure hidden
            ShowShutterContent(ShutterState.GameOver);

            // Elastic Text Animation for Title (match Loop Clear style)
            if (gameOverTitleText != null)
            {
                gameOverTitleText.transform.localScale = Vector3.zero;
                gameOverTitleText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
            }

            // Animate Score Counting like Win Screen
            if (finalScoreText != null)
            {
                // Init at 0
                finalScoreText.text = "SCORE: 0.00"; 
                finalScoreText.transform.localScale = Vector3.zero; // Start invisible
                
                // Pop In
                finalScoreText.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack).SetDelay(0.2f).SetUpdate(true);
                
                DOTween.To(() => 0f, x => {
                    string display = "SCORE: " + x.ToString("F2");
                    if (isNewRecord) display += "\n <color=yellow>(NEW!)</color>";
                    finalScoreText.text = display;
                }, totalScore, 1.5f).SetEase(Ease.OutExpo).SetDelay(0.2f).SetUpdate(true);
            }

            if (finalLoopText != null) finalLoopText.text = "LOOPS: " + loopsSurvived;

            if (newRecordVisual != null)
            {
                newRecordVisual.SetActive(isNewRecord);
                if(isNewRecord)
                    newRecordVisual.transform.DOScale(1.2f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetLink(newRecordVisual).SetUpdate(true);
            }
        });
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // --- SHUTTER CONTROLS ---
    public void CloseShutters(float duration, bool impactFlash)
    {
        // Screen flash for impact
        if (impactFlash && screenFlash != null)
        {
            screenFlash.color = new Color(1, 1, 1, 0.8f);
            screenFlash.DOFade(0f, 0.3f).SetUpdate(true);
        }
        
        Ease ease = impactFlash ? Ease.OutQuad : Ease.OutExpo;
        
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

    public void UpdateEchoCount(int count)
    {
        if (echoCountText != null)
        {
            echoCountText.text = $"{count} REMAINING";
        }
    }

    [Header("Tutorial UI")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI tutorialPromptText;
    public GameObject tutorialArrow;

    [Header("Tutorial End")]
    public GameObject tutorialEndPanel;
    public TextMeshProUGUI tutorialEndText;

    private Coroutine typewriterCoroutine;

    public void ShowTutorialPrompt(string text)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (tutorialArrow != null) tutorialArrow.SetActive(true);

        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
        if (tutorialPromptText != null)
        {
            tutorialPromptText.text = text; // Simple set; typewriter optional
        }
    }

    public void HideTutorialPrompt()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (tutorialArrow != null) tutorialArrow.SetActive(false);
        if (typewriterCoroutine != null) { StopCoroutine(typewriterCoroutine); typewriterCoroutine = null; }
    }

    public void ShowTutorialEnd(string message)
    {
        if (tutorialEndPanel != null) tutorialEndPanel.SetActive(true);
        if (tutorialEndText != null) tutorialEndText.text = message;
    }

}