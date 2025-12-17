using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Intro,
    Playing,
    Rewinding,
    GameOver
}

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
    
    private List<LoopData> allLoopDatas = new List<LoopData>(); 
    private List<GameObject> activeEchoes = new List<GameObject>();
    private List<Vector2> usedSpawnPositions = new List<Vector2>();
    private int currentWeaponIndex = 0;
    
    void OnEnable() { EchoController.OnEnemyKilled += HandleEnemyDeath; }
    void OnDisable() { EchoController.OnEnemyKilled -= HandleEnemyDeath; }
    
    void Awake() { Instance = this; }
    void Start() { StartNewLoop(); }
    
    public void StartNewLoop()
    {
        currentLoop++;
        currentTimer = loopDuration; 
        currentState = GameState.Intro;
        
        currentWeaponIndex = Random.Range(0, availableWeapons.Count);
        WeaponData selectedWeapon = availableWeapons[currentWeaponIndex];
        player.EquipWeapon(selectedWeapon);
        
        if (GameUI.Instance != null)
        {
            GameUI.Instance.HideSummary();
            
            GameUI.Instance.ShowLoopStart(currentLoop, selectedWeapon.weaponName.ToUpper(), () => 
            {
                currentState = GameState.Playing;
                playerRecorder.StartRecording();
            });
            
            GameUI.Instance.UpdateLoop(currentLoop);
            GameUI.Instance.UpdateTimer(currentTimer);
        }

        Vector2 spawnPos = GetRandomSpawnPosition();
        usedSpawnPositions.Add(spawnPos);
        player.transform.position = spawnPos;
        player.gameObject.SetActive(true);
        player.transform.rotation = Quaternion.identity; 
        player.ResetState(); 
        
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
                WeaponData echoWeapon = availableWeapons[data.weaponIndex];
                echoScript.Initialize(data.frames, echoWeapon);
                newEcho.tag = "Enemy"; 
                activeEchoes.Add(newEcho);
            }
        }
    }
    
    void HandleEnemyDeath(int points)
    {
        if (currentState != GameState.Playing) return;
        currentScore += points;
        if (GameUI.Instance != null) GameUI.Instance.UpdateScore(currentScore);
        CheckWinCondition();
    }
    
    public void CheckWinCondition()
    {
        if (currentState != GameState.Playing) return;
        int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (enemyCount <= 0) EndLoop(true);
    }
    
    public void EndLoop(bool isWin)
    {
        if (currentState != GameState.Playing) return;
        playerRecorder.StopRecording();
        
        if (isWin)
        {
            currentState = GameState.Rewinding;
            int timeBonus = Mathf.FloorToInt(currentTimer * 100); 
            int totalNewScore = currentScore + timeBonus;

            if (CameraShaker.Instance != null) CameraShaker.Instance.Shake(2f, 0.5f);
            if (GameUI.Instance != null) GameUI.Instance.ShowWinSummary(currentScore, currentTimer, totalNewScore);

            currentScore = totalNewScore;

            LoopData newData = new LoopData(currentWeaponIndex, new List<FrameData>(playerRecorder.recordedFrames));
            allLoopDatas.Add(newData);
            
            StartCoroutine(RewindRoutine());
        }
        else
        {
            currentState = GameState.GameOver; // Stop the game logic
            Debug.Log("Game Over!");

            // Show UI
            if (GameUI.Instance != null) 
            {
                GameUI.Instance.ShowGameOver(currentScore, currentLoop);
            }
        }
    }

    IEnumerator RewindRoutine()
    {
        yield return new WaitForSeconds(3f);
        StartNewLoop();
    }
    
    void Update() {
        if (currentState == GameState.Playing && currentTimer > 0 && player.gameObject.activeSelf)
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