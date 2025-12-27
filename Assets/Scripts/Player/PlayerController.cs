using UnityEngine;
using System.Collections;
using DG.Tweening; // Keeping this if you need tweening

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public FloatingJoystick joystick;
    public Transform firePoint;
    // public GameObject bulletPrefab; // Unused (we use ObjectPooler)
    
    [Header("Visuals")]
    public SpriteRenderer weaponRenderer; // Drag your Gun Sprite object here!

    private Rigidbody2D rb;
    private Vector2 moveInput;
    
    [Header("Weapon System")]
    private WeaponData currentWeapon;
    private float nextFireTime = 0f;
    
    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private float nextDashTime = 0f;
    
    [Header("Aim Assist")]
    public float assistRange = 5f;
    public float assistAngle = 45f;
    public LayerMask enemyLayer;
    
    // --- STATE FLAGS ---
    [HideInInspector] public bool isDashing = false;
    private bool canControl = true; // NEW: Pauses input during Loop Transition
    
    // --- RECORDER FLAGS ---
    [HideInInspector] public bool justShotTargetFrame = false;
    [HideInInspector] public bool justDashedTargetFrame = false;
    [HideInInspector] public float rotationAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // --- NEW: EVENT LISTENING (For Pausing) ---
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
        // Only allow movement if the game is actually Playing
        canControl = (newState == GameState.Playing);
        
        if (!canControl)
        {
            rb.velocity = Vector2.zero;
            moveInput = Vector2.zero;
        }
    }
    // ------------------------------------------
    
    public void EquipWeapon(WeaponData newData)
    {
        currentWeapon = newData;

        // NEW: Visual Update
        if (weaponRenderer != null && newData.weaponSprite != null)
        {
            weaponRenderer.sprite = newData.weaponSprite;
        }
    }
    
    void Update()
    {
        // NEW: Stop inputs if paused
        if (!canControl) return;
        if (isDashing) return;
        
        moveInput.x = joystick.Horizontal;
        moveInput.y = joystick.Vertical;
        
        if (moveInput != Vector2.zero)
        {
            rotationAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle - 90f);
        }
    
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= nextDashTime)
        {
            StartCoroutine(Dash());
        }

        // Safety check: ensure weapon exists
        if (currentWeapon != null && Input.GetKey(KeyCode.LeftShift) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / currentWeapon.fireRate;
            justShotTargetFrame = true; 
        }
    }

    void FixedUpdate()
    {
        // NEW: Stop physics if paused
        if (!canControl) return;

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
        
        if (target != null)
        {
            Vector2 directionToTarget = target.position - firePoint.position;
            float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            baseRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }

        for (int i = 0; i < currentWeapon.bulletCount; i++)
        {
            float randomSpread = Random.Range(-currentWeapon.spreadAngle / 2f, currentWeapon.spreadAngle / 2f);
            Quaternion finalRotation = baseRotation * Quaternion.Euler(0, 0, randomSpread);

            // Use the tag from the WeaponData (e.g., "PlayerBullet" or "ShotgunShell")
            ObjectPooler.Instance.SpawnFromPool(currentWeapon.bulletTag, firePoint.position, finalRotation);
        }
        
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(currentWeapon.shootSound);
        
        if (CameraShaker.Instance != null) 
            CameraShaker.Instance.Shake(currentWeapon.shakeIntensity, 0.1f);
    }

    Transform GetClosestEnemyInSights()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, assistRange, enemyLayer);
        
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D hit in hits)
        {
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
        
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(SoundType.Dash);
            
        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing) return;
        
        if (collision.gameObject.CompareTag("Enemy"))
        {
            GameEvents.OnPlayerDeath?.Invoke();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDashing) return;
        
        if (other.CompareTag("EnemyBullet"))
        {
            Debug.Log("Event: Player Death Broadcasted");
            GameEvents.OnPlayerDeath?.Invoke(); 
            // Destroy(other.gameObject); // Handled by Bullet script usually
            other.gameObject.SetActive(false); // Pooling friendly
        }
    }
    
    public void ResetState()
    {
        isDashing = false;
        justDashedTargetFrame = false;
        nextDashTime = 0f;
        
        if(rb != null) rb.velocity = Vector2.zero;
        moveInput = Vector2.zero;
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