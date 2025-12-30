using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private int currentScore = 0;
    public GameState currentState = GameState.Intro;
    
    [Header("Arsenal")]
    public List<WeaponData> availableWeapons;
    
    // DATA STORAGE
    private List<LoopData> allLoopDatas = new List<LoopData>(); 
    private List<GameObject> activeEchoes = new List<GameObject>();
    private List<Vector2> usedSpawnPositions = new List<Vector2>();
    
    private int currentWeaponIndex = 0;
    private Coroutine autoAdvanceCoroutine;

    void Awake() 
    { 
        if (Instance == null) Instance = this; 
        else Destroy(gameObject);
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
    
    void Start() { StartNewLoop(); }

    // --- NEW METHODS CAUSING THE ERROR (Make sure these are present!) ---
    
    public void TogglePause()
    {
        // 1. If Playing -> Pause
        if (currentState == GameState.Playing)
        {
            currentState = GameState.Paused;
            Time.timeScale = 0f; 
            Debug.Log("Game PAUSED. TimeScale is now: " + Time.timeScale);
            GameEvents.OnStateChanged?.Invoke(GameState.Paused);
        }
        // 2. If Paused -> Resume
        else if (currentState == GameState.Paused)
        {
            currentState = GameState.Playing;
            Time.timeScale = 1f; 
            Debug.Log("Game RESUMED. TimeScale is now: " + Time.timeScale);
            GameEvents.OnStateChanged?.Invoke(GameState.Playing);
        }
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f; // Always reset time before loading scenes
        SceneManager.LoadScene("MainMenu");
    }
    
    // -------------------------------------------------------------------

    public void StartNewLoop()
    {
        Time.timeScale = 1f; 
        currentLoop++;
        currentTimer = loopDuration; 
        currentState = GameState.Intro;
        
        if (availableWeapons.Count > 0)
        {
            currentWeaponIndex = Random.Range(0, availableWeapons.Count);
            WeaponData selectedWeapon = availableWeapons[currentWeaponIndex];
            player.EquipWeapon(selectedWeapon); 

            if (GameUI.Instance != null)
            {
                GameUI.Instance.HideSummary();
                GameUI.Instance.ShowLoopStart(currentLoop, selectedWeapon.weaponName.ToUpper(), () => 
                {
                    currentState = GameState.Playing;
                    GameEvents.OnStateChanged?.Invoke(GameState.Playing);
                    playerRecorder.StartRecording();
                    
                    if (SoundManager.Instance != null) 
                        SoundManager.Instance.PlaySound(selectedWeapon.loadSound);
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
        
        currentScore += 100;
        
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
        int baseScore = currentScore;
        int timeBonus = Mathf.FloorToInt(currentTimer * 100); 
        int totalNewScore = baseScore + timeBonus;

        currentScore = totalNewScore;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(SoundType.Win);

        LoopData newData = new LoopData(currentWeaponIndex, new List<FrameData>(playerRecorder.recordedFrames));
        allLoopDatas.Add(newData);

        currentState = GameState.LoopTransition;
        GameEvents.OnStateChanged?.Invoke(GameState.LoopTransition);
        
        if (GameUI.Instance != null) 
        {
            GameUI.Instance.ShowWinSummary(baseScore, currentTimer, totalNewScore);
        }
        
        GameEvents.OnLoopCompleted?.Invoke();

        autoAdvanceCoroutine = StartCoroutine(AutoAdvanceRoutine());
    }

    private void HandleGameOver()
    {
        currentState = GameState.GameOver;
        GameEvents.OnStateChanged?.Invoke(GameState.GameOver);
        GameEvents.OnLoopEnded?.Invoke(); 
        
        Debug.Log("Game Over!");

        int savedHighScore = PlayerPrefs.GetInt("HighScore", 0);
        bool isNewRecord = false;

        if (currentScore > savedHighScore)
        {
            savedHighScore = currentScore;
            PlayerPrefs.SetInt("HighScore", savedHighScore);
            PlayerPrefs.Save();
            isNewRecord = true;
        }

        if (GameUI.Instance != null) 
            GameUI.Instance.ShowGameOver(currentScore, currentLoop, savedHighScore, isNewRecord);
    }

    IEnumerator AutoAdvanceRoutine()
    {
        yield return new WaitForSeconds(3.0f);
        StartCoroutine(RewindRoutine());
    }

    IEnumerator RewindRoutine()
    {
        currentState = GameState.Rewinding;
        GameEvents.OnStateChanged?.Invoke(GameState.Rewinding);
        GameEvents.OnLoopEnded?.Invoke(); 
        
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(SoundType.LoopRewind);
            
        if (CameraShaker.Instance != null) CameraShaker.Instance.Shake(2f, 0.5f);

        yield return new WaitForSeconds(1.5f);
        
        StartNewLoop();
    }
    
    void Update() 
    {
        // STRICT GUARD: If not playing, STOP ALL LOGIC.
        if (currentState != GameState.Playing) return;

        if (currentTimer > 0 && player.gameObject.activeSelf)
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