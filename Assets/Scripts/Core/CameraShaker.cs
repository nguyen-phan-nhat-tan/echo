using UnityEngine;
using DG.Tweening;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance;
    
    // Config for different shake intensities
    public float shootShakeStr = 0.2f;
    public float hitShakeStr = 1f;
    public float duration = 0.1f;

    void Awake()
    {
        Instance = this;
    }

    public void Shake(float strength, float duration)
    {
        // DOKill ensures we don't stack shakes weirdly
        transform.DOKill(); 
        
        // Reset position before shaking (optional, prevents drifting)
        transform.localPosition = new Vector3(0, 0, -10); 
        
        transform.DOShakePosition(duration, strength, 20, 90f);
    }

    // Preset helper for shooting
    public void ShakeShoot()
    {
        Shake(shootShakeStr, duration);
    }

    // Preset helper for getting hit / enemy death
    public void ShakeImpact()
    {
        Shake(hitShakeStr, 0.2f);
    }
}