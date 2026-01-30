using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; 

public class EchoController : MonoBehaviour
{
    [Header("References")]
    public Transform firePoint;
    private WeaponData currentWeapon;
    private List<FrameData> framesToPlay;
    private int currentFrameIndex = 0;
    
    private bool isDead = false; 
    private bool isStaticDummy = false;
    private bool wasDashing = false;
    
    private bool canMove = false; 
    
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Color originalColor = Color.red;

    private Vector3 initialScale;

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        originalColor = spriteRenderer.color;
        initialScale = transform.localScale; // Capture inspector scale
        if (firePoint != null) defaultFirePointPos = firePoint.localPosition;
    }
    
    void OnEnable()
    {
        GameEvents.OnStateChanged += OnGameStateChanged;
    }

    void OnDisable()
    {
        GameEvents.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState newState)
    {
        canMove = (newState == GameState.Playing);
    }

    public void Initialize(List<FrameData> frames, WeaponData weapon)
    {
        framesToPlay = new List<FrameData>(frames);
        currentWeapon = weapon;
        
        currentFrameIndex = 0;
        isStaticDummy = false;
        wasDashing = false;
        currentSpiralAngle = 0f; // Reset Spiral

        if (firePoint != null)
        {
            firePoint.localPosition = defaultFirePointPos;
            firePoint.localRotation = Quaternion.identity;
        }
        
        ResetState();
    }
    
    // ...

    public void InitializeDummy()
    {
        isStaticDummy = true;
        framesToPlay = null; 
        currentWeapon = null;
        ResetState();
    }

    void ResetState()
    {
        isDead = false;
        spriteRenderer.color = originalColor; 
        spriteRenderer.DOFade(0.7f, 0f);  
        col.enabled = true;               
        tag = "Enemy";                    
        transform.localScale = Vector3.zero;
        transform.DOScale(initialScale, 0.5f).SetEase(Ease.OutBack).SetLink(gameObject);
    }

    void FixedUpdate()
    {
        if (isDead || isStaticDummy) return; 
        
        if (!canMove) return; 

        if (framesToPlay == null) return;
        
        // Fix: Stop playing if we run out of frames. 
        // Do NOT clamp to last frame, or it will repeat the last action (shooting) forever.
        if (currentFrameIndex >= framesToPlay.Count)
        {
            return;
        }

        if (currentFrameIndex < 0) return;

        FrameData data = framesToPlay[currentFrameIndex];

        transform.position = data.position;
        transform.rotation = Quaternion.Euler(0f, 0f, data.rotation - 90f);

        if (data.isDashing && !wasDashing) StartDash();
        else if (!data.isDashing && wasDashing) EndDash();
        
        wasDashing = data.isDashing;

        if (data.isShooting && !data.isDashing) 
        {
            FireBullet();
            // Note: Sounds are handled by FeedbackManager via Events now if desired,
            // or we can add a specific "OnEnemyShoot" event later.
        }

        currentFrameIndex++;
    }
    
    void StartDash()
    {
        spriteRenderer.DOColor(Color.cyan, 0.1f);
        spriteRenderer.DOFade(0.4f, 0.1f);
        col.enabled = false;
    }
    
    void EndDash()
    {
        spriteRenderer.DOColor(originalColor, 0.1f);
        spriteRenderer.DOFade(0.7f, 0.1f);
        col.enabled = true;
    }
    
    // State for Spiral
    private float currentSpiralAngle = 0f;
    private Vector3 defaultFirePointPos;

    void FireBullet()
    {
        if (currentWeapon == null) return;
        
        Quaternion baseRotation = firePoint.rotation;

        // NEW: Ring/Nova Pattern Logic (Parity with PlayerController)
        if (currentWeapon.spiralRate != 0)
        {
             int pelletCount = currentWeapon.bulletCount; 
             if (pelletCount < 1) pelletCount = 1;

             float angleStep = 360f / pelletCount;

             for (int i = 0; i < pelletCount; i++)
             {
                 float angle = i * angleStep;
                 // Note: Enocders usually capture world rotation. Here we generate a local ring.
                 // Echoes follow frame data rotation, but for the Ring pattern we just blast 360 relative to up.
                 Quaternion rotation = Quaternion.Euler(0f, 0f, angle - 90f); 

                 // FIXED: Spawn "EnemyBullet" directly
                 GameObject bulletObj = ObjectPooler.Instance.SpawnFromPool("EnemyBullet", firePoint.position, rotation);
                 
                 if (bulletObj != null)
                 {
                     Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                     if (bulletScript != null)
                     {
                         bulletScript.Initialize(currentWeapon);
                         bulletScript.isEnemyBullet = true; 
                         // No SetColor needed, prefab should be Red
                         bulletObj.tag = "EnemyBullet";     
                     }
                 }
             }
             return; // Done
        }

        // Standard Logic (if not Nova)
        for (int i = 0; i < currentWeapon.bulletCount; i++)
        {
            float randomSpread = UnityEngine.Random.Range(-currentWeapon.spreadAngle / 2f, currentWeapon.spreadAngle / 2f);
            Quaternion finalRotation = baseRotation * Quaternion.Euler(0, 0, randomSpread);
            
            // FIXED: Spawn "EnemyBullet" directly
            GameObject bulletObj = ObjectPooler.Instance.SpawnFromPool("EnemyBullet", firePoint.position, finalRotation);
            
            if (bulletObj != null)
            {
                Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                if (bulletScript != null)
                {
                    bulletScript.Initialize(currentWeapon);
                    bulletScript.isEnemyBullet = true; 
                    // No SetColor needed
                    bulletObj.tag = "EnemyBullet"; 
                }
            }
        }
    }
    
    public void Die()
    {
        if (isDead) return; 
        isDead = true;
        
        gameObject.tag = "Untagged"; 
        
        // --- FIXED: Event Only (FeedbackManager handles Shake/Sound) ---
        // --- FIXED: Event Only (FeedbackManager handles Shake/Sound) ---
        GameEvents.OnEnemyDeath?.Invoke();
        GameEvents.OnEnemyExplosion?.Invoke(transform.position); 
        // ---------------------------------------------------------------
        
        spriteRenderer.DOKill();
        spriteRenderer.color = new Color(0.3f, 0f, 0f, 1f); 
        // Fade to 0 (Invisible) and deactivate
        spriteRenderer.DOFade(0f, 0.3f).OnComplete(() => {
            gameObject.SetActive(false);
        });
        spriteRenderer.sortingOrder = -1; 
        col.enabled = false;
    }
    
    void OnDestroy()
    {
        transform.DOKill(); 
        if (spriteRenderer != null) spriteRenderer.DOKill();
    }
}