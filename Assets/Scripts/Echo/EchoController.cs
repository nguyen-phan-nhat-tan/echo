using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening; 

public class EchoController : MonoBehaviour
{
    private List<FrameData> framesToPlay;
    private int currentFrameIndex = 0;
    public GameObject bulletPrefab;
    public Transform firePoint;
    
    [Header("State Flags")]
    private bool isDead = false; 
    private bool isStaticDummy = false;
    private bool wasDashing = false; // Tracks previous frame state
    
    public static event Action<int> OnEnemyKilled;
    
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    
    private Color originalColor = Color.red;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        originalColor = spriteRenderer.color;
    }
    
    // Hàm khởi tạo cho Echo bình thường (có di chuyển)
    public void Initialize(List<FrameData> frames)
    {
        framesToPlay = new List<FrameData>(frames);
        currentFrameIndex = 0;
        isStaticDummy = false; // Đảm bảo không phải là dummy
        wasDashing = false;
        
        ResetState(); // Gọi hàm reset trạng thái chung
    }

    // MỚI: Hàm khởi tạo cho Echo hình nhân (đứng im ở Loop 1)
    public void InitializeDummy()
    {
        isStaticDummy = true;
        framesToPlay = null; 
        
        ResetState();
    }

    // Hàm phụ để reset màu sắc và va chạm (dùng chung)
    void ResetState()
    {
        isDead = false;
        spriteRenderer.color = originalColor; 
        spriteRenderer.DOFade(0.7f, 0f);  
        col.enabled = true;               
        tag = "Enemy";                    
        
        // Hiệu ứng xuất hiện
        transform.localScale = Vector3.zero;
        transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
    }

    void FixedUpdate()
    {
        if (isDead || isStaticDummy) return; // Logic cũ
        
        if (framesToPlay == null || currentFrameIndex >= framesToPlay.Count) return;

        FrameData data = framesToPlay[currentFrameIndex];

        // Tái hiện di chuyển (Giữ nguyên)
        transform.position = data.position;
        transform.rotation = Quaternion.Euler(0f, 0f, data.rotation - 90f);

        if (data.isDashing && !wasDashing)
        {
            StartDash();
        }
        else if (!data.isDashing && wasDashing)
        {
            EndDash();
        }
        wasDashing = data.isDashing;

        // 3. Handle Shooting
        // Only shoot if NOT dashing (prevent weird sliding shots)
        if (data.isShooting && !data.isDashing) 
        {
            FireBullet();
        }

        currentFrameIndex++;
    }
    
    void StartDash()
    {
        // Visuals: Turn Cyan and Transparent (Ghostly)
        spriteRenderer.DOColor(Color.cyan, 0.1f);
        spriteRenderer.DOFade(0.4f, 0.1f);

        // Logic: Disable collider so we don't unfairly kill player
        col.enabled = false;
    }
    
    void EndDash()
    {
        // Visuals: Return to Red and Normal Opacity
        spriteRenderer.DOColor(originalColor, 0.1f);
        spriteRenderer.DOFade(0.7f, 0.1f);

        // Logic: Re-enable collider to become lethal again
        col.enabled = true;
    }
    
    void FireBullet()
    {
        ObjectPooler.Instance.SpawnFromPool("EnemyBullet", firePoint.position, firePoint.rotation);
    }
    
    public void Die()
    {
        if (isDead) return; 
        isDead = true;
        
        OnEnemyKilled?.Invoke(100);
        
        spriteRenderer.DOKill();
        spriteRenderer.color = Color.gray; 
        spriteRenderer.DOFade(0.8f, 0.2f);

        col.enabled = false;
        gameObject.tag = "Untagged"; 
        
        transform.DOShakeScale(0.2f, 0.5f);
    }
}