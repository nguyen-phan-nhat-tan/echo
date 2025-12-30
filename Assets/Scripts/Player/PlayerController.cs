using UnityEngine;
using System.Collections;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public FloatingJoystick joystick;
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
    
    // --- STATE FLAGS ---
    [HideInInspector] public bool isDashing = false;
    private bool canControl = true;
    
    // --- RECORDER FLAGS ---
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
    }

    void OnDisable()
    {
        GameEvents.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState newState)
    {
        canControl = (newState == GameState.Playing);
        
        // FORCE RESET JOYSTICK
        // We toggle the Component (.enabled) instead of the GameObject (.SetActive)
        // to prevent visual flickering/layout rebuild glitches.
        if (joystick != null)
        {
            joystick.enabled = canControl;

            // If disabling, we also want to hide the visuals so they don't get stuck on screen
            if (!canControl)
            {
                // Most Joystick packs have the background/handle as children.
                // Resetting the RectTransform inputs is handled by OnDisable in most assets.
                // We zero out our local input to be safe.
                joystick.OnPointerUp(null); // Try to force internal reset if supported
                joystick.transform.GetChild(0).gameObject.SetActive(false); // Hide Background/Handle usually child 0
            }
            else
            {
                // Re-enable visuals for next loop (Floating joystick usually handles this on touch, 
                // but we ensure the object is ready)
                joystick.transform.GetChild(0).gameObject.SetActive(true);
            }
        }

        if (!canControl)
        {
            moveInput = Vector2.zero;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
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

        if (currentWeapon != null && Input.GetKey(KeyCode.LeftShift) && Time.time >= nextFireTime)
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
            rb.velocity = Vector2.zero;
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