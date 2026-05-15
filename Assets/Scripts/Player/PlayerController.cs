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
    
    [Header("Debug")]
    public bool isInvincible = false;
    
    // STAT MULTIPLIERS (Debuffs/Buffs)
    private float moveSpeedMult = 1f;
    private float fireRateMult = 1f;
    private float dashCooldownMult = 1f;

    // MECHANICS FLAGS
    private bool driftEnabled = false;
    
    public void SetStatsMultiplier(float move, float fire, float dash)
    {
        moveSpeedMult = move;
        fireRateMult = fire;
        dashCooldownMult = dash;
    }

    public void SetMechanicsRef(bool drift)
    {
        driftEnabled = drift;
    }

    // Recorder Flags
    [HideInInspector] public bool justShotTargetFrame = false;
    [HideInInspector] public bool justDashedTargetFrame = false;
    [HideInInspector] public float rotationAngle;

    private Collider2D col; // NEW

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>(); 
        if (firePoint != null) defaultFirePointPos = firePoint.localPosition; // Store initial
        if (weaponRenderer != null) 
        {
            defaultWeaponPos = weaponRenderer.transform.localPosition;
            defaultWeaponRot = weaponRenderer.transform.localRotation;
        }
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
            // Stop logic handles in Update, but physically stop here
            if (rb != null)
            {
               // Keep velocity if drifting? No, pause breaks physics usually
               if (newState != GameState.Playing)
               {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
               }
            }
        }
    }
    
    public void EquipWeapon(WeaponData newData)
    {
        currentWeapon = newData;
        currentSpiralAngle = 0f; // Reset spiral
        if (firePoint != null)
        {
            firePoint.localRotation = Quaternion.identity; 
            firePoint.localPosition = defaultFirePointPos; // Reset position
        }

        if (weaponRenderer != null && weaponRenderer.transform != transform)
        {
            weaponRenderer.transform.localPosition = defaultWeaponPos;
            weaponRenderer.transform.localRotation = defaultWeaponRot;
        }

        if (weaponRenderer != null && newData.weaponSprite != null)
        {
            weaponRenderer.sprite = newData.weaponSprite;
        }
    }
    
    // Virtual Input (Mobile)
    [HideInInspector] public Vector2 virtualMove = Vector2.zero;
    [HideInInspector] public bool virtualFire = false;
    [HideInInspector] public bool virtualDash = false;

    void Update()
    {
        if (!canControl) return;
        if (isDashing) return;
        
        Vector2 actionMove = Vector2.zero;
        if (moveAction != null)
        {
            actionMove = moveAction.action.ReadValue<Vector2>();
        }

        // Touch joystick takes priority while active; keyboard/gamepad remains fallback.
        moveInput = virtualMove.sqrMagnitude > 0.0001f ? virtualMove : actionMove;
        
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
            StartShootRoutine();
            // Calculation for next fire time should account for the burst duration? 
            // Usually fire rate is "time between starts of bursts".
            nextFireTime = Time.time + 1f / (currentWeapon.fireRate * fireRateMult);
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
            Vector2 targetVel = moveInput * (moveSpeed * moveSpeedMult);
            
            if (driftEnabled)
            {
                // DRIFT PHYSICS: Lerp velocity for slippery feel
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVel, 5f * Time.fixedDeltaTime);
            }
            else
            {
                // STANDARD: Direct position move (snappy)
                Vector2 nextPos = rb.position + targetVel * Time.fixedDeltaTime;
                
                // CLAMP TO MAP
                if (GameManager.Instance != null)
                {
                    Vector2 halfMap = GameManager.Instance.mapSize / 2f;
                    nextPos.x = Mathf.Clamp(nextPos.x, -halfMap.x, halfMap.x);
                    nextPos.y = Mathf.Clamp(nextPos.y, -halfMap.y, halfMap.y);
                }

                rb.MovePosition(nextPos);
                
                // Ensure physics velocity doesn't fight MovePosition
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private Coroutine shootCoroutine;

    void StartShootRoutine()
    {
        if (shootCoroutine != null) StopCoroutine(shootCoroutine);
        shootCoroutine = StartCoroutine(ShootSequence());
    }

    IEnumerator ShootSequence()
    {
        int shots = currentWeapon.burstCount;
        if (shots < 1) shots = 1;

        for (int b = 0; b < shots; b++)
        {
            PerformShoot();
            if (shots > 1) yield return new WaitForSeconds(currentWeapon.burstDelay);
        }
    }

    // State for Spiral
    private float currentSpiralAngle = 0f;
    private Vector3 defaultFirePointPos;
    private Vector3 defaultWeaponPos;
    private Quaternion defaultWeaponRot;

    void PerformShoot()
    {
        Quaternion baseRotation = firePoint.rotation;
        
        // ASPIRAL LOGIC
        if (currentWeapon.spiralRate != 0)
        {
            // NEW: Ring/Nova Pattern
            // Fire multiple bullets in a circle instantly
            int pelletCount = currentWeapon.bulletCount; 
            if (pelletCount < 1) pelletCount = 1;
            
            float angleStep = 360f / pelletCount;
            
            for (int i = 0; i < pelletCount; i++)
            {
                float angle = i * angleStep;
                Quaternion rotation = Quaternion.Euler(0f, 0f, angle - 90f); // -90 because Up is 0, usually sprite faces Up
                
                // If firePoint exists, use its position, otherwise transform
                Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
                
                GameObject bulletObj = ObjectPooler.Instance.SpawnFromPool(currentWeapon.bulletTag, spawnPos, rotation);
                
                Bullet bulletScript = bulletObj.GetComponent<Bullet>();
                if (bulletScript != null) bulletScript.Initialize(currentWeapon);
            }

            // Since we handled spawning inside this block, we should return or skip the standard logic below
            // Ideally we structure this so we don't duplicate code, but the standard logic below handles "Spread" and "Burst" differently.
            // The request purely asked to rewrite the SPIRAL logic.
            
            if (currentWeapon != null)
            {
                 // Custom Muzzle Flash
                 if (currentWeapon.muzzleFlashPrefab != null)
                 {
                     Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
                     Quaternion spawnRot = (firePoint != null) ? firePoint.rotation : transform.rotation;
                     Instantiate(currentWeapon.muzzleFlashPrefab, spawnPos, spawnRot);
                 }
                 
                 if (currentWeapon.shootClip != null)
                 {
                      GameEvents.OnPlayerShoot?.Invoke(currentWeapon.shootClip);
                 }
            }
            return; // Exit early as we fired the ring
        }

        else
        {
            // Standard Aiming (only if not spiraling)
            Transform target = GetClosestEnemyInSights();
            if (target != null)
            {
                Vector2 directionToTarget = target.position - firePoint.position;
                float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
                baseRotation = Quaternion.Euler(0f, 0f, angle - 90f);
            }
        }

        int count = currentWeapon.bulletCount;
        
        for (int i = 0; i < count; i++)
        {
            Quaternion finalRotation;
            
            // FIXED PATTERN LOGIC
            if (currentWeapon.fixedPatternCount > 0 && count > 1)
            {
                // Evenly distributed spread
                // Example: Spanning 90 degrees total. -45 to 45.
                // Or use spreadAngle as total arc.
                float totalArc = currentWeapon.spreadAngle;
                float step = totalArc / (count - 1);
                float startAngle = -totalArc / 2f;
                float currentAngle = startAngle + (step * i);
                
                finalRotation = baseRotation * Quaternion.Euler(0, 0, currentAngle);
            }
            else
            {
                // Random Spread (Original)
                float randomSpread = Random.Range(-currentWeapon.spreadAngle / 2f, currentWeapon.spreadAngle / 2f);
                finalRotation = baseRotation * Quaternion.Euler(0, 0, randomSpread);
            }

            GameObject bulletObj = ObjectPooler.Instance.SpawnFromPool(currentWeapon.bulletTag, firePoint.position, finalRotation);
            
            // Initialize Bullet Data (Ricochet, Wave)
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            if (bulletScript != null) bulletScript.Initialize(currentWeapon);
        }
        
        if (currentWeapon != null)
        {
            // Custom Muzzle Flash
             if (currentWeapon.muzzleFlashPrefab != null)
             {
                 Instantiate(currentWeapon.muzzleFlashPrefab, firePoint.position, firePoint.rotation);
             }

            if (currentWeapon.shootClip != null)
            {
                GameEvents.OnPlayerShoot?.Invoke(currentWeapon.shootClip);
            }
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
        nextDashTime = Time.time + (dashCooldown * dashCooldownMult); // Applied multiplier (higher mult = longer cooldown? actually logic was mult < 1 for debuff?)
        // Wait, for Heavy debuff I set mult = 0.5. If cooldown is 1s, 0.5s is faster.
        // I should probably DIVIDE by mult if I want 'Heavy' to make it SLOWER (longer cooldown).
        // OR I should change GameManager to send > 1 for debuffs on delay.
        // Let's check GameManager: "if (currentDebuff == DebuffType.Heavy) dashMult = 0.5f;"
        // If I want Slow Dash (Heavy), I probably want LONGER cooldown or SLOWER speed.
        // "Heavy" usually implies slow movement or slow actions.
        // If I multiply cooldown by 0.5, it becomes FASTER. I want to multiply by (1/mult) or 2.
        // Let's stick to the convention: Multiplier < 1 means WORSE stat. 
        // For Cooldown, WORSE means LONGER. So NewCooldown = Base / Mult.
        // Example: Base=1, Mult=0.5. New = 1/0.5 = 2s. CORRECT.
        
        // Wait, wait. Let's look at FireRate. 
        // nextFireTime = Time.time + 1f / (fireRate * fireRateMult);
        // Mult = 0.5. Rate = 5 * 0.5 = 2.5. Delay = 1/2.5 = 0.4s (vs 0.2s). SLOWER. CORRECT.
        
        // So for COOLDOWN (Dash), to make it SLOWER (worse), I need to INCREASE the value.
        // So dividing by mult (if mult < 1) increases it.
        
        nextDashTime = Time.time + (dashCooldown / dashCooldownMult); // FIXED LOGIC
        
        GameEvents.OnPlayerDash?.Invoke();
        
        // --- VISUALS & PHYSICS ---
        // Ghost Mode (Pass through everything)
        if (col != null) col.isTrigger = true; 
        
        // Visual Feedback (Transparency)
        if (weaponRenderer != null) 
        {
             SpriteRenderer bodySr = GetComponentInChildren<SpriteRenderer>();
             if (bodySr != null) bodySr.DOFade(0.4f, 0.1f);
             if (weaponRenderer != null) weaponRenderer.DOFade(0.4f, 0.1f);
        }
        
        yield return new WaitForSeconds(dashDuration);
        
        // Restore
        if (col != null) col.isTrigger = false;
        
        SpriteRenderer bodySrEnd = GetComponentInChildren<SpriteRenderer>();
        if (bodySrEnd != null) bodySrEnd.DOFade(1f, 0.1f);
        if (weaponRenderer != null) weaponRenderer.DOFade(1f, 0.1f);
            
        isDashing = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing) return;
        if (isInvincible) return;
        if (collision.gameObject.CompareTag("Enemy"))
        {
            GameEvents.OnPlayerDeath?.Invoke();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDashing) return;
        if (isInvincible) return;
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