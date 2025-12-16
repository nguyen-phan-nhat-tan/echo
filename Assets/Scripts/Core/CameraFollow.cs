using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;       // Mục tiêu để bám theo (Player)
    public float smoothSpeed = 0.125f; // Độ mượt (càng nhỏ càng trễ, càng mượt)
    public Vector3 offset;         // Khoảng cách lệch (để camera không đè lên đầu nhân vật)

    void LateUpdate() // Dùng LateUpdate để đảm bảo Player đã di chuyển xong rồi Camera mới chạy theo
    {
        if (target == null) return;

        // Tính toán vị trí mong muốn
        Vector3 desiredPosition = target.position + offset;
        
        // Dùng Lerp để di chuyển từ từ thay vì giật cục
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // Gán vị trí mới (giữ nguyên Z để không bị mất hình)
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
    }
}