using UnityEngine;
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

    [Header("Menus")]
    public CanvasGroup introGroup; 
    public CanvasGroup summaryGroup; 
    public CanvasGroup gameOverGroup;
    public CanvasGroup pauseGroup; 

    [Header("Text References")]
    public TextMeshProUGUI introLoopText;
    public TextMeshProUGUI introWeaponText;
    public TextMeshProUGUI summaryScoreText; 
    public TextMeshProUGUI summaryTimeText;  
    public TextMeshProUGUI summaryTotalText; 
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI finalLoopText; 
    public GameObject newRecordVisual; 
    
    void Awake()
    {
        Instance = this;
        
        if (introGroup) { introGroup.alpha = 0; introGroup.blocksRaycasts = false; }
        if (summaryGroup) { summaryGroup.alpha = 0; summaryGroup.blocksRaycasts = false; }
        if (gameOverGroup) { gameOverGroup.alpha = 0; gameOverGroup.blocksRaycasts = false; }
        
        if (pauseGroup) 
        { 
            pauseGroup.alpha = 0; 
            pauseGroup.blocksRaycasts = false; 
            pauseGroup.gameObject.SetActive(false); 
        }

        if (newRecordVisual != null) newRecordVisual.SetActive(false);
    }

    // --- UPDATED BUTTON EVENTS ---

    public void OnPausePressed()
    {
        if (GameManager.Instance == null) return;
        
        if (pauseGroup)
        {
            pauseGroup.gameObject.SetActive(true);
            // FIX: Added .SetUpdate(true) so it fades in while game is paused
            pauseGroup.DOFade(1f, 0.2f).SetUpdate(true).SetLink(pauseGroup.gameObject);
            pauseGroup.blocksRaycasts = true;
        }
        
        if (pauseButton) pauseButton.SetActive(false);

        GameManager.Instance.TogglePause();
    }

    public void OnResumePressed()
    {
        if (GameManager.Instance == null) return;

        if (pauseGroup)
        {
            // FIX: Added .SetUpdate(true) so it fades out while game is paused
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

    // ----------------------------------------------------

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

    public void ShowLoopStart(int loopCount, string weaponName, Action onIntroFinished)
    {
        if (introLoopText) introLoopText.text = "LOOP " + loopCount;
        if (introWeaponText) introWeaponText.text = "WEAPON: " + weaponName;
        
        if (introGroup)
        {
            introGroup.alpha = 1f;
            Sequence seq = DOTween.Sequence().SetLink(gameObject);
            seq.Append(introGroup.DOFade(1f, 0.5f));
            seq.AppendInterval(1.5f); 
            seq.Append(introGroup.DOFade(0f, 0.5f));
            seq.OnComplete(() => onIntroFinished?.Invoke());
        }
        else
        {
            onIntroFinished?.Invoke();
        }
        
        if (pauseButton) pauseButton.SetActive(true);
    }

    public void ShowWinSummary(int baseScore, float timeLeft, int totalScore)
    {
        if (pauseButton) pauseButton.SetActive(false);

        if (summaryGroup == null) return;

        summaryGroup.blocksRaycasts = true; 

        if (summaryScoreText) summaryScoreText.text = baseScore.ToString("N0");
        if (summaryTimeText) summaryTimeText.text = "+" + (timeLeft * 100).ToString("N0"); 
        if (summaryTotalText) summaryTotalText.text = "CALCULATING...";
    
        summaryGroup.alpha = 1f;

        if (summaryTotalText)
        {
            DOTween.To(() => baseScore, x => summaryTotalText.text = x.ToString("N0"), totalScore, 1f)
                .SetDelay(0.5f) 
                .SetEase(Ease.OutExpo)
                .SetUpdate(true) 
                .SetLink(gameObject);
        }
    }

    public void HideSummary()
    {
        if (summaryGroup == null) return;
        summaryGroup.blocksRaycasts = false; 
        summaryGroup.DOFade(0f, 0.2f).SetLink(summaryGroup.gameObject);
    }
    
    public void ShowGameOver(int totalScore, int loopsSurvived, int highScore, bool isNewRecord)
    {
        if (pauseButton) pauseButton.SetActive(false);
        if (gameOverGroup == null) return;
        gameOverGroup.blocksRaycasts = true;

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
        
        gameOverGroup.DOFade(1f, 1f).SetEase(Ease.OutExpo).SetLink(gameOverGroup.gameObject);
    }
    
    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnNextLoopPressed()
    {
        HideSummary();
        if(GameManager.Instance != null)
            GameManager.Instance.ConfirmNextLoop();
    }
}