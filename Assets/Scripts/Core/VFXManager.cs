using UnityEngine;
using System.Collections.Generic;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;
    
    [System.Serializable]
    public class VFXEntry
    {
        public string effectName;
        public GameObject prefab;
        public float lifetime = 3f;
        
        [Header("Optional Pooling")]
        public bool usePooling = false;
        public string poolTag;
    }
    
    [Header("VFX Library")]
    public List<VFXEntry> effects = new List<VFXEntry>();
    
    // Quick lookup
    private Dictionary<string, VFXEntry> effectDict = new Dictionary<string, VFXEntry>();

    void Awake()
    {
        Instance = this;
        
        // Build dictionary for fast lookup
        effectDict.Clear();
        foreach (var entry in effects)
        {
            if (!string.IsNullOrEmpty(entry.effectName))
                effectDict[entry.effectName] = entry;
        }
    }

    void OnEnable()
    {
        GameEvents.OnBulletImpact += HandleBulletImpact;
        GameEvents.OnEnemyExplosion += HandleEnemyExplosion;
        GameEvents.OnPlayerShoot += HandlePlayerShoot;
        GameEvents.OnPlayerDash += HandlePlayerDash;
    }

    void OnDisable()
    {
        GameEvents.OnBulletImpact -= HandleBulletImpact;
        GameEvents.OnEnemyExplosion -= HandleEnemyExplosion;
        GameEvents.OnPlayerShoot -= HandlePlayerShoot;
        GameEvents.OnPlayerDash -= HandlePlayerDash;
    }

    // --- Event Handlers ---
    private void HandlePlayerShoot(AudioClip clip)
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null && player.firePoint != null)
        {
            Spawn("MuzzleFlash", player.firePoint.position, player.firePoint.rotation);
        }
    }

    private void HandleBulletImpact(Vector2 pos, Quaternion rot)
    {
        Spawn("BulletImpact", pos, rot);
    }

    private void HandleEnemyExplosion(Vector2 pos)
    {
        Spawn("EnemyDeath", pos, Quaternion.identity);
    }
    
    private void HandlePlayerDash()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            Spawn("DashEffect", player.transform.position, Quaternion.identity);
        }
    }

    // --- Public API ---
    public void Spawn(string effectName, Vector3 position, Quaternion rotation)
    {
        if (!effectDict.TryGetValue(effectName, out VFXEntry entry))
        {
            // Effect not configured, silently skip
            return;
        }
        
        if (entry.usePooling && ObjectPooler.Instance != null && !string.IsNullOrEmpty(entry.poolTag))
        {
            ObjectPooler.Instance.SpawnFromPool(entry.poolTag, position, rotation);
        }
        else if (entry.prefab != null)
        {
            GameObject vfx = Instantiate(entry.prefab, position, rotation);
            if (entry.lifetime > 0)
                Destroy(vfx, entry.lifetime);
        }
    }
    
    // Convenience overload
    public void Spawn(string effectName, Vector3 position)
    {
        Spawn(effectName, position, Quaternion.identity);
    }
}