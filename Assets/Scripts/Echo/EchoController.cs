using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening; 

public class EchoController : MonoBehaviour
{
    public static event Action<int> OnEnemyKilled;

    [Header("References")]
    public Transform firePoint;
    private WeaponData currentWeapon;

    private List<FrameData> framesToPlay;
    private int currentFrameIndex = 0;
    
    [Header("State Flags")]
    private bool isDead = false; 
    private bool isStaticDummy = false;
    private bool wasDashing = false;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Color originalColor = Color.red;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        originalColor = spriteRenderer.color;
    }

    public void Initialize(List<FrameData> frames, WeaponData weapon)
    {
        framesToPlay = new List<FrameData>(frames);
        currentWeapon = weapon;
        
        currentFrameIndex = 0;
        isStaticDummy = false;
        wasDashing = false;
        
        ResetState();
    }

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
        transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
    }

    void FixedUpdate()
    {
        if (isDead || isStaticDummy) return; 
        if (GameManager.Instance.currentState != GameState.Playing) return;
        if (framesToPlay == null || currentFrameIndex >= framesToPlay.Count) return;

        FrameData data = framesToPlay[currentFrameIndex];

        transform.position = data.position;
        transform.rotation = Quaternion.Euler(0f, 0f, data.rotation - 90f);

        if (data.isDashing && !wasDashing) StartDash();
        else if (!data.isDashing && wasDashing) EndDash();
        
        wasDashing = data.isDashing;

        if (data.isShooting && !data.isDashing) 
        {
            FireBullet();
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
    
    void FireBullet()
    {
        if (currentWeapon == null) return;
        
        for (int i = 0; i < currentWeapon.bulletCount; i++)
        {
            float randomSpread = UnityEngine.Random.Range(-currentWeapon.spreadAngle / 2f, currentWeapon.spreadAngle / 2f);
            Quaternion finalRotation = firePoint.rotation * Quaternion.Euler(0, 0, randomSpread);
            
            ObjectPooler.Instance.SpawnFromPool("EnemyBullet", firePoint.position, finalRotation);
        }
    }
    
    public void Die()
    {
        if (isDead) return; 
        isDead = true;
        
        gameObject.tag = "Untagged"; 
        OnEnemyKilled?.Invoke(100);
        
        spriteRenderer.DOKill();

        spriteRenderer.color = new Color(0.3f, 0f, 0f, 1f); 
        spriteRenderer.DOFade(0.8f, 0.2f);
        spriteRenderer.sortingOrder = -1; 

        col.enabled = false;
    }
    
    void OnDestroy()
    {
        transform.DOKill(); 
        if (spriteRenderer != null) spriteRenderer.DOKill();
    }
}