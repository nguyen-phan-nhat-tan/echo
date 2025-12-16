using UnityEngine;

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
    
    // Biến dùng để ghi dữ liệu (sẽ nói ở Bước 3)
    [HideInInspector] public bool justShotTargetFrame = false;
    [HideInInspector] public float rotationAngle;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Input di chuyển
        moveInput.x = joystick.Horizontal;
        moveInput.y = joystick.Vertical;

        // Xoay nhân vật theo hướng di chuyển
        if (moveInput != Vector2.zero)
        {
            rotationAngle = Mathf.Atan2(moveInput.y, moveInput.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle - 90f);
        }

        // Logic bắn súng (Nút Attack - hoặc giữ để spam)
        // Ở đây giả lập nhấn Space hoặc nút UI Attack
        if (Input.GetKey(KeyCode.LeftShift) && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + 1f / fireRate;
            
            // MỚI: Đánh dấu là đã bắn
            justShotTargetFrame = true; 
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void Shoot()
    {
        // Giới hạn tốc độ bắn (FireRate) ở đây nếu cần
        // Tìm dòng bắn đạn và sửa tag thành "PlayerBullet"
        ObjectPooler.Instance.SpawnFromPool("PlayerBullet", firePoint.position, firePoint.rotation);
    }
    
    // Hàm này tự động chạy khi Player va chạm vật lý với ai đó
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Kiểm tra xem cái mình vừa tông vào có phải là Enemy (Echo) không
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Game Over! Bạn chạm vào bản sao.");
            // TODO: Gọi hàm EndLoop(false) của GameManager
        }
    }
}