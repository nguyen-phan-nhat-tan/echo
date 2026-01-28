using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
        
        [Header("VFX Settings")]
        public bool isVFX = false;
        public float autoReturnTime = 3f; // Time before VFX returns to pool
    }

    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolSettings;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolSettings = new Dictionary<string, Pool>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogError($"ObjectPooler: Pool with tag '{pool.tag}' has a missing Prefab reference! Please assign it in the Inspector.");
                continue;
            }
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
            poolSettings.Add(pool.tag, pool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        // Set transform BEFORE activating (prevents ghost collisions)
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        
        // Handle VFX-specific setup
        Pool settings = poolSettings[tag];
        if (settings.isVFX)
        {
            PrepareVFX(objectToSpawn);
        }
        
        objectToSpawn.SetActive(true);
        
        // Auto-replay particle systems
        if (settings.isVFX)
        {
            PlayVFX(objectToSpawn);
            StartCoroutine(ReturnToPoolAfterDelay(tag, objectToSpawn, settings.autoReturnTime));
        }

        // Re-enqueue for reuse (only if not VFX, VFX will re-enqueue after delay)
        if (!settings.isVFX)
        {
            poolDictionary[tag].Enqueue(objectToSpawn);
        }

        return objectToSpawn;
    }
    
    private void PrepareVFX(GameObject vfx)
    {
        // Clear trails
        TrailRenderer[] trails = vfx.GetComponentsInChildren<TrailRenderer>(true);
        foreach (var trail in trails)
        {
            trail.Clear();
        }
        
        // Stop and clear particle systems
        ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    
    private void PlayVFX(GameObject vfx)
    {
        ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particles)
        {
            ps.Play(true);
        }
    }
    
    private IEnumerator ReturnToPoolAfterDelay(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(tag, obj);
    }
    
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag)) return;
        
        obj.SetActive(false);
        poolDictionary[tag].Enqueue(obj);
    }
}