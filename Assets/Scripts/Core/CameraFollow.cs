using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f; // Time to reach target (smaller = tighter/faster)
    public Vector3 offset = new Vector3(0, 0, -10f); // Default Z offset for 2D
    
    private Camera cam;
    private Vector3 velocity = Vector3.zero; // Reference for SmoothDamp

    void Start()
    {
        cam = GetComponent<Camera>();
        if(cam != null) offset.z = -10f; // Ensure Z is set for Ortho
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        
        // Use SmoothDamp for "Centering" feel (spring-like) instead of asymptotic Lerp
        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        
        float x = smoothedPosition.x;
        float y = smoothedPosition.y;

        // Clamp to Map Size bounds if GameManager exists
        if (GameManager.Instance != null && cam != null)
        {
             Vector2 mapSize = GameManager.Instance.mapSize;
             
             // Calculate camera dimensions
             float vertExtent = cam.orthographicSize;
             float horzExtent = vertExtent * cam.aspect;

             // Calculate bounds (Map Center is 0,0)
             // Left bound = -Width/2 + CameraHalfWidth
             float minX = (-mapSize.x / 2f) + horzExtent;
             float maxX = (mapSize.x / 2f) - horzExtent;
             float minY = (-mapSize.y / 2f) + vertExtent;
             float maxY = (mapSize.y / 2f) - vertExtent;

             // Handle case where map is smaller than camera view (center it)
             if (minX > maxX) x = 0f;
             else x = Mathf.Clamp(x, minX, maxX);

             if (minY > maxY) y = 0f;
             else y = Mathf.Clamp(y, minY, maxY);
        }
        
        transform.position = new Vector3(x, y, transform.position.z);
    }
}