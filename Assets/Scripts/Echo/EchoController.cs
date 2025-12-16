using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; 

public class EchoController : MonoBehaviour
{
    private List<FrameData> framesToPlay;
    private int currentFrameIndex = 0;
    public GameObject bulletPrefab;
    public Transform firePoint;
    
    private bool isDead = false; 
    private bool isStaticDummy = false; // MỚI: Biến đánh dấu hình nhân đứng im
    
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }
    
    // Hàm khởi tạo cho Echo bình thường (có di chuyển)
    public void Initialize(List<FrameData> frames)
    {
        framesToPlay = new List<FrameData>(frames);
        currentFrameIndex = 0;
        isStaticDummy = false; // Đảm bảo không phải là dummy
        
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
        spriteRenderer.color = Color.red; 
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

        // --- SỬA ĐOẠN NÀY ---
        // Không cần check Time.time hay Cooldown nữa
        // Chỉ cần frame đó có lệnh bắn là thực hiện ngay
        if (data.isShooting) 
        {
            FireBullet();
        }
        // --------------------

        currentFrameIndex++;

        currentFrameIndex++;
    }

    void FireBullet()
    {
        ObjectPooler.Instance.SpawnFromPool("EnemyBullet", firePoint.position, firePoint.rotation);
    }
    
    public void Die()
    {
        if (isDead) return; 
        isDead = true;

        spriteRenderer.color = Color.gray; 
        spriteRenderer.DOFade(0.5f, 0.2f); 

        col.enabled = false;
        gameObject.tag = "Untagged"; 
        
        transform.DOShakeScale(0.2f, 0.5f);
    }
}