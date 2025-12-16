using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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
    
    private List<List<FrameData>> allEchoDatas = new List<List<FrameData>>();
    private List<GameObject> activeEchoes = new List<GameObject>();
    private List<Vector2> usedSpawnPositions = new List<Vector2>();
    
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
        // 1. TÍNH TOÁN VỊ TRÍ SPAWN MỚI CHO PLAYER
        Vector2 spawnPos = GetRandomSpawnPosition();
        usedSpawnPositions.Add(spawnPos);
        
        player.transform.position = spawnPos;
        player.gameObject.SetActive(true);
        player.transform.rotation = Quaternion.identity; 

        // 2. DỌN DẸP ECHO CŨ
        foreach (var echo in activeEchoes) 
        {
            if (echo != null) Destroy(echo); 
        }
        activeEchoes.Clear();

        // 3. SPAWN ECHO HÌNH NHÂN (DUMMY) - LUÔN LUÔN XUẤT HIỆN
        // Con này luôn đứng im ở (0,0,0) làm bia đỡ đạn mỗi vòng
        GameObject dummyEcho = Instantiate(echoPrefab, Vector3.zero, Quaternion.identity);
        dummyEcho.GetComponent<EchoController>().InitializeDummy(); 
        dummyEcho.tag = "Enemy"; 
        activeEchoes.Add(dummyEcho);

        // 4. SPAWN CÁC ECHO DI CHUYỂN (CÁC BẢN THỂ CŨ)
        // Chỉ chạy nếu đã có dữ liệu (tức là từ Vòng 2 trở đi)
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

        // 5. BẮT ĐẦU GHI HÌNH VÒNG MỚI
        playerRecorder.StartRecording();
    }
    
    public void CheckWinCondition()
    {
        // Đếm tất cả Enemy còn sống (Bao gồm cả Dummy và Echo di chuyển)
        int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (enemyCount == 0)
        {
            EndLoop(true);
        }
    }
    
    // --- CÁC HÀM PHỤ TRỢ BÊN DƯỚI GIỮ NGUYÊN ---
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

    public void EndLoop(bool isWin)
    {
        playerRecorder.StopRecording();
        if (isWin)
        {
            allEchoDatas.Add(new List<FrameData>(playerRecorder.recordedFrames));
            StartCoroutine(RewindRoutine());
        }
        else
        {
            Debug.Log("Game Over!");
        }
    }

    IEnumerator RewindRoutine()
    {
        yield return new WaitForSeconds(1f);
        StartNewLoop();
    }
    
    void Update() {
        if (Input.GetKeyDown(KeyCode.R)) { 
            EndLoop(true);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapSize.x, mapSize.y, 0));
    }
}