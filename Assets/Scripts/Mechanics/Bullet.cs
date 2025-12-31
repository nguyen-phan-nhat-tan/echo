using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 20f; // Increased default speed for snappy feel
    public float lifeTime = 3f; // Reduced lifetime (10s is too long for off-screen bullets)
    public bool isEnemyBullet = false;
    
    private float timer;

    void OnEnable() 
    {
        // Reset timer when pulled from pool
        timer = lifeTime;
    }

    void Update()
    {
        // Move Local Up (Forward)
        transform.Translate(Vector2.up * speed * Time.deltaTime);

        // Manual timer is cleaner than Coroutine for high-frequency objects
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Disable();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Hit Wall
        if (other.CompareTag("Wall"))
        {
            // VFX: Spawn sparks rotating 180 degrees from bullet direction
            Quaternion impactRot = transform.rotation * Quaternion.Euler(0, 0, 180);
            GameEvents.OnBulletImpact?.Invoke(transform.position, impactRot);
            
            Disable();
            return;
        }

        // 2. Enemy Bullet hitting Player
        if (isEnemyBullet)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("Player Hit!");
                GameEvents.OnPlayerDeath?.Invoke();
                
                // Optional: Spawn blood/hit VFX on player
                // GameEvents.OnPlayerHit?.Invoke(transform.position);
                
                Disable();
            }
        }
        // 3. Player Bullet hitting Enemy
        else
        {
            if (other.CompareTag("Enemy"))
            {
                EchoController echo = other.GetComponent<EchoController>();
                if (echo != null) 
                {
                    echo.Die(); // Echo handles its own Death Event and Explosion VFX
                }
            
                Disable();
                // REMOVED: GameEvents.OnEnemyDeath?.Invoke(); 
                // Reason: EchoController.Die() already calls this. Calling it here causes double counting.
            }
        }
    }

    void Disable()
    {
        gameObject.SetActive(false);
    }
}