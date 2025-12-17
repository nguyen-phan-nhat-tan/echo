using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public FloatingJoystick joystick;
    public GameObject bulletPrefab;
    public Transform firePoint;
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    
    [Header("Weapon System")]
    private WeaponData currentWeapon; // The current stats
    private float nextFireTime = 0f;
    
    [Header("Dash Settings")] // <--- NEW SECTION
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private float nextDashTime = 0f;
    
    [Header("Aim Assist")]
    public float assistRange = 5f;       // How far we scan
    public float assistAngle = 45f;      // The width of the cone (half-angle)
    public LayerMask enemyLayer;
    
    [HideInInspector] public bool isDashing = false; // "I-Frame" flag
    
    // Biến dùng để ghi dữ liệu (sẽ nói ở Bước 3)
    [HideInInspector] public bool justShotTargetFrame = false;
    [HideInInspector] public bool justDashedTargetFrame = false;
    [HideInInspector] public float rotationAngle;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    public void EquipWeapon(WeaponData newData)
    {
        currentWeapon = newData;
    }
    
    void Update()
    {
        if (isDashing) return;
        
        // Input di chuyển
        moveInput.x = joystick.Horizontal;
        moveInput.y = joystick.Vertical;

        // Xoay nhân vật theo hướng di chuyển
        if (moveInput != Vector2.zero)
        {
            rotationAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle - 90f);
        }
    
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextDashTime)
        {
            StartCoroutine(Dash());
        }
        // Logic bắn súng (Nút Attack - hoặc giữ để spam)
        // Ở đây giả lập nhấn Space hoặc nút UI Attack
        if (Input.GetKey(KeyCode.LeftShift) && Time.time >= nextFireTime)
        {
            if (Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + 1f / currentWeapon.fireRate;
                justShotTargetFrame = true; 
            }
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            rb.MovePosition(rb.position + (Vector2)transform.up * dashSpeed * Time.fixedDeltaTime);
        }
        else
        {
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void Shoot()
    {
        Quaternion baseRotation = firePoint.rotation;
        Transform target = GetClosestEnemyInSights();

        // If we found a valid target, snap aim to them
        if (target != null)
        {
            Vector2 directionToTarget = target.position - firePoint.position;
            float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            baseRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        for (int i = 0; i < currentWeapon.bulletCount; i++)
        {
            // Calculate Spread
            // If spread is 15 degrees, we pick a random angle between -7.5 and +7.5
            float randomSpread = Random.Range(-currentWeapon.spreadAngle / 2f, currentWeapon.spreadAngle / 2f);
            Quaternion finalRotation = baseRotation * Quaternion.Euler(0, 0, randomSpread);

            ObjectPooler.Instance.SpawnFromPool(currentWeapon.bulletTag, firePoint.position, finalRotation);
        }

        // 3. JUICE (Use Data intensity)
        if (CameraShaker.Instance != null) 
            CameraShaker.Instance.Shake(currentWeapon.shakeIntensity, 0.1f);
    }
    Transform GetClosestEnemyInSights()
    {
        // 1. Get all colliders within range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, assistRange, enemyLayer);
        
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
            // Only care about Enemies
            if (!hit.CompareTag("Enemy")) continue;

            Vector2 directionToEnemy = (hit.transform.position - transform.position).normalized;
            
            if (moveInput != Vector2.zero)
            {
                float angleToEnemy = Vector2.Angle(moveInput, directionToEnemy);
                
                if (angleToEnemy > assistAngle / 2) continue;
            }
            
            float distance = Vector2.Distance(transform.position, hit.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = hit.transform;
            }
        }

        return bestTarget;
    }
    IEnumerator Dash()
    {
        isDashing = true;
        justDashedTargetFrame = true;
        nextDashTime = Time.time + dashCooldown;
        
        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // I-Frames: If dashing, we phase through them
        if (isDashing) return;

        // Check if we hit an Echo (Enemy Tag)
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Game Over: Touched Echo!");
            
            // CRITICAL FIX: Notify the manager!
            if (GameManager.Instance != null) 
                GameManager.Instance.EndLoop(false);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // I-Frames: If dashing, bullets pass through us
        if (isDashing) return;

        // Check if we hit a Bullet
        if (other.CompareTag("EnemyBullet"))
        {
            Debug.Log("Game Over: Shot by Bullet!");
            
            // CRITICAL FIX: Notify the manager!
            if (GameManager.Instance != null) 
                GameManager.Instance.EndLoop(false);
            
            // Cleanup the bullet so it looks like it hit us
            Destroy(other.gameObject); 
        }
    }
    
    public void ResetState()
    {
        isDashing = false;
        justDashedTargetFrame = false;
        nextDashTime = 0f;
        
        if(rb != null) rb.velocity = Vector2.zero;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, assistRange);

        if (moveInput != Vector2.zero)
        {
            Vector3 forward = new Vector3(moveInput.x, moveInput.y, 0).normalized;
            Vector3 leftBoundary = Quaternion.Euler(0, 0, assistAngle / 2) * forward * assistRange;
            Vector3 rightBoundary = Quaternion.Euler(0, 0, -assistAngle / 2) * forward * assistRange;

            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
            Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        }
    }
}