using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public InputActionReference moveAction; 
    public InputActionReference dashAction;
    public InputActionReference fireAction; 
    public Transform firePoint;
    
    [Header("Visuals")]
    public SpriteRenderer weaponRenderer; 

    private Rigidbody2D rb;
    private Vector2 moveInput;
    
    [Header("Weapon System")]
    private WeaponData currentWeapon;
    private float nextFireTime = 0f;
    
    [Header("Dash Settings")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.5f; // UPDATED: 0.5s
    public float dashCooldown = 1f;
    private float nextDashTime = 0f;
    
    [Header("Aim Assist")]
    public float assistRange = 5f;
    public float assistAngle = 45f;
    public LayerMask enemyLayer;
    public LayerMask obstacleLayer; // NEW: To prevent shooting walls
    
    // Optimization: Pre-allocate array to avoid Garbage Collection
    private Collider2D[] hitBuffer = new Collider2D[20]; 
    
    // State Flags
    [HideInInspector] public bool isDashing = false;
    private bool canControl = true;
    
    // Recorder Flags
    [HideInInspector] public bool justShotTargetFrame = false;
    [HideInInspector] public bool justDashedTargetFrame = false;
    [HideInInspector] public float rotationAngle;

    private Collider2D col; // NEW

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>(); // NEW
    }

    void OnEnable()
    {
        GameEvents.OnStateChanged += OnGameStateChanged;
        if(moveAction != null) moveAction.action.Enable();
        if(dashAction != null) dashAction.action.Enable();
        if(fireAction != null) fireAction.action.Enable();
    }

    void OnDisable()
    {
        GameEvents.OnStateChanged -= OnGameStateChanged;
        if(moveAction != null) moveAction.action.Disable();
        if(dashAction != null) dashAction.action.Disable();
        if(fireAction != null) fireAction.action.Disable();
    }

    private void OnGameStateChanged(GameState newState)
    {
        canControl = (newState == GameState.Playing);
        if (!canControl)
        {
            moveInput = Vector2.zero;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
    }
    
    public void EquipWeapon(WeaponData newData)
    {
        currentWeapon = newData;
        if (weaponRenderer != null && newData.weaponSprite != null)
        {
            weaponRenderer.sprite = newData.weaponSprite;
        }
    }
    
    // Virtual Input (Mobile)
    [HideInInspector] public bool virtualFire = false;
    [HideInInspector] public bool virtualDash = false;

    void Update()
    {
        if (!canControl) return;
        if (isDashing) return;
        
        if (moveAction != null)
            moveInput = moveAction.action.ReadValue<Vector2>();
        
        if (moveInput != Vector2.zero)
        {
            rotationAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle - 90f);
        }
    
        // Dash Check (Input System OR Virtual Button)
        bool dashInput = (dashAction != null && dashAction.action.WasPressedThisFrame()) || virtualDash;
        if (dashInput && Time.time >= nextDashTime)
        {
            // Reset virtual dash trigger immediately so it doesn't spam
            virtualDash = false; 
            StartCoroutine(Dash());
        }

        // Fire Check (Input System OR Virtual Button)
        bool fireInput = (fireAction != null && fireAction.action.IsPressed()) || virtualFire;
        if (currentWeapon != null && fireInput && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / currentWeapon.fireRate;
            justShotTargetFrame = true; 
        }
    }

    void FixedUpdate()
    {
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

            ObjectPooler.Instance.SpawnFromPool(currentWeapon.bulletTag, firePoint.position, finalRotation);
        }
        
        if (currentWeapon != null && currentWeapon.shootClip != null)
        {
            GameEvents.OnPlayerShoot?.Invoke(currentWeapon.shootClip);
        }
    }

    Transform GetClosestEnemyInSights()
    {
        int count = Physics2D.OverlapCircleNonAlloc(transform.position, assistRange, hitBuffer, enemyLayer);
        
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;
        
        Vector2 facingDir = moveInput != Vector2.zero ? moveInput : (Vector2)transform.up;

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hitBuffer[i];
            if (hit == null) continue;
            if (!hit.CompareTag("Enemy")) continue;

            Vector2 directionToEnemy = (hit.transform.position - transform.position).normalized;
            float distance = Vector2.Distance(transform.position, hit.transform.position);

            float angleToEnemy = Vector2.Angle(facingDir, directionToEnemy);
            if (angleToEnemy > assistAngle / 2) continue;

            Vector2 startPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            RaycastHit2D wallHit = Physics2D.Raycast(startPos, directionToEnemy, distance, obstacleLayer);
            
            if (wallHit.collider != null) continue; // Blocked by wall

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
        
        GameEvents.OnPlayerDash?.Invoke();
        
        // --- VISUALS & PHYSICS ---
        // Ghost Mode (Pass through everything)
        if (col != null) col.isTrigger = true; 
        
        // Visual Feedback (Transparency)
        if (weaponRenderer != null) 
        {
             SpriteRenderer bodySr = GetComponent<SpriteRenderer>();
             if (bodySr != null) bodySr.DOFade(0.4f, 0.1f);
             if (weaponRenderer != null) weaponRenderer.DOFade(0.4f, 0.1f);
        }
        
        yield return new WaitForSeconds(dashDuration);
        
        // Restore
        if (col != null) col.isTrigger = false;
        
        SpriteRenderer bodySrEnd = GetComponent<SpriteRenderer>();
        if (bodySrEnd != null) bodySrEnd.DOFade(1f, 0.1f);
        if (weaponRenderer != null) weaponRenderer.DOFade(1f, 0.1f);
            
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
            GameEvents.OnPlayerDeath?.Invoke(); 
            other.gameObject.SetActive(false); 
        }
    }
    
    public void ResetState()
    {
        isDashing = false;
        justDashedTargetFrame = false;
        nextDashTime = 0f;
        if(rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        } 
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