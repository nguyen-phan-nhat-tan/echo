using UnityEngine;
using DG.Tweening;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance;

    private Transform camTransform;
    private Vector3 initialPos;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        camTransform = GetComponent<Transform>();
        initialPos = camTransform.localPosition;
    }

    public void Shake(float intensity, float duration)
    {
        // 1. Kill any existing shake to prevent conflicts
        camTransform.DOKill();

        // 2. Reset to initial position (clean slate)
        camTransform.localPosition = initialPos;

        // 3. Start Shake
        // .SetLink(gameObject) is the Fix: It tells DOTween "If this Camera dies, stop shaking immediately."
        camTransform.DOShakePosition(duration, intensity, 20, 90, false, true)
            .SetLink(gameObject); 
    }
}