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
    
    [Header("Combat Settings")]
    public float fireRate = 5f; // Bắn 5 viên/giây
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
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
            justShotTargetFrame = true; 
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            // Move very fast in the forward direction
            rb.MovePosition(rb.position + (Vector2)transform.up * dashSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // Normal movement
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }

    void Shoot()
    {
        // Giới hạn tốc độ bắn (FireRate) ở đây nếu cần
        // Tìm dòng bắn đạn và sửa tag thành "PlayerBullet"
        Quaternion finalRotation = firePoint.rotation;
        Transform target = GetClosestEnemyInSights();

        // If we found a valid target, snap aim to them
        if (target != null)
        {
            Vector2 directionToTarget = target.position - firePoint.position;
            float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            finalRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        // Use the calculated rotation instead of the default firePoint.rotation
        ObjectPooler.Instance.SpawnFromPool("PlayerBullet", firePoint.position, finalRotation);
        if (CameraShaker.Instance != null) CameraShaker.Instance.ShakeShoot();
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
            
            // 2. Check if Enemy is within our movement/facing angle (The Cone)
            // We use the joystick input (moveInput) to determine where we are 'trying' to aim
            if (moveInput != Vector2.zero)
            {
                float angleToEnemy = Vector2.Angle(moveInput, directionToEnemy);
                
                // If enemy is outside the cone, ignore them
                if (angleToEnemy > assistAngle / 2) continue;
            }

            // 3. Find the closest one among the valid ones
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
        justDashedTargetFrame = true; // Tell recorder we dashed
        nextDashTime = Time.time + dashCooldown;

        // This effectively gives us I-Frames because we check 'isDashing' in collision
        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }
    // Hàm này tự động chạy khi Player va chạm vật lý với ai đó
    void OnCollisionEnter2D(Collision2D other)
    {
        // Kiểm tra xem cái mình vừa tông vào có phải là Enemy (Echo) không
        if (isDashing) return;

        // Standard Hit Logic
        if (other.gameObject.CompareTag("EnemyBullet"))
        {
            Debug.Log("Player Hit by Bullet!");
            // TODO: Notify GameManager of Death
        }
    }
    
    public void ResetState()
    {
        isDashing = false;
        justDashedTargetFrame = false; // Clear recorder flag
        nextDashTime = 0f; // Reset cooldown so you can dash immediately
    
        // Stop any lingering physics forces from the previous loop
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