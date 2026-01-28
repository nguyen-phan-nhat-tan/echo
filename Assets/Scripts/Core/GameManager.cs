using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    [Header("UI References")]
    public PlayerController player;
    public Recorder playerRecorder;
    public GameObject echoPrefab;
    public GameObject mainCanvas; // NEW: Reference to enable Canvas
    
    [Header("Spawn Settings")]
    public Vector2 mapSize = new Vector2(25f, 25f); 
    public float minDistanceFromCenter = 3f;        
    public float minDistanceFromHistory = 3f;       
    
    [Header("Game State")]
    public float baseLoopDuration = 15f; 
    public float timerGrowthRate = 5f;   
    public float maxLoopDuration = 300f;  
    private float currentLoopDuration;   
    private float currentTimer;
    private int currentLoop = 0;
    private float currentScore = 0f;
    public GameState currentState = GameState.Intro;
    
    [Header("Arsenal")]
    public List<WeaponData> availableWeapons;
    
    [Header("Loop Transition Timing")]
    public float winDelay = 1.0f;
    public float autoAdvanceDelay = 3.0f;
    public float rewindDelay = 1.5f;
    
    [Header("Slow Motion Settings")]
    public float slowMoTimeScale = 0.3f;
    public float slowMoDuration = 1.5f;
    
    // DATA STORAGE
    private List<LoopData> allLoopDatas = new List<LoopData>(); 
    private List<GameObject> activeEchoes = new List<GameObject>();
    private List<Vector2> usedSpawnPositions = new List<Vector2>();
    
    // DEBUFF SYSTEM
    public List<DebuffData> availableDebuffs;
    public DebuffData currentActiveDebuff; 

    // Internal State
    private int currentWeaponIndex = 0;
    private Coroutine autoAdvanceCoroutine;

    void Awake() 
    { 
        if (Instance == null) Instance = this; 
        else Destroy(gameObject);
        
        // Auto-load debuffs if empty
        if (availableDebuffs == null || availableDebuffs.Count == 0)
        {
            var loaded = Resources.LoadAll<DebuffData>("Debuffs");
            if (loaded.Length > 0)
            {
                availableDebuffs = new List<DebuffData>(loaded);
            }
        }
    }

    void OnEnable()
    {
        GameEvents.OnEnemyDeath += HandleEnemyDeath;
        GameEvents.OnPlayerDeath += HandlePlayerDeath;
    }

    void OnDisable()
    {
        GameEvents.OnEnemyDeath -= HandleEnemyDeath;
        GameEvents.OnPlayerDeath -= HandlePlayerDeath;
    }
    
    void Start() 
    { 
        if (mainCanvas != null) mainCanvas.SetActive(true);
        StartNewLoop(); 
    }

    // --- PAUSE LOGIC ---
    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f; 
            GameEvents.OnStateChanged?.Invoke(GameState.Paused);
        }
        else if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f; 
            GameEvents.OnStateChanged?.Invoke(GameState.Playing);
        }
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f; 
        SceneManager.LoadScene("MainMenu");
    }
    
    public void StartNewLoop()
    {
        Time.timeScale = 1f; 
        currentLoop++;
        
        // Linear Timer: Base + (Loop-1) * Growth
        currentLoopDuration = baseLoopDuration + (timerGrowthRate * (currentLoop - 1));
        currentLoopDuration = Mathf.Min(currentLoopDuration, maxLoopDuration);
        currentTimer = currentLoopDuration;

        
        currentState = GameState.Intro;
        
        if (availableWeapons.Count > 0)
        {
            // SEQUENTIAL vs RANDOM selection
            if (currentLoop <= availableWeapons.Count)
            {
                // First pass: Sequential order (Loops 1..Count -> Indices 0..Count-1)
                currentWeaponIndex = currentLoop - 1;
            }
            else
            {
                // Subsequent loops: Random
                currentWeaponIndex = Random.Range(0, availableWeapons.Count);
            }
            WeaponData selectedWeapon = availableWeapons[currentWeaponIndex];
            player.EquipWeapon(selectedWeapon); 

            // SELECT DEBUFF (Loop 6+)
            currentActiveDebuff = null;
            if (currentLoop > 5 && availableDebuffs.Count > 0)
            {
                // 25% Chance to have NO debuff (Respite)
                if (Random.value > 0.25f)
                {
                    currentActiveDebuff = availableDebuffs[Random.Range(0, availableDebuffs.Count)];
                }
            }

            // APPLY DEBUFF
            float moveMult = 1f;
            float fireMult = 1f;
            float dashMult = 1f;
            
            bool isFoggy = false;
            bool isDrift = false;

            if (currentActiveDebuff != null)
            {
                moveMult = currentActiveDebuff.moveSpeedMultiplier;
                fireMult = currentActiveDebuff.fireRateMultiplier;
                dashMult = currentActiveDebuff.dashCooldownMultiplier;
                isFoggy = currentActiveDebuff.fog;
                isDrift = currentActiveDebuff.drift;
            }
            
            player.SetStatsMultiplier(moveMult, fireMult, dashMult);
            player.SetMechanicsRef(isDrift);

            if (GameUI.Instance != null)
            {
                GameUI.Instance.ToggleFog(isFoggy);
                
                GameUI.Instance.HideSummary();
                string debuffName = (currentActiveDebuff != null) ? currentActiveDebuff.debuffName.ToUpper() : "";
                
                GameUI.Instance.ShowLoopStart(currentLoop, selectedWeapon.weaponName.ToUpper(), debuffName, () => 
                {
                    currentState = GameState.Playing;
                    GameEvents.OnStateChanged?.Invoke(GameState.Playing);
                    playerRecorder.StartRecording();
                });
                GameUI.Instance.UpdateLoop(currentLoop);
                GameUI.Instance.UpdateTimer(currentTimer);
            }
        }
        else
        {
            Debug.LogError("No weapons assigned in GameManager!");
        }

        GameEvents.OnStateChanged?.Invoke(GameState.Intro);
        GameEvents.OnLoopStart?.Invoke(currentLoop);

        SpawnPlayer();
        SpawnEchoes();
    }
    
    public void ConfirmNextLoop()
    {
        if (currentState == GameState.Rewinding) return;

        if (currentState == GameState.LoopTransition)
        {
            if (autoAdvanceCoroutine != null) StopCoroutine(autoAdvanceCoroutine);
            StartCoroutine(RewindRoutine());
        }
    }

    private void SpawnPlayer()
    {
        player.gameObject.SetActive(false); // Ensure we reset so OnEnable triggers again
        Vector2 spawnPos = GetRandomSpawnPosition();
        usedSpawnPositions.Add(spawnPos);
        player.transform.position = spawnPos;
        player.gameObject.SetActive(true);
        player.transform.rotation = Quaternion.identity; 
        player.ResetState(); 
    }

    private void SpawnEchoes()
    {
        foreach (var echo in activeEchoes) 
        {
            if (echo != null) Destroy(echo); 
        }
        activeEchoes.Clear();

        GameObject dummyEcho = Instantiate(echoPrefab, Vector3.zero, Quaternion.identity);
        dummyEcho.GetComponent<EchoController>().InitializeDummy(); 
        dummyEcho.tag = "Enemy"; 
        activeEchoes.Add(dummyEcho);
        
        if (allLoopDatas.Count > 0)
        {
            foreach (LoopData data in allLoopDatas)
            {
                GameObject newEcho = Instantiate(echoPrefab, Vector3.zero, Quaternion.identity); 
                EchoController echoScript = newEcho.GetComponent<EchoController>();
                
                if (data.weaponIndex >= 0 && data.weaponIndex < availableWeapons.Count)
                {
                    WeaponData echoWeapon = availableWeapons[data.weaponIndex];
                    echoScript.Initialize(data.frames, echoWeapon);
                }
                else
                {
                    if (availableWeapons.Count > 0)
                        echoScript.Initialize(data.frames, availableWeapons[0]);
                }

                newEcho.tag = "Enemy"; 
                activeEchoes.Add(newEcho);
            }
        }
    }

    private void HandleEnemyDeath()
    {
        if (currentState != GameState.Playing) return;
        
        // NO POINTS for killing enemies
        // currentScore += 100;
        
        if (GameUI.Instance != null) GameUI.Instance.UpdateScore(currentScore);
        CheckWinCondition();
    }

    private void HandlePlayerDeath()
    {
        EndLoop(false);
    }
    
    private void CheckWinCondition()
    {
        if (currentState != GameState.Playing) return;

        int enemyCount = 0;
        foreach(var echo in activeEchoes)
        {
            if (echo != null && echo.CompareTag("Enemy")) enemyCount++;
        }

        if (enemyCount <= 0) EndLoop(true);
    }
    
    private void EndLoop(bool isWin)
    {
        if (currentState != GameState.Playing) return;
        
        playerRecorder.StopRecording();
        
        if (isWin)
        {
            HandleWin();
        }
        else
        {
            HandleGameOver();
        }
    }

    private void HandleWin()
    {
        float baseScore = currentScore;
        
        // NEW SCORING: 50 Points + Time Remaining (Seconds + Decimals)
        float loopClearBonus = 0f;
        float timeBonus = currentTimer; 
        
        float totalNewScore = baseScore + loopClearBonus + timeBonus;
        currentScore = totalNewScore;

        // Save Data
        LoopData newData = new LoopData(currentWeaponIndex, new List<FrameData>(playerRecorder.recordedFrames));
        allLoopDatas.Add(newData);

        // Stop Gameplay immediately
        currentState = GameState.LoopTransition;
        GameEvents.OnStateChanged?.Invoke(GameState.LoopTransition);
        
        // CINEMATIC WIN SEQUENCE
        string debuffName = (currentActiveDebuff != null) ? currentActiveDebuff.debuffName.ToUpper() : "";
        StartCoroutine(CinematicWinSequence(baseScore, currentTimer, totalNewScore, debuffName));
    }

    private IEnumerator CinematicWinSequence(float baseScore, float timer, float totalScore, string debuffName)
    {
        // 1. SLOW MOTION
        Time.timeScale = slowMoTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        
        yield return new WaitForSecondsRealtime(slowMoDuration);
        
        // 2. IMPACT EFFECT (chromatic aberration burst)
        if (FeedbackManager.Instance != null) FeedbackManager.Instance.PlayImpact();
        
        yield return new WaitForSecondsRealtime(0.4f); // Give effect time to be seen
        
        // 3. CLOSE SHUTTERS
        if (GameUI.Instance != null) GameUI.Instance.CloseShutters(false); // false = no built-in flash
        
        yield return new WaitForSecondsRealtime(0.4f); // Wait for shutters to close
        
        // 4. RESTORE TIME
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        // 5. SHOW SCORE
        yield return new WaitForSecondsRealtime(winDelay);
        
        if (GameUI.Instance != null) 
        {
            GameUI.Instance.ShowWinSummary(baseScore, timer, totalScore, debuffName);
        }
        
        GameEvents.OnLoopCompleted?.Invoke();
        autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine());
    }

    private void HandleGameOver()
    {
        currentState = GameState.GameOver;
        GameEvents.OnStateChanged?.Invoke(GameState.GameOver);
        GameEvents.OnPlayerDeath?.Invoke(); // FeedbackManager listens to this
        GameEvents.OnLoopEnded?.Invoke(); 
        
        // CINEMATIC DEATH SEQUENCE
        StartCoroutine(CinematicDeathSequence());
    }
    
    private IEnumerator CinematicDeathSequence()
    {
        // 1. SLOW MOTION
        Time.timeScale = slowMoTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        
        yield return new WaitForSecondsRealtime(slowMoDuration);
        
        // 2. IMPACT EFFECT (chromatic aberration burst)
        if (FeedbackManager.Instance != null) FeedbackManager.Instance.PlayImpact();
        
        yield return new WaitForSecondsRealtime(0.4f); // Give effect time to be seen
        
        // 3. CLOSE SHUTTERS
        if (GameUI.Instance != null) GameUI.Instance.CloseShutters(false);
        
        yield return new WaitForSecondsRealtime(0.4f);
        
        // 4. RESTORE TIME
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        
        // 5. CALCULATE HIGH SCORE
        float savedHighScore = PlayerPrefs.GetFloat("HighScore", 0f);
        bool isNewRecord = false;
        if (currentScore > savedHighScore)
        {
            savedHighScore = currentScore;
            PlayerPrefs.SetFloat("HighScore", savedHighScore);
            PlayerPrefs.Save();
            isNewRecord = true;
        }

        // 6. SHOW GAME OVER
        yield return new WaitForSecondsRealtime(winDelay);
        
        if (GameUI.Instance != null) 
            GameUI.Instance.ShowGameOver(currentScore, currentLoop, savedHighScore, isNewRecord);
    }

    IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSeconds(autoAdvanceDelay);
        StartCoroutine(RewindRoutine());
    }

    IEnumerator RewindRoutine()
    {
        currentState = GameState.Rewinding;
        GameEvents.OnStateChanged?.Invoke(GameState.Rewinding);
        GameEvents.OnLoopEnded?.Invoke(); 
        
        yield return new WaitForSeconds(rewindDelay);
        StartNewLoop();
    }
    
    void Update() 
    {
        if (currentState != GameState.Playing) return;

        if (currentTimer > 0 && player.gameObject.activeSelf)
        {
            float dt = Time.deltaTime;
            if (currentActiveDebuff != null) dt *= currentActiveDebuff.timerSpeedMultiplier;
            
            currentTimer -= dt; 
            if (GameUI.Instance != null) GameUI.Instance.UpdateTimer(currentTimer);
            if (currentTimer <= 0)
            {
                EndLoop(false); 
            }
        }
    }
    
    Vector2 GetRandomSpawnPosition()
    {
        int maxAttempts = 100; 
        for (int i = 0; i < maxAttempts; i++)
        {
            float randomX = Random.Range(-mapSize.x / 2, mapSize.x / 2);
            float randomY = Random.Range(-mapSize.y / 2, mapSize.y / 2);
            Vector2 candidatePos = new Vector2(randomX, randomY);

            if (Vector2.Distance(candidatePos, Vector2.zero) < minDistanceFromCenter) continue; 

            bool isTooCloseToHistory = false;
            foreach (Vector2 oldPos in usedSpawnPositions)
            {
                if (Vector2.Distance(candidatePos, oldPos) < minDistanceFromHistory)
                {
                    isTooCloseToHistory = true;
                    break;
                }
            }
            if (isTooCloseToHistory) continue; 
            return candidatePos;
        }
        return new Vector2(Random.Range(-10, 10), Random.Range(-10, 10));
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapSize.x, mapSize.y, 0));
    }
}