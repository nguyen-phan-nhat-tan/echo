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
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    private float nextDashTime = 0f;
    
    [Header("Aim Assist")]
    public float assistRange = 5f;
    public float assistAngle = 45f;
    public LayerMask enemyLayer;
    
    // State Flags
    [HideInInspector] public bool isDashing = false;
    private bool canControl = true;
    
    // Recorder Flags
    [HideInInspector] public bool justShotTargetFrame = false;
    [HideInInspector] public bool justDashedTargetFrame = false;
    [HideInInspector] public float rotationAngle;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
    
        if (dashAction != null && dashAction.action.WasPressedThisFrame() && Time.time >= nextDashTime)
        {
            StartCoroutine(Dash());
        }

        if (currentWeapon != null && fireAction != null && fireAction.action.IsPressed() && Time.time >= nextFireTime)
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
        
        // --- UPDATED: Use Event instead of direct Managers ---
        if (currentWeapon != null && currentWeapon.shootClip != null)
        {
            GameEvents.OnPlayerShoot?.Invoke(currentWeapon.shootClip);
        }
        // ----------------------------------------------------
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
        
        // --- UPDATED: Use Event ---
        GameEvents.OnPlayerDash?.Invoke();
        // --------------------------
            
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