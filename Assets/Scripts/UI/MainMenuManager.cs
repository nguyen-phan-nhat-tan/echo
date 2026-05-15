using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI highScoreText;
    public CanvasGroup mainGroup;
    public GameObject settingsMenu;

    
    [Header("Scene Configuration")]
    public string gameSceneName = "GameScene"; 

    void Start()
    {
        // Ensure time is running when we load the menu
        Time.timeScale = 1f;

        // Load and Display High Score
        float bestScore = PlayerPrefs.GetFloat("HighScore", 0f);
        
        if (highScoreText != null)
        {
            highScoreText.text = bestScore.ToString("F2");
        }
            
        // Intro Animation
        if (mainGroup != null)
        {
            mainGroup.alpha = 0f;
            mainGroup.DOFade(1f, 1f).SetEase(Ease.OutExpo).SetLink(gameObject);
        }
    }
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
    
    public void OnSettingsPressed()
    {
        if (settingsMenu != null)
        {
            settingsMenu.SetActive(true);
            // Optionally hide main buttons? 
            // For now, Settings acts as an overlay.
        }
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