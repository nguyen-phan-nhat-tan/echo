using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public PlayerController player;
    public Recorder playerRecorder;
    public GameObject echoPrefab;
    
    [Header("Spawn Settings")]
    public Vector2 mapSize = new Vector2(25f, 25f); 
    public float minDistanceFromCenter = 3f;        
    public float minDistanceFromHistory = 3f;       
    
    [Header("Game State")]
    public float loopDuration = 60f;
    private float currentTimer;
    private int currentLoop = 0;
    private int currentScore = 0; // The Total Score
    
    private bool isGameActive = false;
    
    private List<List<FrameData>> allEchoDatas = new List<List<FrameData>>();
    private List<GameObject> activeEchoes = new List<GameObject>();
    private List<Vector2> usedSpawnPositions = new List<Vector2>();

    // --- EVENT SUBSCRIPTION (NEW) ---
    void OnEnable() 
    { 
        // Listen for the Echo screaming "I died!"
        EchoController.OnEnemyKilled += HandleEnemyDeath; 
    }
    
    void OnDisable() 
    { 
        EchoController.OnEnemyKilled -= HandleEnemyDeath; 
    }
    // --------------------------------
    
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartNewLoop();
    }
    
    public void StartNewLoop()
    {
        currentLoop++;
        currentTimer = loopDuration; 
        isGameActive = false;
        
        // 1. UI UPDATES (Intro & Hide Summary)
        if (GameUI.Instance != null)
        {
            GameUI.Instance.HideSummary();
            
            // PASS THE CALLBACK FUNCTION HERE
            GameUI.Instance.ShowLoopStart(currentLoop, "PISTOL", () => 
            {
                // This code runs ONLY after the Intro Fade is finished:
                isGameActive = true; 
                playerRecorder.StartRecording(); // Start recording syncs with timer
            });

            GameUI.Instance.UpdateLoop(currentLoop);
            GameUI.Instance.UpdateTimer(currentTimer);
        }
        
        // 2. PLAYER SPAWN
        Vector2 spawnPos = GetRandomSpawnPosition();
        usedSpawnPositions.Add(spawnPos);
        
        player.transform.position = spawnPos;
        player.gameObject.SetActive(true);
        player.transform.rotation = Quaternion.identity; 
        player.ResetState(); // Reset Dash/Cooldowns

        // 3. CLEAN OLD ECHOES
        foreach (var echo in activeEchoes) 
        {
            if (echo != null) Destroy(echo); 
        }
        activeEchoes.Clear();
        
        // 4. SPAWN DUMMY
        GameObject dummyEcho = Instantiate(echoPrefab, Vector3.zero, Quaternion.identity);
        dummyEcho.GetComponent<EchoController>().InitializeDummy(); 
        dummyEcho.tag = "Enemy"; 
        activeEchoes.Add(dummyEcho);

        // 5. SPAWN MOVING ECHOES
        if (allEchoDatas.Count > 0)
        {
            foreach (var loopData in allEchoDatas)
            {
                GameObject newEcho = Instantiate(echoPrefab, Vector3.zero, Quaternion.identity); 
                newEcho.GetComponent<EchoController>().Initialize(loopData);
                newEcho.tag = "Enemy"; 
                activeEchoes.Add(newEcho);
            }
        }

        // 6. START RECORDING
        playerRecorder.StartRecording();
    }
    
    // --- NEW: EVENT HANDLER ---
    void HandleEnemyDeath(int points)
    {
        // 1. Add Score immediately
        currentScore += points;
        if (GameUI.Instance != null) GameUI.Instance.UpdateScore(currentScore);

        // 2. Check Win Condition
        // NOTE: Ensure EchoController changes tag to "Untagged" BEFORE invoking this event!
        CheckWinCondition();
    }
    
    public void CheckWinCondition()
    {
        int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (enemyCount <= 0)
        {
            EndLoop(true);
        }
    }
    
    public void EndLoop(bool isWin)
    {
        playerRecorder.StopRecording();
        
        if (isWin)
        {
            // --- NEW: MATH & JUICE ---
            
            // 1. Calculate Time Bonus (1 sec = 100 pts)
            int timeBonus = Mathf.FloorToInt(currentTimer * 100); 
            int totalNewScore = currentScore + timeBonus;

            // 2. Big Screen Shake (Only happens here!)
            if (CameraShaker.Instance != null) CameraShaker.Instance.Shake(2f, 0.5f);

            // 3. Show the "Math" Screen
            if (GameUI.Instance != null)
            {
                GameUI.Instance.ShowWinSummary(currentScore, currentTimer, totalNewScore);
            }

            // 4. Update the real score to the new total
            currentScore = totalNewScore;

            // 5. Save Data
            allEchoDatas.Add(new List<FrameData>(playerRecorder.recordedFrames));
            
            StartCoroutine(RewindRoutine());
        }
        else
        {
            Debug.Log("Game Over!");
            // TODO: Handle Game Over logic (Restart Game?)
        }
    }

    IEnumerator RewindRoutine()
    {
        // Wait 3 seconds so player can see the Score Summary
        yield return new WaitForSeconds(3f);
        StartNewLoop();
    }
    
    void Update() {
        // Only run timer if player is active (not during rewind)
        if (isGameActive && currentTimer > 0 && player.gameObject.activeSelf)
        {
            currentTimer -= Time.deltaTime;
            
            if (GameUI.Instance != null) GameUI.Instance.UpdateTimer(currentTimer);

            if (currentTimer <= 0)
            {
                Debug.Log("Time's Up!");
                EndLoop(false); 
            }
        }
    }
    
    // REMOVED: public void AddScore(int amount) -> We use HandleEnemyDeath now
    
    Vector2 GetRandomSpawnPosition()
    {
        // (Keep existing logic...)
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