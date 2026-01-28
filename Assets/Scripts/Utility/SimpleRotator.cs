using UnityEngine;

public class SimpleRotator : MonoBehaviour
{
    [Tooltip("Degrees per second")]
    public Vector3 rotationSpeed = new Vector3(0, 90f, 0);

    // Option to use Unscaled time (rotates even when paused)
    public bool useUnscaledTime = false;

    void Update()
    {
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        transform.Rotate(rotationSpeed * dt);
    }
}
