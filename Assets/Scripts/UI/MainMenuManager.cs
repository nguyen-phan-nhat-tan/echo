using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI highScoreText;
    public CanvasGroup mainGroup;
    
    [Header("Scene Configuration")]
    // IMPORTANT: Make sure your Game Scene is added to File -> Build Settings
    public string gameSceneName = "GameScene"; 

    void Start()
    {
        // SAFETY FIX: Ensure time is running when we load the menu
        // (In case we quit the game while it was paused)
        Time.timeScale = 1f;

        // 1. Load and Display High Score
        int bestScore = PlayerPrefs.GetInt("HighScore", 0);
        
        if (highScoreText != null)
        {
            if (bestScore > 0)
                highScoreText.text = "HIGH SCORE: " + bestScore.ToString("N0");
            else
                highScoreText.text = ""; // Hide if no score yet
        }
            
        // 2. Intro Animation (Smooth Fade In)
        if (mainGroup != null)
        {
            mainGroup.alpha = 0f;
            mainGroup.DOFade(1f, 1f).SetEase(Ease.OutExpo).SetLink(gameObject);
        }
    }

    // Link this to your "PLAY" Button
    public void OnPlayPressed()
    {
        // Fade out before loading
        if (mainGroup != null)
        {
            mainGroup.DOFade(0f, 0.5f).SetLink(gameObject).OnComplete(() => {
                LoadGameScene();
            });
        }
        else
        {
            LoadGameScene();
        }
    }
    
    private void LoadGameScene()
    {
        SceneManager.LoadScene(gameSceneName);
    }
    
    // Link this to your "QUIT" Button
    public void OnQuitPressed()
    {
        Application.Quit();
    }
    
    // Optional: Dev tool to reset data
    public void OnResetDataPressed()
    {
        PlayerPrefs.DeleteAll();
        if (highScoreText != null) highScoreText.text = "";
        Debug.Log("Data Reset!");
    }
}