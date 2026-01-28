using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 20f; // Increased default speed for snappy feel
    public float lifeTime = 3f; // Reduced lifetime (10s is too long for off-screen bullets)
    [Header("Mechanics")]
    public bool isEnemyBullet = false;
    public bool useWave = false; // New
    public bool useRicochet = false; // New
    public int bounces = 0; // New

    private float timer;
    private float startTime; // For wave

    void OnEnable() 
    {
        timer = lifeTime;
        startTime = Time.time;
        isEnemyBullet = false;
        useWave = false;
        useRicochet = false;
        bounces = 0;
        gameObject.tag = "PlayerBullet"; 
    }

    // Initialize method to pass weapon data
    private WeaponData sourceWeapon; // NEW

    public void Initialize(WeaponData data)
    {
        sourceWeapon = data; // Store reference
        speed = data.bulletSpeed;
        useWave = data.waveMovement;
        useRicochet = data.ricochet;
        bounces = data.ricochet ? 1 : 0;
        
        // Spawn Trail
        if (sourceWeapon.bulletTrailPrefab != null)
        {
            GameObject trail = Instantiate(sourceWeapon.bulletTrailPrefab, transform.position, Quaternion.identity);
            trail.transform.SetParent(transform);
            trail.transform.localPosition = Vector3.zero;
        }
    }

    void Update()
    {
        // Movement
        float waveOffset = 0f;
        if (useWave)
        {
            waveOffset = Mathf.Sin((Time.time - startTime) * 10f) * 5f; 
        }

        Vector3 moveDir = Vector2.up * speed * Time.deltaTime;
        // Apply wave relative to right vector
        if (useWave) transform.Translate(Vector2.right * waveOffset * Time.deltaTime);
        
        transform.Translate(Vector2.up * speed * Time.deltaTime);

        timer -= Time.deltaTime;
        if (timer <= 0) Disable();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Hit Wall
        if (other.CompareTag("Wall"))
        {
            // RICOCHET LOGIC
            if (useRicochet && bounces > 0)
            {
                bounces--;
                
                // Simple reflection: Raycast to find normal
                Vector2 dir = transform.up;
                RaycastHit2D hit = Physics2D.Raycast(transform.position - (Vector3)dir * 0.5f, dir, 1.0f);
                if (hit.collider != null)
                {
                    Vector2 reflectDir = Vector2.Reflect(dir, hit.normal);
                    float rot = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, rot - 90);
                    return; // Don't destroy
                }
            }

            // CUSTOM WALL HIT VFX
            if (sourceWeapon != null && sourceWeapon.wallHitPrefab != null)
            {
                 Instantiate(sourceWeapon.wallHitPrefab, transform.position, transform.rotation);
            }
            else
            {
                Quaternion impactRot = transform.rotation * Quaternion.Euler(0, 0, 180);
                GameEvents.OnBulletImpact?.Invoke(transform.position, impactRot);
            }
            
            Disable();
            return;
        }

        // 2. Enemy Bullet hitting Player
        if (isEnemyBullet)
        {
            if (other.CompareTag("Player"))
            {
                // Check for Dash Invulnerability
                PlayerController pc = other.GetComponent<PlayerController>();
                if (pc != null && pc.isDashing) return;

                Debug.Log("Player Hit!");
                GameEvents.OnPlayerDeath?.Invoke();
                
                Disable();
            }
        }
        // 3. Player Bullet hitting Enemy
        else
        {
            if (other.CompareTag("Enemy"))
            {
                // CUSTOM ENEMY HIT VFX
                if (sourceWeapon != null && sourceWeapon.enemyHitPrefab != null)
                {
                    Instantiate(sourceWeapon.enemyHitPrefab, transform.position, transform.rotation);
                }
                else
                {
                    Quaternion impactRot = transform.rotation * Quaternion.Euler(0, 0, 180);
                    GameEvents.OnBulletImpact?.Invoke(transform.position, impactRot);
                }

                EchoController echo = other.GetComponent<EchoController>();
                if (echo != null) 
                {
                    echo.Die(); // Echo handles its own Death Event and Explosion VFX
                }
            
                Disable();
            }
        }
    }

    void Disable()
    {
        gameObject.SetActive(false);
    }
}