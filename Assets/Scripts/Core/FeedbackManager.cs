using UnityEngine;
using MoreMountains.Feedbacks; 

public class FeedbackManager : MonoBehaviour
{
    [Header("Player Feedbacks")]
    public MMF_Player playerShootFeedback; 
    public MMF_Player playerDashFeedback;  

    [Header("Game Feedbacks")]
    public MMF_Player enemyDeathFeedback; 
    public MMF_Player loopWinFeedback;     
    public MMF_Player gameOverFeedback;    

    void OnEnable()
    {
        GameEvents.OnPlayerShoot += OnPlayerShoot;
        GameEvents.OnPlayerDash += OnPlayerDash;
        GameEvents.OnEnemyDeath += OnEnemyDeath;
        GameEvents.OnLoopCompleted += OnLoopCompleted;
        GameEvents.OnPlayerDeath += OnPlayerDeath;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerShoot -= OnPlayerShoot;
        GameEvents.OnPlayerDash -= OnPlayerDash;
        GameEvents.OnEnemyDeath -= OnEnemyDeath;
        GameEvents.OnLoopCompleted -= OnLoopCompleted;
        GameEvents.OnPlayerDeath -= OnPlayerDeath;
    }

    // --- UPDATED HANDLER ---
    private void OnPlayerShoot(AudioClip clipToPlay)
    {
        if (playerShootFeedback != null)
        {
            // 1. Find the Sound Feedback inside the player
            // Note: MMF_Sound is the class name for the "Audio > Sound" feedback
            MMF_Sound soundFeedback = playerShootFeedback.GetFeedbackOfType<MMF_Sound>();

            // 2. Inject the clip
            if (soundFeedback != null)
            {
                soundFeedback.Sfx = clipToPlay;
                
                // Safety: Ensure settings are correct for 2D
                soundFeedback.MinVolume = 1f;
                soundFeedback.MaxVolume = 1f;
                // soundFeedback.SpatialBlend = 0f; // Feel 5.x property usually
            }

            // 3. Play
            playerShootFeedback.PlayFeedbacks();
        }
    }
    // -----------------------

    private void OnPlayerDash()
    {
        if (playerDashFeedback != null) playerDashFeedback.PlayFeedbacks();
    }

    private void OnEnemyDeath()
    {
        if (enemyDeathFeedback != null) enemyDeathFeedback.PlayFeedbacks();
    }

    private void OnLoopCompleted()
    {
        if (loopWinFeedback != null) loopWinFeedback.PlayFeedbacks();
    }

    private void OnPlayerDeath()
    {
        if (gameOverFeedback != null) gameOverFeedback.PlayFeedbacks();
    }
}