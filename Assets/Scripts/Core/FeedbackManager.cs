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
    public MMF_Player newLoopFeedback;
    public MMF_Player rewindFeedback;
    public MMF_Player impactFeedback; // Heavy chromatic aberration burst
    
    void OnEnable()
    {
        GameEvents.OnPlayerShoot += OnPlayerShoot;
        GameEvents.OnPlayerDash += OnPlayerDash;
        GameEvents.OnEnemyDeath += OnEnemyDeath;
        GameEvents.OnLoopCompleted += OnLoopCompleted;
        GameEvents.OnLoopStart += OnLoopStart; // NEW
        GameEvents.OnPlayerDeath += OnPlayerDeath;
        GameEvents.OnBulletImpact += OnBulletImpact;
        GameEvents.OnEnemyExplosion += OnEnemyExplosion;
        GameEvents.OnStateChanged += OnGameStateChanged;
    }

    void OnDisable()
    {
        GameEvents.OnPlayerShoot -= OnPlayerShoot;
        GameEvents.OnPlayerDash -= OnPlayerDash;
        GameEvents.OnEnemyDeath -= OnEnemyDeath;
        GameEvents.OnLoopCompleted -= OnLoopCompleted;
        GameEvents.OnLoopStart -= OnLoopStart; // NEW
        GameEvents.OnPlayerDeath -= OnPlayerDeath;
        GameEvents.OnBulletImpact -= OnBulletImpact;
        GameEvents.OnEnemyExplosion -= OnEnemyExplosion;
        GameEvents.OnStateChanged -= OnGameStateChanged;
    }

    // --- OPTIMIZED HANDLER ---
    private MMF_Sound cachedShootSound;
    private float lastShootTime;

    private void OnPlayerShoot(AudioClip clipToPlay)
    {
        // 0. Cooldown check (prevent audio overlap/stutter)
        if (Time.time < lastShootTime + 0.05f) return;
        lastShootTime = Time.time;

        if (playerShootFeedback != null)
        {
            // 1. Cache the reference to avoid frequent GetComponent calls
            if (cachedShootSound == null) 
            {
                cachedShootSound = playerShootFeedback.GetFeedbackOfType<MMF_Sound>();
            }

            // 2. Inject the clip
            if (cachedShootSound != null)
            {
                cachedShootSound.Sfx = clipToPlay;
                
                // Safety: Ensure settings are correct for 2D
                cachedShootSound.MinVolume = 1f;
                cachedShootSound.MaxVolume = 1f;
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

    private void OnLoopStart(int loopCount) // NEW
    {
        if (newLoopFeedback != null) newLoopFeedback.PlayFeedbacks();
    }

    // --- NEW: Grid Ripple Handlers ---
    private void OnBulletImpact(Vector2 pos, Quaternion rot)
    {
        // Wall Hit Ripple
        if (ReactiveGrid.Instance != null)
        {
            ReactiveGrid.Instance.ApplyForce(pos, 2f, 2f, Color.white, true);
        }
    }

    private void OnEnemyExplosion(Vector2 pos)
    {
        // Enemy Hit/Death Ripple
        if (ReactiveGrid.Instance != null)
        {
            ReactiveGrid.Instance.ApplyForce(pos, 5f, 3f, Color.red, true);
        }
    }
    
    void OnGameStateChanged(GameState state)
    {
        if (state == GameState.Rewinding && rewindFeedback != null)
            rewindFeedback.PlayFeedbacks();
    }
    
    // Public method for external calls
    public static FeedbackManager Instance;
    void Awake() { Instance = this; }
    
    public void PlayImpact()
    {
        if (impactFeedback != null)
            impactFeedback.PlayFeedbacks();
    }
}