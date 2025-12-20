using UnityEngine;
using DG.Tweening;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker Instance;
    
    public float shootShakeStr = 0.2f;
    public float hitShakeStr = 1f;
    public float duration = 0.1f;

    void Awake()
    {
        Instance = this;
    }

    public void Shake(float strength, float duration)
    {
        transform.DOKill(); 

        transform.localPosition = new Vector3(0, 0, -10); 
        
        transform.DOShakePosition(duration, strength, 20, 90f);
    }

    public void ShakeShoot()
    {
        Shake(shootShakeStr, duration);
    }

    public void ShakeImpact()
    {
        Shake(hitShakeStr, 0.2f);
    }
}